using System.Runtime.InteropServices;
using System.Threading.Channels;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using Microsoft.Extensions.Logging;

namespace AudioAnalyzer.Platform.macOS.AsciiVideo;

/// <summary>
/// Webcam capture using AVFoundation via <c>libvideo_capture_shim.dylib</c>; latest frame only, CPU BGRA.
/// A background worker applies <see cref="PrepareForFrame"/> commands; frames arrive on the shim's dispatch queue.
/// </summary>
public sealed partial class MacOsAsciiVideoFrameSource : IAsciiVideoFrameSource
{
    private static readonly MacOsVideoCaptureShimNative.VideoCaptureFrameCallback s_frameCallback = OnFrameStatic;

    private readonly Channel<AsciiVideoCaptureRequest?> _commandChannel;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _worker;
    private readonly object _frameLock = new();
    private readonly ILogger<MacOsAsciiVideoFrameSource> _logger;
    private GCHandle _selfHandle;
    private AsciiVideoCaptureRequest? _activeRequest;
    private byte[]? _latestPixels;
    private int _latestWidth;
    private int _latestHeight;
    private long _latestSequence;
    private volatile bool _webcamStarting;
    private volatile bool _webcamSessionActive;
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="MacOsAsciiVideoFrameSource"/> class.</summary>
    /// <param name="logger">Logger for capture failures.</param>
    public MacOsAsciiVideoFrameSource(ILogger<MacOsAsciiVideoFrameSource> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _selfHandle = GCHandle.Alloc(this);
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

        StopNativeCapture();

        if (_selfHandle.IsAllocated)
        {
            _selfHandle.Free();
        }

        _cts.Dispose();
    }

    private async Task ProcessCommandsAsync()
    {
        try
        {
            await foreach (AsciiVideoCaptureRequest? command in _commandChannel.Reader.ReadAllAsync(_cts.Token).ConfigureAwait(false))
            {
                AsciiVideoCaptureRequest? latest = command;
                while (_commandChannel.Reader.TryRead(out AsciiVideoCaptureRequest? more))
                {
                    latest = more;
                }

                ApplyCommand(latest);
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

        StopNativeCapture();
    }

    private void ApplyCommand(AsciiVideoCaptureRequest? request)
    {
        if (request == null || request.SourceKind != AsciiVideoSourceKind.Webcam)
        {
            StopNativeCapture();
            ClearLatestFrame();
            _activeRequest = null;
            return;
        }

        if (_activeRequest != null && RequestMatches(_activeRequest, request))
        {
            return;
        }

        StopNativeCapture();
        ClearLatestFrame();
        StartNativeCapture(request);
    }

    private static bool RequestMatches(AsciiVideoCaptureRequest a, AsciiVideoCaptureRequest b)
    {
        return a.SourceKind == b.SourceKind
            && a.WebcamDeviceIndex == b.WebcamDeviceIndex
            && a.MaxCaptureWidth == b.MaxCaptureWidth
            && a.MaxCaptureHeight == b.MaxCaptureHeight;
    }

    private void StartNativeCapture(AsciiVideoCaptureRequest request)
    {
        if (!EnsureLibraryLoaded())
        {
            LogAsciiVideoShimMissing();
            return;
        }

        SetWebcamStarting(true);
        IntPtr errorBuffer = Marshal.AllocHGlobal(512);
        try
        {
            var config = new MacOsVideoCaptureShimNative.VideoCaptureConfig
            {
                DeviceIndex = request.WebcamDeviceIndex,
                MaxWidth = request.MaxCaptureWidth ?? 0,
                MaxHeight = request.MaxCaptureHeight ?? 0,
            };

            int result = MacOsVideoCaptureShimNative.VideoCaptureStart(
                ref config,
                s_frameCallback,
                GCHandle.ToIntPtr(_selfHandle),
                errorBuffer,
                (UIntPtr)512);

            if (result != 0)
            {
                string message = Marshal.PtrToStringUTF8(errorBuffer) ?? "video_capture_start failed.";
                LogAsciiVideoStartSessionFailed(message);
                return;
            }

            _activeRequest = request;
            SetWebcamSessionActive(true);
        }
        catch (Exception ex)
        {
            LogAsciiVideoStartSessionThrew(ex);
        }
        finally
        {
            Marshal.FreeHGlobal(errorBuffer);
            SetWebcamStarting(false);
        }
    }

    private void StopNativeCapture()
    {
        if (!MacOsVideoCaptureShimNative.IsLibraryLoaded)
        {
            SetWebcamSessionActive(false);
            return;
        }

        try
        {
            MacOsVideoCaptureShimNative.VideoCaptureStop();
        }
        catch (Exception ex)
        {
            LogAsciiVideoStopFailed(ex);
        }
        finally
        {
            SetWebcamSessionActive(false);
        }
    }

    private static bool EnsureLibraryLoaded()
    {
        if (MacOsVideoCaptureShimNative.IsLibraryLoaded)
        {
            return true;
        }

        try
        {
            _ = MacOsVideoCaptureShimNative.VideoCaptureIsSupported();
        }
        catch (DllNotFoundException)
        {
            return false;
        }

        return MacOsVideoCaptureShimNative.IsLibraryLoaded;
    }

    private static void OnFrameStatic(IntPtr userData, IntPtr bgraBase, int width, int height, int bytesPerRow)
    {
        if (userData == IntPtr.Zero || bgraBase == IntPtr.Zero || width <= 0 || height <= 0)
        {
            return;
        }

        GCHandle handle = GCHandle.FromIntPtr(userData);
        if (handle.Target is MacOsAsciiVideoFrameSource self)
        {
            self.OnFrame(bgraBase, width, height, bytesPerRow);
        }
    }

    private void OnFrame(IntPtr bgraBase, int width, int height, int bytesPerRow)
    {
        if (_disposed)
        {
            return;
        }

        int rowBytes = width * 4;
        if (bytesPerRow < rowBytes)
        {
            return;
        }

        try
        {
            var pixels = GC.AllocateUninitializedArray<byte>(rowBytes * height);
            for (int y = 0; y < height; y++)
            {
                Marshal.Copy(bgraBase + (y * bytesPerRow), pixels, y * rowBytes, rowBytes);
            }

            PublishFrame(pixels, width, height);
        }
        catch (Exception ex)
        {
            LogAsciiVideoFrameProcessingFailed(ex);
        }
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

    private void SetWebcamSessionActive(bool active)
    {
        _webcamSessionActive = active;
    }

    private void SetWebcamStarting(bool starting)
    {
        _webcamStarting = starting;
    }
}
