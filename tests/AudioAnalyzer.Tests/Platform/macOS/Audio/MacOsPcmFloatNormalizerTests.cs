using System.Buffers.Binary;
using System.Runtime.InteropServices;
using AudioAnalyzer.Platform.macOS.Audio;
using AudioAnalyzer.Platform.macOS.Audio.CoreAudio;
using Xunit;

namespace AudioAnalyzer.Tests.Platform.macOS.Audio;

public sealed class MacOsPcmFloatNormalizerTests
{
    [Fact]
    public void ToInterleavedFloat32LittleEndian_Int16Stereo_ProducesNonZeroFloatSamples()
    {
        short l = 16000;
        short r = -8000;
        byte[] pcm = new byte[4];
        BinaryPrimitives.WriteInt16LittleEndian(pcm.AsSpan(0, 2), l);
        BinaryPrimitives.WriteInt16LittleEndian(pcm.AsSpan(2, 2), r);

        byte[] floats = MacOsPcmFloatNormalizer.ToInterleavedFloat32LittleEndian(
            pcm,
            frameBytes: 4,
            channels: 2,
            isFloat: false,
            isBigEndian: false,
            formatFlags: MacOsCoreAudioConstants.kAudioFormatFlagIsSignedInteger,
            bitsPerChannel: 16);

        Assert.Equal(8, floats.Length);
        var f = MemoryMarshal.Cast<byte, float>(floats);
        Assert.InRange(f[0], 0.4f, 0.52f);
        Assert.InRange(f[1], -0.26f, -0.22f);
    }

    [Fact]
    public void ToInterleavedFloat32LittleEndian_Float32Stereo_PassThroughCopies()
    {
        float l = 0.25f;
        float r = -0.5f;
        byte[] pcm = new byte[8];
        Assert.True(BitConverter.TryWriteBytes(pcm.AsSpan(0, 4), l));
        Assert.True(BitConverter.TryWriteBytes(pcm.AsSpan(4, 4), r));

        byte[] floats = MacOsPcmFloatNormalizer.ToInterleavedFloat32LittleEndian(
            pcm,
            frameBytes: 8,
            channels: 2,
            isFloat: true,
            isBigEndian: false,
            formatFlags: MacOsCoreAudioConstants.kAudioFormatFlagIsFloat,
            bitsPerChannel: 32);

        Assert.Equal(pcm.Length, floats.Length);
        Assert.Equal(pcm, floats);
    }

    [Fact]
    public void ToInterleavedFloat32LittleEndian_TrimsPartialTrailingFrame()
    {
        byte[] pcm = new byte[5];
        pcm[0] = 0xff;
        pcm[1] = 0x7f;

        byte[] floats = MacOsPcmFloatNormalizer.ToInterleavedFloat32LittleEndian(
            pcm,
            frameBytes: 4,
            channels: 2,
            isFloat: false,
            isBigEndian: false,
            formatFlags: 0,
            bitsPerChannel: 16);

        Assert.Equal(8, floats.Length);
        var f = MemoryMarshal.Cast<byte, float>(floats);
        Assert.True(f[0] > 0.99f);
    }
}
