using System.Runtime.InteropServices;
using AudioAnalyzer.Application.Abstractions;
using AudioToolbox;
using CoreFoundation;
using CoreMedia;
using Foundation;
using Microsoft.Extensions.Logging;
using ScreenCaptureKit;

namespace AudioAnalyzer.Platform.macOS.Audio;

/// <summary>
/// System/desktop audio via ScreenCaptureKit (<see cref="CrossPlatformAudioDeviceIds.MacOsScreenCaptureKitSystemAudio"/>).
/// Requires Screen Recording consent; failures are logged when capture cannot start (no throw from <see cref="Start"/>).
/// </summary>
public sealed partial class MacOsScreenCaptureKitSystemAudioInput : IAudioInput
{
    private readonly ILogger<MacOsScreenCaptureKitSystemAudioInput> _logger;
    private readonly object _lock = new();

    private DispatchQueue? _sampleQueue;
    private MacOsSckStreamOutputHandler? _outputHandler;
    private SCStream? _stream;
    private bool _running;
    private bool _prepared;
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="MacOsScreenCaptureKitSystemAudioInput"/> class.</summary>
    public MacOsScreenCaptureKitSystemAudioInput(ILogger<MacOsScreenCaptureKitSystemAudioInput> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public event EventHandler<AudioDataAvailableEventArgs>? DataAvailable;

    /// <inheritdoc />
    public void Start()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        SCStream? stream;
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            if (!_prepared && !TryPrepareLocked())
            {
                return;
            }

            stream = _stream;
            if (stream == null)
            {
                return;
            }

            if (_running)
            {
                return;
            }
        }

        // IMPORTANT: do not block the calling thread on ScreenCaptureKit native callbacks.
        // The macOS host app is single-process/single-threaded for the console UI; blocking here can prevent it from opening.
        stream.StartCapture(e =>
        {
            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }

                _running = e == null;
            }

            if (e != null)
            {
                LogSckStartFailed(e.LocalizedDescription ?? e.ToString());
            }
        });
    }

    /// <inheritdoc />
    public void StopCapture()
    {
        SCStream? stream;
        lock (_lock)
        {
            stream = _stream;
            _running = false;
        }

        if (stream == null)
        {
            return;
        }

        var done = new ManualResetEventSlim(false);
        stream.StopCapture(_ => done.Set());
        _ = done.Wait(TimeSpan.FromSeconds(15));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        SCStream? stream;
        MacOsSckStreamOutputHandler? handler;
        DispatchQueue? queue;
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _running = false;
            stream = _stream;
            handler = _outputHandler;
            queue = _sampleQueue;
            _stream = null;
            _outputHandler = null;
            _sampleQueue = null;
            _prepared = false;
        }

        if (stream != null)
        {
            if (handler != null)
            {
                _ = stream.RemoveStreamOutput(handler, SCStreamOutputType.Audio, out _);
            }

            var done = new ManualResetEventSlim(false);
            stream.StopCapture(_ => done.Set());
            _ = done.Wait(TimeSpan.FromSeconds(15));
            stream.Dispose();
        }

        handler?.Dispose();
        queue?.Dispose();
        GC.SuppressFinalize(this);
    }

    private bool TryPrepareLocked()
    {
        if (_prepared)
        {
            return _stream != null;
        }

        try
        {
            // Native capture preparation can block on consent / device discovery. Avoid hanging the host process indefinitely.
            Task prepareTask = Task.Run(PrepareCoreAsync);
            const int prepareTimeoutSeconds = 10;
            bool completed = prepareTask.Wait(TimeSpan.FromSeconds(prepareTimeoutSeconds));
            if (!completed)
            {
                LogSckPrepareTimeout(prepareTimeoutSeconds);
                return false;
            }

            prepareTask.GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            LogSckPrepareFailed(ex);
            return false;
        }

        _prepared = _stream != null;
        return _stream != null;
    }

    private async Task PrepareCoreAsync()
    {
        SCShareableContent content = await SCShareableContent.GetShareableContentAsync(false, false)
            .ConfigureAwait(false);

        if (content.Displays.Length == 0)
        {
            LogSckNoDisplays();
            return;
        }

        SCDisplay display = content.Displays[0];
        var filter = new SCContentFilter(display, Array.Empty<SCWindow>(), SCContentFilterOption.Include);
        var configuration = new SCStreamConfiguration
        {
            CapturesAudio = true,
            CaptureMicrophone = false,
            ExcludesCurrentProcessAudio = true,
            SampleRate = (nint)48_000,
            ChannelCount = (nint)2,
        };

        var stream = new SCStream(filter, configuration, new SCStreamDelegate());
        var queue = new DispatchQueue("dev.audioanalyzer.sck.audio");
        var handler = new MacOsSckStreamOutputHandler(OnSampleBuffer);
        if (!stream.AddStreamOutput(handler, SCStreamOutputType.Audio, queue, out NSError? addErr) || addErr != null)
        {
            LogSckAddOutputFailed(addErr?.LocalizedDescription ?? "(unknown)");
            stream.Dispose();
            queue.Dispose();
            handler.Dispose();
            return;
        }

        lock (_lock)
        {
            if (_disposed)
            {
                _ = stream.RemoveStreamOutput(handler, SCStreamOutputType.Audio, out _);
                stream.Dispose();
                queue.Dispose();
                handler.Dispose();
                return;
            }

            _stream = stream;
            _sampleQueue = queue;
            _outputHandler = handler;
        }
    }

    private void OnSampleBuffer(CMSampleBuffer sampleBuffer)
    {
        lock (_lock)
        {
            if (_disposed || !_running)
            {
                return;
            }
        }

        if (!sampleBuffer.IsValid)
        {
            return;
        }

        CMAudioFormatDescription? audioDesc = sampleBuffer.GetAudioFormatDescription();
        if (audioDesc == null)
        {
            return;
        }

        AudioStreamBasicDescription? asbdNullable = audioDesc.AudioStreamBasicDescription;
        if (!asbdNullable.HasValue)
        {
            return;
        }

        AudioStreamBasicDescription asbd = asbdNullable.Value;
        int channels = (int)asbd.ChannelsPerFrame;
        if (channels <= 0 || channels > 32)
        {
            return;
        }
        uint bitsPerChannel = (uint)asbd.BitsPerChannel;
        uint formatFlagsRaw = (uint)asbd.FormatFlags;
        var formatFlags = (AudioFormatFlags)formatFlagsRaw;
        bool isFloat = (formatFlags & AudioFormatFlags.IsFloat) != 0;
        bool isBigEndian = (formatFlags & AudioFormatFlags.IsBigEndian) != 0;
        bool nonInterleaved = (formatFlags & AudioFormatFlags.IsNonInterleaved) != 0;

        nint numSamplesNative = sampleBuffer.NumSamples;
        int numSamples = numSamplesNative <= 0 ? 0 : (numSamplesNative > int.MaxValue ? int.MaxValue : (int)numSamplesNative);
        if (numSamples <= 0)
        {
            return;
        }

        int bytesPerSample = isFloat ? sizeof(float) : Math.Max(1, (int)((bitsPerChannel + 7) / 8));
        long bytesPerBufferLong = (long)numSamples * bytesPerSample * (nonInterleaved ? 1 : channels);
        if (bytesPerBufferLong <= 0 || bytesPerBufferLong > int.MaxValue)
        {
            return;
        }

        int bytesPerBuffer = (int)bytesPerBufferLong;
        int bufferCount = nonInterleaved ? channels : 1;
        var audioBuffers = new AudioBuffers(bufferCount);
        IntPtr[] allocated = new IntPtr[bufferCount];
        try
        {
            for (int b = 0; b < bufferCount; b++)
            {
                allocated[b] = Marshal.AllocHGlobal(bytesPerBuffer);
                audioBuffers.SetData(b, allocated[b], bytesPerBuffer);
            }

            if (sampleBuffer.CopyPCMDataIntoAudioBufferList(0, numSamples, audioBuffers) != CMSampleBufferError.None)
            {
                return;
            }

            if (!TryMaterializeInterleavedPcm(
                    audioBuffers,
                    channels,
                    nonInterleaved,
                    isFloat,
                    bitsPerChannel,
                    out byte[]? raw,
                    out int frameBytes,
                    out int effectiveChannels))
            {
                return;
            }

            byte[] payload = MacOsPcmFloatNormalizer.ToInterleavedFloat32LittleEndian(
                raw.AsSpan(),
                frameBytes,
                effectiveChannels,
                isFloat,
                isBigEndian,
                formatFlagsRaw,
                bitsPerChannel);

            if (payload.Length == 0)
            {
                return;
            }

            if (effectiveChannels > 2)
            {
                payload = TakeFirstTwoFloatChannels(payload, effectiveChannels);
                effectiveChannels = 2;
            }

            int sampleRate = (int)Math.Clamp(Math.Round(asbd.SampleRate), 1, 192_000);
            var deliverFormat = new global::AudioAnalyzer.Application.Abstractions.AudioFormat
            {
                SampleRate = sampleRate,
                BitsPerSample = 32,
                Channels = effectiveChannels,
            };

            DataAvailable?.Invoke(this, new AudioDataAvailableEventArgs
            {
                Buffer = payload,
                BytesRecorded = payload.Length,
                Format = deliverFormat,
            });
        }
        finally
        {
            for (int b = 0; b < allocated.Length; b++)
            {
                if (allocated[b] != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(allocated[b]);
                }
            }
        }
    }

    /// <summary>Produces one interleaved PCM payload per analyzer expectations (mono/stereo; first two channels if N&gt;2 planar).</summary>
    private static bool TryMaterializeInterleavedPcm(
        AudioBuffers buffers,
        int channels,
        bool nonInterleaved,
        bool isFloat,
        uint bitsPerChannel,
        out byte[]? raw,
        out int frameBytes,
        out int effectiveChannels)
    {
        raw = null;
        frameBytes = 0;
        effectiveChannels = 0;
        int bytesPerSample = isFloat
            ? sizeof(float)
            : Math.Max(1, (int)((bitsPerChannel + 7) / 8));

        if (!nonInterleaved)
        {
            if (buffers.Count < 1)
            {
                return false;
            }

            AudioBuffer b0 = buffers[0];
            if (b0.Data == IntPtr.Zero || b0.DataByteSize <= 0)
            {
                return false;
            }

            raw = new byte[b0.DataByteSize];
            Marshal.Copy(b0.Data, raw, 0, b0.DataByteSize);
            frameBytes = channels * bytesPerSample;
            effectiveChannels = channels;
            return frameBytes > 0 && raw.Length % frameBytes == 0;
        }

        // Planar: one buffer per channel.
        if (buffers.Count < channels)
        {
            return false;
        }

        int planesToInterleave = Math.Min(channels, 2);
        AudioBuffer p0 = buffers[0];
        if (p0.Data == IntPtr.Zero || p0.DataByteSize <= 0)
        {
            return false;
        }

        int planeBytes = p0.DataByteSize;
        for (int c = 1; c < planesToInterleave; c++)
        {
            planeBytes = Math.Min(planeBytes, buffers[c].DataByteSize);
        }

        int frames = planeBytes / bytesPerSample;
        if (frames <= 0)
        {
            return false;
        }

        if (planesToInterleave == 1)
        {
            raw = new byte[frames * bytesPerSample];
            Marshal.Copy(p0.Data, raw, 0, raw.Length);
            frameBytes = bytesPerSample;
            effectiveChannels = 1;
            return true;
        }

        byte[] left = new byte[planeBytes];
        byte[] right = new byte[planeBytes];
        Marshal.Copy(buffers[0].Data, left, 0, planeBytes);
        Marshal.Copy(buffers[1].Data, right, 0, planeBytes);

        if (isFloat)
        {
            raw = InterleavePlanarFloatStereo(left, right, frames);
        }
        else
        {
            raw = InterleavePlanarIntegerStereo(left, right, frames, bytesPerSample);
        }

        frameBytes = bytesPerSample * 2;
        effectiveChannels = 2;
        return raw.Length == frames * frameBytes;
    }

    private static byte[] TakeFirstTwoFloatChannels(byte[] interleavedFloatLe, int channelCount)
    {
        int frames = interleavedFloatLe.Length / (channelCount * sizeof(float));
        if (frames <= 0)
        {
            return Array.Empty<byte>();
        }

        byte[] dst = new byte[frames * 2 * sizeof(float)];
        var srcF = MemoryMarshal.Cast<byte, float>(interleavedFloatLe.AsSpan());
        var dstF = MemoryMarshal.Cast<byte, float>(dst.AsSpan());
        for (int i = 0; i < frames; i++)
        {
            int s = i * channelCount;
            dstF[i * 2] = srcF[s];
            dstF[i * 2 + 1] = srcF[s + 1];
        }

        return dst;
    }

    private static byte[] InterleavePlanarFloatStereo(byte[] leftPlane, byte[] rightPlane, int frames)
    {
        byte[] dst = new byte[frames * 2 * sizeof(float)];
        var left = MemoryMarshal.Cast<byte, float>(leftPlane.AsSpan()[..(frames * sizeof(float))]);
        var right = MemoryMarshal.Cast<byte, float>(rightPlane.AsSpan()[..(frames * sizeof(float))]);
        var dstF = MemoryMarshal.Cast<byte, float>(dst.AsSpan());
        for (int i = 0; i < frames; i++)
        {
            dstF[i * 2] = left[i];
            dstF[i * 2 + 1] = right[i];
        }

        return dst;
    }

    private static byte[] InterleavePlanarIntegerStereo(byte[] leftPlane, byte[] rightPlane, int frames, int bytesPerSample)
    {
        int planeBytes = frames * bytesPerSample;
        byte[] dst = new byte[planeBytes * 2];
        for (int i = 0; i < frames; i++)
        {
            int srcOff = i * bytesPerSample;
            int dstOff = i * bytesPerSample * 2;
            Buffer.BlockCopy(leftPlane, srcOff, dst, dstOff, bytesPerSample);
            Buffer.BlockCopy(rightPlane, srcOff, dst, dstOff + bytesPerSample, bytesPerSample);
        }

        return dst;
    }
}
