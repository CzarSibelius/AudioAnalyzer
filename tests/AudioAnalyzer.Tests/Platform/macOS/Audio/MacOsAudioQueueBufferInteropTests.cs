using System.Runtime.InteropServices;
using AudioAnalyzer.Platform.macOS.Audio.CoreAudio;
using Xunit;

namespace AudioAnalyzer.Tests.Platform.macOS.Audio;

public sealed class MacOsAudioQueueBufferInteropTests
{
    [Fact]
    public void TryReadAudioPayload_DarwinPaddedLayout_ReturnsEmbeddedPointerAndSize()
    {
        int capacity = 512;
        int headerBeforeAudio = 32;
        int filled = 128;

        IntPtr buf = Marshal.AllocHGlobal(capacity + headerBeforeAudio + 64);
        try
        {
            for (int i = 0; i < capacity + headerBeforeAudio + 64; i++)
            {
                Marshal.WriteByte(buf, i, 0);
            }

            Marshal.WriteInt32(buf, 0, capacity);
            IntPtr audioStart = IntPtr.Add(buf, headerBeforeAudio);
            Marshal.WriteIntPtr(buf, 8, audioStart);
            Marshal.WriteInt32(buf, 16, filled);
            Marshal.WriteByte(audioStart, 0xaa);

            bool ok = MacOsAudioQueueBufferInterop.TryReadAudioPayload(
                buf,
                (uint)capacity,
                out IntPtr data,
                out uint sz);

            Assert.True(ok);
            Assert.Equal(audioStart, data);
            Assert.Equal((uint)filled, sz);
            Assert.Equal(0xaa, Marshal.ReadByte(data));
        }
        finally
        {
            Marshal.FreeHGlobal(buf);
        }
    }

    [Fact]
    public void TryReadAudioPayload_PackedLayout_ReturnsEmbeddedPointerAndSize()
    {
        int capacity = 512;
        int headerBeforeAudio = 24;
        int filled = 64;

        IntPtr buf = Marshal.AllocHGlobal(capacity + headerBeforeAudio + 64);
        try
        {
            for (int i = 0; i < capacity + headerBeforeAudio + 64; i++)
            {
                Marshal.WriteByte(buf, i, 0);
            }

            Marshal.WriteInt32(buf, 0, capacity);
            IntPtr audioStart = IntPtr.Add(buf, headerBeforeAudio);
            Marshal.WriteIntPtr(buf, 4, audioStart);
            Marshal.WriteInt32(buf, 12, filled);
            Marshal.WriteByte(audioStart, 0xbb);

            bool ok = MacOsAudioQueueBufferInterop.TryReadAudioPayload(
                buf,
                (uint)capacity,
                out IntPtr data,
                out uint sz);

            Assert.True(ok);
            Assert.Equal(audioStart, data);
            Assert.Equal((uint)filled, sz);
            Assert.Equal(0xbb, Marshal.ReadByte(data));
        }
        finally
        {
            Marshal.FreeHGlobal(buf);
        }
    }

    [Fact]
    public void TryReadAudioPayload_NonEmbeddedPointer_ReturnsFalse()
    {
        IntPtr isolated = Marshal.AllocHGlobal(64);
        IntPtr buf = Marshal.AllocHGlobal(256);
        try
        {
            for (int i = 0; i < 256; i++)
            {
                Marshal.WriteByte(buf, i, 0);
            }

            Marshal.WriteInt32(buf, 0, 512);
            Marshal.WriteIntPtr(buf, 8, isolated);
            Marshal.WriteInt32(buf, 16, 64);

            bool ok = MacOsAudioQueueBufferInterop.TryReadAudioPayload(buf, 512, out _, out _);

            Assert.False(ok);
        }
        finally
        {
            Marshal.FreeHGlobal(buf);
            Marshal.FreeHGlobal(isolated);
        }
    }

    [Fact]
    public void IsPayloadEmbeddedInBufferAllocation_CopyBeyondCapacity_ReturnsFalse()
    {
        IntPtr buf = Marshal.AllocHGlobal(128);
        try
        {
            IntPtr audio = IntPtr.Add(buf, 32);
            Assert.False(MacOsAudioQueueBufferInterop.IsPayloadEmbeddedInBufferAllocation(buf, audio, 500, 400));
        }
        finally
        {
            Marshal.FreeHGlobal(buf);
        }
    }
}
