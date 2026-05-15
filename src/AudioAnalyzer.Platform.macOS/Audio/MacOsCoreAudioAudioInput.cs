using System.Runtime.InteropServices;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Platform.macOS.Audio.CoreAudio;
using Microsoft.Extensions.Logging;

namespace AudioAnalyzer.Platform.macOS.Audio;

/// <summary>Core Audio Audio Queue capture feeding <see cref="IAudioInput"/> with PCM compatible with the analysis pipeline.</summary>
public sealed partial class MacOsCoreAudioAudioInput : IAudioInput
{
    private static readonly MacOsCoreAudioInterop.AudioQueueInputCallback s_callback = OnRecordedStatic;

    private readonly string _deviceUid;
    private readonly ILogger<MacOsCoreAudioAudioInput> _logger;
    private readonly object _lock = new();

    private GCHandle _selfHandle;
    private IntPtr _queue;
    private readonly List<IntPtr> _buffers = new();
    private AudioStreamBasicDescription _hardwareFormat;
    private AudioFormat _deliveryFormat = null!;
    private byte[] _deliveryScratch = Array.Empty<byte>();
    private bool _opened;
    private bool _running;
    private bool _disposed;
    private uint _allocatedBufferCapacity;

    /// <summary>Initializes capture for the Core Audio device UID (resolved at <see cref="Start"/>).</summary>
    public MacOsCoreAudioAudioInput(string deviceUid, ILogger<MacOsCoreAudioAudioInput> logger)
    {
        _deviceUid = deviceUid ?? throw new ArgumentNullException(nameof(deviceUid));
        _logger = logger;
    }

    /// <inheritdoc />
    public event EventHandler<AudioDataAvailableEventArgs>? DataAvailable;

    /// <inheritdoc />
    public void Start()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            if (_running && _queue != IntPtr.Zero)
            {
                return;
            }

            if (!_opened)
            {
                OpenQueueLocked();
            }

            int status = MacOsCoreAudioInterop.AudioQueueStart(_queue, IntPtr.Zero);
            if (status != MacOsCoreAudioConstants.noErr)
            {
                LogAudioQueueStartFailed(status);
                ThrowOs(status, "AudioQueueStart");
            }

            _running = true;
        }
    }

    /// <inheritdoc />
    public void StopCapture()
    {
        IntPtr queueCopy;
        lock (_lock)
        {
            queueCopy = _queue;
            if (queueCopy == IntPtr.Zero)
            {
                return;
            }

            _running = false;
        }

        // Must not hold _lock: AudioQueueStop waits for the input callback, which acquires _lock in OnRecorded.
        _ = MacOsCoreAudioInterop.AudioQueueStop(queueCopy, immediate: 1);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        IntPtr queueCopy;
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _running = false;
            queueCopy = _queue;
            _queue = IntPtr.Zero;
        }

        if (queueCopy != IntPtr.Zero)
        {
            _ = MacOsCoreAudioInterop.AudioQueueStop(queueCopy, immediate: 1);
            _ = MacOsCoreAudioInterop.AudioQueueDispose(queueCopy, immediate: 1);
        }

        lock (_lock)
        {
            _buffers.Clear();
            FreeSelfHandle();
            _opened = false;
            GC.SuppressFinalize(this);
        }
    }

    private void OpenQueueLocked()
    {
        if (!MacOsCoreAudioDeviceQueries.TryFindDeviceIdByUid(_deviceUid, _logger, out uint deviceId))
        {
            throw new InvalidOperationException($"Core Audio device UID not found: {_deviceUid}");
        }

        if (!MacOsCoreAudioDeviceQueries.TryReadStreamBasicDescription(deviceId, _logger, out _hardwareFormat))
        {
            throw new InvalidOperationException($"Could not read Core Audio input format for UID {_deviceUid}.");
        }

        _deliveryFormat = BuildFloat32DeliveryFormat(_hardwareFormat);

        _selfHandle = GCHandle.Alloc(this);
        int status = MacOsCoreAudioInterop.AudioQueueNewInput(
            ref _hardwareFormat,
            s_callback,
            GCHandle.ToIntPtr(_selfHandle),
            IntPtr.Zero,
            IntPtr.Zero,
            0,
            out _queue);

        if (status != MacOsCoreAudioConstants.noErr || _queue == IntPtr.Zero)
        {
            FreeSelfHandle();
            LogAudioQueueNewInputFailed(status);
            ThrowOs(status, "AudioQueueNewInput");
        }

        uint bpf = Math.Max(_hardwareFormat.mBytesPerFrame, 1);
        uint bytesPerBuffer = Math.Max(bpf * 4096, bpf * 512);
        _allocatedBufferCapacity = bytesPerBuffer;

        // Allocate buffers before CurrentDevice / ChannelLayout — some macOS builds reject those properties until buffers exist.
        for (int i = 0; i < 3; i++)
        {
            status = MacOsCoreAudioInterop.AudioQueueAllocateBuffer(_queue, bytesPerBuffer, out IntPtr bufRef);
            if (status != MacOsCoreAudioConstants.noErr)
            {
                LogAllocateBufferFailed(status);
                TearDownQueueLocked();
                ThrowOs(status, "AudioQueueAllocateBuffer");
            }

            _buffers.Add(bufRef);
        }

        TryBindCurrentDevice(deviceId);
        TrySetChannelLayout();

        foreach (IntPtr bufRef in _buffers)
        {
            status = MacOsCoreAudioInterop.AudioQueueEnqueueBuffer(_queue, bufRef, 0, IntPtr.Zero);
            if (status != MacOsCoreAudioConstants.noErr)
            {
                LogEnqueueBufferFailed(status);
                TearDownQueueLocked();
                ThrowOs(status, "AudioQueueEnqueueBuffer");
            }
        }

        _opened = true;

        LogCaptureOpened(
            _deviceUid,
            _hardwareFormat.mSampleRate,
            _hardwareFormat.mChannelsPerFrame,
            _hardwareFormat.mBitsPerChannel,
            _hardwareFormat.mFormatFlags);
    }

    /// <summary>Binds the queue device using Core Audio’s CFString UID when possible (SDL-style); falls back to UTF-8 rebuild.</summary>
    private void TryBindCurrentDevice(uint audioDeviceId)
    {
        IntPtr holder = Marshal.AllocHGlobal(IntPtr.Size);
        try
        {
            IntPtr cfUid = IntPtr.Zero;
            bool copied =
                MacOsCoreAudioDeviceQueries.TryCopyDeviceUidCfString(
                    audioDeviceId,
                    MacOsCoreAudioConstants.kAudioDevicePropertyScopeInput,
                    out cfUid)
                || MacOsCoreAudioDeviceQueries.TryCopyDeviceUidCfString(
                    audioDeviceId,
                    MacOsCoreAudioConstants.kAudioObjectPropertyScopeGlobal,
                    out cfUid);

            if (!copied || cfUid == IntPtr.Zero)
            {
                cfUid = MacOsCfInterop.CreateCfStringUtf8(_deviceUid);
            }

            try
            {
                Marshal.WriteIntPtr(holder, cfUid);
                int status = MacOsCoreAudioInterop.AudioQueueSetProperty(
                    _queue,
                    MacOsCoreAudioInterop.kAudioQueueProperty_CurrentDevice,
                    holder,
                    (uint)IntPtr.Size);

                if (status != MacOsCoreAudioConstants.noErr)
                {
                    LogCurrentDevicePropertyFailed(_deviceUid, status);
                    if (status == -66684)
                    {
                        LogMicrophonePermissionHint(status);
                    }
                }
            }
            finally
            {
                MacOsCfInterop.CFRelease(cfUid);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(holder);
        }
    }

    private void TrySetChannelLayout()
    {
        uint propSize = 0;
        int getSize = MacOsCoreAudioInterop.AudioQueueGetPropertySize(
            _queue,
            MacOsCoreAudioInterop.kAudioQueueProperty_ChannelLayout,
            ref propSize);

        if (getSize != MacOsCoreAudioConstants.noErr || propSize == 0 || propSize > 65536)
        {
            return;
        }

        uint tag = _hardwareFormat.mChannelsPerFrame <= 1
            ? MacOsCoreAudioConstants.kAudioChannelLayoutTag_Mono
            : MacOsCoreAudioConstants.kAudioChannelLayoutTag_Stereo;

        IntPtr pLayout = Marshal.AllocHGlobal((int)propSize);
        try
        {
            for (int b = 0; b < (int)propSize; b++)
            {
                Marshal.WriteByte(pLayout, b, 0);
            }

            Marshal.WriteInt32(pLayout, 0, unchecked((int)tag));

            int status = MacOsCoreAudioInterop.AudioQueueSetProperty(
                _queue,
                MacOsCoreAudioInterop.kAudioQueueProperty_ChannelLayout,
                pLayout,
                propSize);

            if (status != MacOsCoreAudioConstants.noErr)
            {
                LogChannelLayoutFailed(status);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(pLayout);
        }
    }

    private void TearDownQueueLocked()
    {
        if (_queue != IntPtr.Zero)
        {
            _ = MacOsCoreAudioInterop.AudioQueueDispose(_queue, immediate: 1);
            _queue = IntPtr.Zero;
        }

        _buffers.Clear();
        FreeSelfHandle();
        _opened = false;
        _running = false;
    }

    private void FreeSelfHandle()
    {
        if (_selfHandle.IsAllocated)
        {
            _selfHandle.Free();
        }
    }

    private static void ThrowOs(int status, string step) =>
        throw new InvalidOperationException($"{step} failed (OSStatus={status}).");

    private static AudioFormat BuildFloat32DeliveryFormat(AudioStreamBasicDescription asbd) =>
        new()
        {
            SampleRate = (int)Math.Round(asbd.mSampleRate),
            BitsPerSample = 32,
            Channels = (int)Math.Clamp(asbd.mChannelsPerFrame, 1, 2)
        };

    private static void OnRecordedStatic(
        IntPtr userData,
        IntPtr audioQueue,
        IntPtr bufferRef,
        IntPtr startTime,
        uint numPackets,
        IntPtr packetDesc)
    {
        _ = startTime;
        _ = packetDesc;

        GCHandle handle = GCHandle.FromIntPtr(userData);
        if (handle.Target is MacOsCoreAudioAudioInput self)
        {
            self.OnRecorded(audioQueue, bufferRef, numPackets);
        }
    }

    private void OnRecorded(IntPtr audioQueue, IntPtr bufferRef, uint numPackets)
    {
        uint bpfValidation = Math.Max(_hardwareFormat.mBytesPerFrame, 1);
        bool mustEnqueueBuffer = true;
        byte[]? payload = null;
        AudioFormat? formatForDelivery = null;

        try
        {
            lock (_lock)
            {
                if (_disposed || audioQueue != _queue || _queue == IntPtr.Zero)
                {
                    mustEnqueueBuffer = false;
                    return;
                }

                if (!MacOsAudioQueueBufferInterop.TryReadAudioPayload(
                        bufferRef,
                        _allocatedBufferCapacity,
                        out IntPtr dataPtr,
                        out uint byteSize)
                    || byteSize == 0
                    || dataPtr == IntPtr.Zero)
                {
                    return;
                }

                int framesFromPackets = _hardwareFormat.mFramesPerPacket > 0
                    ? (int)(numPackets * _hardwareFormat.mFramesPerPacket)
                    : (int)numPackets;

                int expectedBytes = framesFromPackets * (int)bpfValidation;
                int bytesUsed = (int)Math.Min(byteSize, Math.Max(expectedBytes, 0));
                if (bytesUsed <= 0)
                {
                    bytesUsed = (int)byteSize;
                }

                bytesUsed = Math.Min(bytesUsed, (int)byteSize);
                bytesUsed = Math.Min(bytesUsed, (int)_allocatedBufferCapacity);

                if (bytesUsed <= 0)
                {
                    return;
                }

                // Marshal.Copy while holding _lock so teardown cannot invalidate buffer metadata vs. native dispose ordering.
                InterleavedPcmChunk interleaved = MaterializePcm(dataPtr, bytesUsed);
                payload = MacOsPcmFloatNormalizer.ToInterleavedFloat32LittleEndian(
                    interleaved.Buffer.AsSpan(),
                    interleaved.FrameBytes,
                    interleaved.Channels,
                    interleaved.IsFloat,
                    interleaved.IsBigEndian,
                    interleaved.FormatFlags,
                    interleaved.BitsPerChannel);
                formatForDelivery = _deliveryFormat;
            }

            if (payload != null && formatForDelivery != null)
            {
                DataAvailable?.Invoke(this, new AudioDataAvailableEventArgs
                {
                    Buffer = payload,
                    BytesRecorded = payload.Length,
                    Format = formatForDelivery
                });
            }
        }
        finally
        {
            if (mustEnqueueBuffer)
            {
                _ = MacOsCoreAudioInterop.AudioQueueEnqueueBuffer(audioQueue, bufferRef, 0, IntPtr.Zero);
            }
        }
    }

    private InterleavedPcmChunk MaterializePcm(IntPtr dataPtr, int bytesRecorded)
    {
        bool nonInterleaved =
            (_hardwareFormat.mFormatFlags & MacOsCoreAudioConstants.kAudioFormatFlagIsNonInterleaved) != 0;
        bool isFloat = (_hardwareFormat.mFormatFlags & MacOsCoreAudioConstants.kAudioFormatFlagIsFloat) != 0;
        bool bigEndian = (_hardwareFormat.mFormatFlags & MacOsCoreAudioConstants.kAudioFormatFlagIsBigEndian) != 0;
        uint hwChannels = _hardwareFormat.mChannelsPerFrame;
        uint bitsPerChannel = _hardwareFormat.mBitsPerChannel;
        uint formatFlags = _hardwareFormat.mFormatFlags;

        if (!nonInterleaved && hwChannels > 2)
        {
            byte[] buf = TakeFirstTwoInterleavedChannels(dataPtr, bytesRecorded, hwChannels, isFloat);
            int bps = BytesPerSampleInterleaved(isFloat, bitsPerChannel);
            return new InterleavedPcmChunk(buf, 2 * bps, 2, isFloat, bigEndian, formatFlags, bitsPerChannel);
        }

        uint channels = Math.Clamp(hwChannels, 1, 2);

        if (!nonInterleaved || channels < 2)
        {
            byte[] buf = CopyBytes(dataPtr, bytesRecorded);
            return new InterleavedPcmChunk(
                buf,
                (int)_hardwareFormat.mBytesPerFrame,
                (int)channels,
                isFloat,
                bigEndian,
                formatFlags,
                bitsPerChannel);
        }

        int bytesPerSample = BytesPerSampleInterleaved(isFloat, bitsPerChannel);
        if (bytesPerSample <= 0)
        {
            byte[] buf = CopyBytes(dataPtr, bytesRecorded);
            return new InterleavedPcmChunk(
                buf,
                (int)_hardwareFormat.mBytesPerFrame,
                (int)channels,
                isFloat,
                bigEndian,
                formatFlags,
                bitsPerChannel);
        }

        int frames = bytesRecorded / (bytesPerSample * (int)channels);
        if (frames <= 0)
        {
            byte[] buf = CopyBytes(dataPtr, bytesRecorded);
            return new InterleavedPcmChunk(
                buf,
                (int)_hardwareFormat.mBytesPerFrame,
                (int)channels,
                isFloat,
                bigEndian,
                formatFlags,
                bitsPerChannel);
        }

        int outBytes = frames * bytesPerSample * (int)channels;
        byte[] dst = new byte[outBytes];
        ReadOnlySpan<byte> src = CopyBytes(dataPtr, bytesRecorded).AsSpan();

        if (isFloat)
        {
            InterleavePlanarFloatStereo(src, dst.AsSpan(), frames);
        }
        else
        {
            InterleavePlanarIntegerStereo(src, dst.AsSpan(), frames, bytesPerSample);
        }

        return new InterleavedPcmChunk(dst, bytesPerSample * 2, 2, isFloat, bigEndian, formatFlags, bitsPerChannel);
    }

    private byte[] TakeFirstTwoInterleavedChannels(IntPtr dataPtr, int bytesRecorded, uint hwChannels, bool isFloat)
    {
        int bytesPerSample = BytesPerSampleInterleaved(isFloat, _hardwareFormat.mBitsPerChannel);
        if (bytesPerSample <= 0 || hwChannels < 2)
        {
            return CopyBytes(dataPtr, bytesRecorded);
        }

        int frameBytes = (int)(hwChannels * bytesPerSample);
        if (frameBytes <= 0 || bytesRecorded < frameBytes)
        {
            return CopyBytes(dataPtr, bytesRecorded);
        }

        int frames = bytesRecorded / frameBytes;
        int outBytes = frames * 2 * bytesPerSample;
        byte[] dst = new byte[outBytes];
        for (int f = 0; f < frames; f++)
        {
            int srcOff = f * frameBytes;
            int dstOff = f * 2 * bytesPerSample;
            Marshal.Copy(IntPtr.Add(dataPtr, srcOff), dst, dstOff, bytesPerSample);
            Marshal.Copy(IntPtr.Add(dataPtr, srcOff + bytesPerSample), dst, dstOff + bytesPerSample, bytesPerSample);
        }

        return dst;
    }

    private byte[] CopyBytes(IntPtr dataPtr, int byteLength)
    {
        if (_deliveryScratch.Length < byteLength)
        {
            _deliveryScratch = new byte[byteLength];
        }

        Marshal.Copy(dataPtr, _deliveryScratch, 0, byteLength);
        byte[] copy = new byte[byteLength];
        Array.Copy(_deliveryScratch, copy, byteLength);
        return copy;
    }

    private static void InterleavePlanarFloatStereo(ReadOnlySpan<byte> src, Span<byte> dst, int frames)
    {
        var left = MemoryMarshal.Cast<byte, float>(src[..(frames * sizeof(float))]);
        var right = MemoryMarshal.Cast<byte, float>(src.Slice(frames * sizeof(float), frames * sizeof(float)));
        var dstF = MemoryMarshal.Cast<byte, float>(dst);
        for (int i = 0; i < frames; i++)
        {
            dstF[i * 2] = left[i];
            dstF[i * 2 + 1] = right[i];
        }
    }

    private static void InterleavePlanarIntegerStereo(ReadOnlySpan<byte> src, Span<byte> dst, int frames, int bytesPerSample)
    {
        int planeBytes = frames * bytesPerSample;
        ReadOnlySpan<byte> left = src[..planeBytes];
        ReadOnlySpan<byte> right = src.Slice(planeBytes, planeBytes);
        for (int i = 0; i < frames; i++)
        {
            int offset = i * bytesPerSample * 2;
            left.Slice(i * bytesPerSample, bytesPerSample).CopyTo(dst.Slice(offset, bytesPerSample));
            right.Slice(i * bytesPerSample, bytesPerSample).CopyTo(dst.Slice(offset + bytesPerSample, bytesPerSample));
        }
    }

    private readonly record struct InterleavedPcmChunk(
        byte[] Buffer,
        int FrameBytes,
        int Channels,
        bool IsFloat,
        bool IsBigEndian,
        uint FormatFlags,
        uint BitsPerChannel);

    private static int BytesPerSampleInterleaved(bool isFloat, uint bitsPerChannel) =>
        isFloat ? sizeof(float) : Math.Max(1, (int)((bitsPerChannel + 7) / 8));
}
