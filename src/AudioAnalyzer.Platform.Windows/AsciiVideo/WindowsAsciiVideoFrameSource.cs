using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Versioning;
using System.Threading.Channels;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using Microsoft.Extensions.Logging;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;

namespace AudioAnalyzer.Platform.Windows.AsciiVideo;

/// <summary>Webcam capture using WinRT <see cref="MediaCapture"/> and <see cref="MediaFrameReader"/>; latest frame only, CPU BGRA.</summary>
[SupportedOSPlatform("windows10.0.19041.0")]
public sealed partial class WindowsAsciiVideoFrameSource : IAsciiVideoFrameSource
{
    private readonly Channel<AsciiVideoCaptureRequest?> _commandChannel;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _worker;
    private readonly object _frameLock = new();
    private readonly ILogger<WindowsAsciiVideoFrameSource> _logger;
    private byte[]? _latestPixels;
    private int _latestWidth;
    private int _latestHeight;
    private long _latestSequence;
    private volatile bool _webcamStarting;
    private volatile bool _webcamSessionActive;
    private int _framePumpBusy;
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="WindowsAsciiVideoFrameSource"/> class.</summary>
    /// <param name="logger">Logger for capture failures.</param>
    public WindowsAsciiVideoFrameSource(ILogger<WindowsAsciiVideoFrameSource> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _commandChannel = Channel.CreateBounded<AsciiVideoCaptureRequest?>(new BoundedChannelOptions(4)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });
        _worker = Task.Run(ProcessCommandsAsync);
    }

    /// <inheritdoc />
    public void PrepareForFrame(AsciiVideoCaptureRequest? request)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _commandChannel.Writer.TryWrite(CloneRequest(request));
    }

    /// <inheritdoc />
    public bool IsWebcamStarting => _webcamStarting;

    /// <inheritdoc />
    public bool IsWebcamSessionActive => _webcamSessionActive;

    /// <inheritdoc />
    public bool TryGetLatestFrame(out AsciiVideoFrameSnapshot? snapshot)
    {
        if (_disposed)
        {
            snapshot = null;
            return false;
        }

        lock (_frameLock)
        {
            if (_latestPixels == null || _latestWidth <= 0 || _latestHeight <= 0)
            {
                snapshot = null;
                return false;
            }

            int len = _latestWidth * _latestHeight * 4;
            var copy = GC.AllocateUninitializedArray<byte>(len);
            Buffer.BlockCopy(_latestPixels, 0, copy, 0, len);
            snapshot = new AsciiVideoFrameSnapshot(_latestWidth, _latestHeight, _latestSequence, copy);
            return true;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _commandChannel.Writer.TryComplete();
        _cts.Cancel();
        try
        {
            _worker.GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            LogAsciiVideoWorkerShutdownFailed(ex);
        }

        _cts.Dispose();
    }

    internal void TryScheduleFrameProcessing(MediaFrameReader sender)
    {
        if (_disposed)
        {
            return;
        }

        if (Interlocked.CompareExchange(ref _framePumpBusy, 1, 0) != 0)
        {
            return;
        }

        _ = ProcessReaderFrameAsync(sender);
    }

    private async Task ProcessReaderFrameAsync(MediaFrameReader sender)
    {
        SoftwareBitmap? surfaceCopy = null;
        try
        {
            if (_disposed)
            {
                return;
            }

            using MediaFrameReference? frame = sender.TryAcquireLatestFrame();
            VideoMediaFrame? vmf = frame?.VideoMediaFrame;
            if (vmf == null)
            {
                return;
            }

            SoftwareBitmap? raw = vmf.SoftwareBitmap;
            if (raw == null && vmf.Direct3DSurface != null)
            {
                IDirect3DSurface surf = vmf.Direct3DSurface;
                surfaceCopy = await SoftwareBitmap.CreateCopyFromSurfaceAsync(surf).AsTask().ConfigureAwait(false);
                raw = surfaceCopy;
            }

            if (raw == null)
            {
                return;
            }

            if (_disposed)
            {
                return;
            }

            using SoftwareBitmap converted = SoftwareBitmap.Convert(raw, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            int w = converted.PixelWidth;
            int h = converted.PixelHeight;
            if (w <= 0 || h <= 0)
            {
                return;
            }

            int byteLen = w * h * 4;
            var pixels = GC.AllocateUninitializedArray<byte>(byteLen);
            MemoryBufferInterop.CopyBgra8FromSoftwareBitmap(converted, w, h, pixels);

            if (_disposed)
            {
                return;
            }

            PublishFrame(pixels, w, h);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAsciiVideoFrameProcessingFailed(ex);
        }
        finally
        {
            surfaceCopy?.Dispose();
            Interlocked.Exchange(ref _framePumpBusy, 0);
        }
    }

    private async Task ProcessCommandsAsync()
    {
        CaptureSession? active = null;
        try
        {
            await foreach (AsciiVideoCaptureRequest? command in _commandChannel.Reader.ReadAllAsync(_cts.Token))
            {
                AsciiVideoCaptureRequest? latest = command;
                while (_commandChannel.Reader.TryRead(out AsciiVideoCaptureRequest? more))
                {
                    latest = more;
                }

                active = await ApplyCommandAsync(active, latest, _cts.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            /* Expected on dispose */
        }
        catch (Exception ex)
        {
            LogAsciiVideoCommandLoopFailed(ex);
        }

        await TearDownSessionAsync(active).ConfigureAwait(false);
    }

    private async Task<CaptureSession?> ApplyCommandAsync(CaptureSession? session, AsciiVideoCaptureRequest? request, CancellationToken ct)
    {
        if (request == null || request.SourceKind != AsciiVideoSourceKind.Webcam)
        {
            await TearDownSessionAsync(session).ConfigureAwait(false);
            ClearLatestFrame();
            return null;
        }

        if (session != null && RequestMatchesSession(session, request))
        {
            return session;
        }

        await TearDownSessionAsync(session).ConfigureAwait(false);
        ClearLatestFrame();
        return await TryStartSessionAsync(request, ct).ConfigureAwait(false);
    }

    private static bool RequestMatchesSession(CaptureSession session, AsciiVideoCaptureRequest request)
    {
        return session.Request is { } r
            && r.SourceKind == request.SourceKind
            && r.WebcamDeviceIndex == request.WebcamDeviceIndex
            && r.MaxCaptureWidth == request.MaxCaptureWidth
            && r.MaxCaptureHeight == request.MaxCaptureHeight;
    }

    private async Task<CaptureSession?> TryStartSessionAsync(AsciiVideoCaptureRequest request, CancellationToken ct)
    {
        SetWebcamStarting(true);
        try
        {
            IReadOnlyList<MediaFrameSourceGroup> groups = await MediaFrameSourceGroup.FindAllAsync().AsTask(ct).ConfigureAwait(false);
            if (groups.Count == 0)
            {
                LogAsciiVideoNoCameraGroups();
                return null;
            }

            int idx = ((request.WebcamDeviceIndex % groups.Count) + groups.Count) % groups.Count;
            MediaFrameSourceGroup group = groups[idx];

            var settings = new MediaCaptureInitializationSettings
            {
                SourceGroup = group,
                SharingMode = MediaCaptureSharingMode.SharedReadOnly,
                StreamingCaptureMode = StreamingCaptureMode.Video,
                MemoryPreference = MediaCaptureMemoryPreference.Cpu
            };

            var mediaCapture = new MediaCapture();
            await mediaCapture.InitializeAsync(settings).AsTask(ct).ConfigureAwait(false);

            MediaFrameSource? frameSource = PickColorOrPreviewSource(mediaCapture);
            if (frameSource == null)
            {
                LogAsciiVideoNoFrameSource();
                mediaCapture.Dispose();
                return null;
            }

            await TryApplyResolutionCapAsync(frameSource, request, ct).ConfigureAwait(false);

            var reader = await mediaCapture.CreateFrameReaderAsync(frameSource).AsTask(ct).ConfigureAwait(false);
            reader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Realtime;

            var captureSession = new CaptureSession(this, mediaCapture, reader, request);
            reader.FrameArrived += captureSession.OnFrameArrived;

            await reader.StartAsync().AsTask(ct).ConfigureAwait(false);
            SetWebcamSessionActive(true);
            return captureSession;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogAsciiVideoStartSessionFailed(ex);
            return null;
        }
        finally
        {
            SetWebcamStarting(false);
        }
    }

    /// <summary>
    /// Picks a frame source in stable order: Color+VideoPreview (Microsoft-recommended), then Color+VideoRecord,
    /// then any Color, then VideoPreview / VideoRecord, else first source by <see cref="MediaFrameSourceInfo.Id"/>.
    /// </summary>
    private static MediaFrameSource? PickColorOrPreviewSource(MediaCapture mediaCapture)
    {
        var sources = mediaCapture.FrameSources.Values.ToList();
        sources.Sort(static (a, b) => string.CompareOrdinal(a.Info.Id, b.Info.Id));

        static MediaFrameSource? FirstMatch(IReadOnlyList<MediaFrameSource> list, Func<MediaFrameSource, bool> predicate)
        {
            for (int i = 0; i < list.Count; i++)
            {
                MediaFrameSource s = list[i];
                if (predicate(s))
                {
                    return s;
                }
            }

            return null;
        }

        MediaFrameSource? picked = FirstMatch(
            sources,
            static s => s.Info.SourceKind == MediaFrameSourceKind.Color
                && s.Info.MediaStreamType == MediaStreamType.VideoPreview);
        if (picked != null)
        {
            return picked;
        }

        picked = FirstMatch(
            sources,
            static s => s.Info.SourceKind == MediaFrameSourceKind.Color
                && s.Info.MediaStreamType == MediaStreamType.VideoRecord);
        if (picked != null)
        {
            return picked;
        }

        picked = FirstMatch(sources, static s => s.Info.SourceKind == MediaFrameSourceKind.Color);
        if (picked != null)
        {
            return picked;
        }

        picked = FirstMatch(sources, static s => s.Info.MediaStreamType == MediaStreamType.VideoPreview);
        if (picked != null)
        {
            return picked;
        }

        picked = FirstMatch(sources, static s => s.Info.MediaStreamType == MediaStreamType.VideoRecord);
        return picked ?? sources.FirstOrDefault();
    }

    private async Task TryApplyResolutionCapAsync(
        MediaFrameSource frameSource,
        AsciiVideoCaptureRequest request,
        CancellationToken ct)
    {
        int? maxW = request.MaxCaptureWidth;
        int? maxH = request.MaxCaptureHeight;
        if (maxW is null or <= 0 && maxH is null or <= 0)
        {
            return;
        }

        try
        {
            IReadOnlyList<MediaFrameFormat> formats = frameSource.SupportedFormats;
            MediaFrameFormat? best = null;
            int bestPixels = -1;
            foreach (MediaFrameFormat format in formats)
            {
                var video = format.VideoFormat;
                int w = (int)video.Width;
                int h = (int)video.Height;
                if (maxW is > 0 && w > maxW)
                {
                    continue;
                }

                if (maxH is > 0 && h > maxH)
                {
                    continue;
                }

                int pixels = w * h;
                if (pixels > bestPixels)
                {
                    bestPixels = pixels;
                    best = format;
                }
            }

            if (best != null)
            {
                await frameSource.SetFormatAsync(best).AsTask(ct).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            LogAsciiVideoFormatCapFailed(ex);
        }
    }

    private async Task TearDownSessionAsync(CaptureSession? session)
    {
        if (session == null)
        {
            return;
        }

        SetWebcamSessionActive(false);

        try
        {
            session.Reader.FrameArrived -= session.OnFrameArrived;
            await session.Reader.StopAsync().AsTask(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogAsciiVideoReaderStopFailed(ex);
        }

        session.Reader.Dispose();
        session.MediaCapture.Dispose();
    }

    private void ClearLatestFrame()
    {
        lock (_frameLock)
        {
            _latestPixels = null;
            _latestWidth = 0;
            _latestHeight = 0;
        }
    }

    private static AsciiVideoCaptureRequest? CloneRequest(AsciiVideoCaptureRequest? r)
    {
        if (r == null)
        {
            return null;
        }

        return new AsciiVideoCaptureRequest(r.SourceKind, r.WebcamDeviceIndex, r.MaxCaptureWidth, r.MaxCaptureHeight);
    }

    private void PublishFrame(byte[] pixels, int w, int h)
    {
        lock (_frameLock)
        {
            _latestPixels = pixels;
            _latestWidth = w;
            _latestHeight = h;
            _latestSequence++;
        }
    }

    private void SetWebcamSessionActive(bool active)
    {
        _webcamSessionActive = active;
    }

    private void SetWebcamStarting(bool starting)
    {
        _webcamStarting = starting;
    }

    private sealed class CaptureSession
    {
        public CaptureSession(
            WindowsAsciiVideoFrameSource owner,
            MediaCapture mediaCapture,
            MediaFrameReader reader,
            AsciiVideoCaptureRequest request)
        {
            Owner = owner;
            MediaCapture = mediaCapture;
            Reader = reader;
            Request = request;
        }

        public WindowsAsciiVideoFrameSource Owner { get; }
        public MediaCapture MediaCapture { get; }
        public MediaFrameReader Reader { get; }
        public AsciiVideoCaptureRequest Request { get; }

        public void OnFrameArrived(MediaFrameReader sender, object args)
        {
            _ = args;
            Owner.TryScheduleFrameProcessing(sender);
        }
    }
}
