using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Platform.macOS.Audio.CoreAudio;
using AudioAnalyzer.Platform.macOS.Audio.CoreAudioTap;
using Microsoft.Extensions.Logging;

namespace AudioAnalyzer.Platform.macOS.Audio;

/// <summary>
/// System/desktop audio via Core Audio process taps (<see cref="CrossPlatformAudioDeviceIds.MacOsCoreAudioTapSystemAudio"/>).
/// Requires macOS 14.2+ and <c>libaudio_tap_shim.dylib</c>; uses System Audio Recording consent (NSAudioCaptureUsageDescription).
/// </summary>
public sealed partial class MacOsCoreAudioTapAudioInput : IAudioInput
{
    private readonly ILogger<MacOsCoreAudioTapAudioInput> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly AudioCaptureOptions _options;
    private readonly object _lock = new();

    private MacOsSystemAudioCapture? _capture;
    private AudioFormat _deliveryFormat = null!;
    private bool _running;
    private bool _disposed;

    /// <summary>Initializes capture with default global stereo tap options.</summary>
    public MacOsCoreAudioTapAudioInput(ILoggerFactory loggerFactory)
        : this(loggerFactory, new AudioCaptureOptions())
    {
    }

    /// <summary>Initializes capture with the given tap configuration.</summary>
    public MacOsCoreAudioTapAudioInput(ILoggerFactory loggerFactory, AudioCaptureOptions options)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<MacOsCoreAudioTapAudioInput>();
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

        MacOsSystemAudioCapture? failedCapture = null;
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            if (_running && _capture != null)
            {
                return;
            }

            if (!MacOsCoreAudioTapAvailability.IsCaptureReady)
            {
                LogTapUnavailable();
                return;
            }

            try
            {
                _capture = new MacOsSystemAudioCapture(
                    _options,
                    _loggerFactory.CreateLogger<MacOsSystemAudioCapture>());
                _capture.PcmChunkAvailable += OnPcmChunk;
                _capture.Start();
                _deliveryFormat = new AudioFormat
                {
                    SampleRate = (int)Math.Round(_capture.Format.SampleRate),
                    BitsPerSample = 32,
                    Channels = Math.Clamp(_capture.Format.Channels, 1, 2),
                };
                _running = true;
            }
            catch (Exception ex)
            {
                LogTapStartFailed(ex);
                failedCapture = DetachCaptureLocked();
            }
        }

        // Dispose outside _lock: the capture's native stop blocks until the in-flight IOProc returns,
        // and that IOProc calls OnPcmChunk which contends for _lock.
        DisposeCapture(failedCapture);
    }

    /// <inheritdoc />
    public void StopCapture()
    {
        MacOsSystemAudioCapture? capture;
        lock (_lock)
        {
            _running = false;
            capture = DetachCaptureLocked();
        }

        DisposeCapture(capture);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        MacOsSystemAudioCapture? capture;
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _running = false;
            capture = DetachCaptureLocked();
        }

        DisposeCapture(capture);
        GC.SuppressFinalize(this);
    }

    private void OnPcmChunk(byte[] buffer, AudioCaptureFormat format)
    {
        EventHandler<AudioDataAvailableEventArgs>? handler;
        AudioFormat delivery;
        lock (_lock)
        {
            if (_disposed || !_running)
            {
                return;
            }

            handler = DataAvailable;
            delivery = _deliveryFormat;
        }

        if (handler == null)
        {
            return;
        }

        int channels = Math.Clamp(format.Channels, 1, 2);
        int frameBytes = channels * (format.IsFloat ? sizeof(float) : Math.Max(1, format.BitsPerSample / 8));
        if (frameBytes <= 0)
        {
            return;
        }

        uint formatFlags = format.IsFloat ? MacOsCoreAudioConstants.kAudioFormatFlagIsFloat : 0u;
        byte[] payload = MacOsPcmFloatNormalizer.ToInterleavedFloat32LittleEndian(
            buffer.AsSpan(),
            frameBytes,
            channels,
            format.IsFloat,
            isBigEndian: false,
            formatFlags,
            (uint)format.BitsPerSample);

        handler.Invoke(this, new AudioDataAvailableEventArgs
        {
            Buffer = payload,
            BytesRecorded = payload.Length,
            Format = delivery,
        });
    }

    private MacOsSystemAudioCapture? DetachCaptureLocked()
    {
        MacOsSystemAudioCapture? capture = _capture;
        if (capture != null)
        {
            capture.PcmChunkAvailable -= OnPcmChunk;
            _capture = null;
        }

        return capture;
    }

    private static void DisposeCapture(MacOsSystemAudioCapture? capture)
    {
        // DisposeAsync completes synchronously (no awaited work); it performs the native tap stop and
        // releases the GCHandle / channel deterministically. Must be called with _lock NOT held.
        // AsTask() avoids CA2012 (direct GetResult on a ValueTask).
        capture?.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

}
