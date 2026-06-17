using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using AudioAnalyzer.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace AudioAnalyzer.Platform.macOS.Audio.CoreAudioTap;

/// <summary>
/// <see cref="ISystemAudioCapture"/> backed by <c>libaudio_tap_shim.dylib</c> (Core Audio process taps, macOS 14.2+).
/// </summary>
public sealed partial class MacOsSystemAudioCapture : ISystemAudioCapture
{
    private static readonly MacOsAudioTapShimNative.AudioTapPcmCallback s_pcmCallback = OnPcmStatic;

    private readonly AudioCaptureOptions _options;
    private readonly ILogger<MacOsSystemAudioCapture> _logger;
    private readonly object _lock = new();
    private readonly Channel<byte[]> _channel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(8)
    {
        FullMode = BoundedChannelFullMode.DropOldest,
        SingleReader = true,
        SingleWriter = true,
    });

    private GCHandle _selfHandle;
    private IntPtr _pinnedProcessIds;
    private IntPtr _pinnedDeviceUid;
    private bool _started;
    private bool _disposed;
    private AudioCaptureFormat _format = new(48000, 2, 32, true);
    private long _diagChunkCount;

    /// <summary>Raised on the native audio I/O thread when a PCM chunk is available.</summary>
    internal event Action<byte[], AudioCaptureFormat>? PcmChunkAvailable;

    /// <summary>Initializes a new instance of the <see cref="MacOsSystemAudioCapture"/> class.</summary>
    public MacOsSystemAudioCapture(AudioCaptureOptions options, ILogger<MacOsSystemAudioCapture> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public AudioCaptureFormat Format
    {
        get
        {
            lock (_lock)
            {
                return _format;
            }
        }
    }

    /// <inheritdoc />
    public void Start()
    {
        if (!OperatingSystem.IsMacOS())
        {
            throw new PlatformNotSupportedException("Core Audio process taps are only available on macOS.");
        }

        if (!MacOsAudioTapShimNative.IsLibraryLoaded && !TryProbeLibrary())
        {
            throw new InvalidOperationException(
                "libaudio_tap_shim.dylib was not found next to the application. Build native/audio-tap-shim and copy the dylib to the output directory.");
        }

        if (MacOsAudioTapShimNative.AudioTapIsSupported() == 0)
        {
            throw new PlatformNotSupportedException("Core Audio process taps require macOS 14.2 or later.");
        }

        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_started)
            {
                return;
            }

            _selfHandle = GCHandle.Alloc(this);
            MacOsAudioTapShimNative.AudioTapConfig config = BuildConfigLocked();
            IntPtr errorBuffer = Marshal.AllocHGlobal(512);
            bool startSucceeded = false;
            try
            {
                int result = MacOsAudioTapShimNative.AudioTapStart(
                    ref config,
                    s_pcmCallback,
                    GCHandle.ToIntPtr(_selfHandle),
                    errorBuffer,
                    (UIntPtr)512);

                if (result != 0)
                {
                    string message = Marshal.PtrToStringUTF8(errorBuffer) ?? "audio_tap_start failed.";
                    LogStartFailed(message);
                    throw new InvalidOperationException(message);
                }

                startSucceeded = true;
            }
            finally
            {
                Marshal.FreeHGlobal(errorBuffer);
                FreePinnedConfigLocked();

                // The self-handle is a strong GCHandle kept alive only while the native tap can call
                // back. On failure the native side is not running, so free it here to avoid leaking
                // this instance (and its bounded channel) on every failed start/retry.
                if (!startSucceeded)
                {
                    FreeSelfHandleLocked();
                }
            }

            _started = true;
            LogCaptureStarted(_format.SampleRate, _format.Channels);
        }
    }

    /// <inheritdoc />
    public void StopCapture()
    {
        lock (_lock)
        {
            if (!_started)
            {
                return;
            }

            _started = false;
        }

        // AudioTapStop() -> AudioDeviceStop blocks until the in-flight IOProc returns, and that IOProc
        // calls back synchronously into OnPcm (which contends for _lock). Holding _lock across the native
        // stop would therefore deadlock, so it must run with no lock held.
        MacOsAudioTapShimNative.AudioTapStop();

        lock (_lock)
        {
            FreeSelfHandleLocked();
            _channel.Writer.TryComplete();
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ReadOnlyMemory<byte>> CaptureAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!_started)
        {
            Start();
        }

        await foreach (byte[] chunk in _channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return chunk;
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        bool stopNative;
        lock (_lock)
        {
            if (_disposed)
            {
                return ValueTask.CompletedTask;
            }

            _disposed = true;
            stopNative = _started;
            _started = false;
        }

        // Stop the native tap with no lock held: AudioTapStop() blocks until the in-flight IOProc
        // returns, and that IOProc calls back synchronously into OnPcm which contends for _lock.
        if (stopNative)
        {
            MacOsAudioTapShimNative.AudioTapStop();
        }

        lock (_lock)
        {
            FreeSelfHandleLocked();
            FreePinnedConfigLocked();
            _channel.Writer.TryComplete();
        }

        return ValueTask.CompletedTask;
    }

    private static bool TryProbeLibrary()
    {
        try
        {
            _ = MacOsAudioTapShimNative.AudioTapIsSupported();
            return MacOsAudioTapShimNative.IsLibraryLoaded;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
    }

    private MacOsAudioTapShimNative.AudioTapConfig BuildConfigLocked()
    {
        int[]? pids = _options.ProcessIds;
        if (pids is { Length: > 0 })
        {
            _pinnedProcessIds = Marshal.AllocHGlobal(pids.Length * sizeof(int));
            Marshal.Copy(pids, 0, _pinnedProcessIds, pids.Length);
        }

        if (!string.IsNullOrEmpty(_options.DeviceUid))
        {
            _pinnedDeviceUid = Marshal.StringToCoTaskMemUTF8(_options.DeviceUid);
        }

        return new MacOsAudioTapShimNative.AudioTapConfig
        {
            CaptureAllProcesses = _options.CaptureAllProcesses ? 1 : 0,
            ProcessIds = _pinnedProcessIds,
            ProcessIdCount = pids?.Length ?? 0,
            Mono = _options.Mono ? 1 : 0,
            SampleRate = _options.SampleRate,
            DeviceUid = _pinnedDeviceUid,
            StreamIndex = _options.StreamIndex,
        };
    }

    private void FreePinnedConfigLocked()
    {
        if (_pinnedProcessIds != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_pinnedProcessIds);
            _pinnedProcessIds = IntPtr.Zero;
        }

        if (_pinnedDeviceUid != IntPtr.Zero)
        {
            Marshal.FreeCoTaskMem(_pinnedDeviceUid);
            _pinnedDeviceUid = IntPtr.Zero;
        }
    }

    private void FreeSelfHandleLocked()
    {
        if (_selfHandle.IsAllocated)
        {
            _selfHandle.Free();
        }
    }

    private static float ComputePeak(byte[] data, AudioCaptureFormat format)
    {
        float peak = 0f;
        if (format.IsFloat && format.BitsPerSample == 32)
        {
            ReadOnlySpan<float> samples = MemoryMarshal.Cast<byte, float>(data);
            foreach (float sample in samples)
            {
                float abs = Math.Abs(sample);
                if (abs > peak)
                {
                    peak = abs;
                }
            }

            return peak;
        }

        if (format.BitsPerSample == 16)
        {
            ReadOnlySpan<short> samples = MemoryMarshal.Cast<byte, short>(data);
            foreach (short sample in samples)
            {
                float abs = Math.Abs(sample / 32768f);
                if (abs > peak)
                {
                    peak = abs;
                }
            }
        }

        return peak;
    }

    private static void OnPcmStatic(IntPtr userData, IntPtr buffer, uint byteCount, IntPtr formatPtr)
    {
        if (userData == IntPtr.Zero || buffer == IntPtr.Zero || byteCount == 0)
        {
            return;
        }

        GCHandle handle = GCHandle.FromIntPtr(userData);
        if (handle.Target is MacOsSystemAudioCapture self)
        {
            self.OnPcm(buffer, byteCount, formatPtr);
        }
    }

    private void OnPcm(IntPtr buffer, uint byteCount, IntPtr formatPtr)
    {
        if (formatPtr != IntPtr.Zero)
        {
            MacOsAudioTapShimNative.AudioTapFormat nativeFormat =
                Marshal.PtrToStructure<MacOsAudioTapShimNative.AudioTapFormat>(formatPtr);
            lock (_lock)
            {
                _format = new AudioCaptureFormat(
                    nativeFormat.SampleRate,
                    (int)nativeFormat.Channels,
                    (int)nativeFormat.BitsPerSample,
                    nativeFormat.IsFloat != 0);
            }
        }

        // Copy native PCM straight into the chunk handed to consumers; no intermediate pooled buffer.
        byte[] copy = new byte[byteCount];
        Marshal.Copy(buffer, copy, 0, (int)byteCount);

        AudioCaptureFormat format;
        Action<byte[], AudioCaptureFormat>? handler;
        lock (_lock)
        {
            format = _format;
            handler = PcmChunkAvailable;
        }

        long chunkIndex = Interlocked.Increment(ref _diagChunkCount);
        if (chunkIndex == 1 || chunkIndex % 100 == 0)
        {
            float peak = ComputePeak(copy, format);
            LogPcmActivity(chunkIndex, byteCount, peak, format.IsFloat, format.BitsPerSample, format.Channels);
        }

        handler?.Invoke(copy, format);
        _channel.Writer.TryWrite(copy);
    }
}
