using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AudioAnalyzer.Platform.macOS.Audio.CoreAudio;

namespace AudioAnalyzer.Platform.macOS.Audio;

/// <summary>
/// Converts Core Audio interleaved PCM (integer or float) to IEEE float32 so the application analysis path
/// (<see cref="AudioAnalyzer.Application.AnalysisEngine"/>) receives samples it understands (16- and 32-bit only today).
/// </summary>
internal static class MacOsPcmFloatNormalizer
{
    /// <summary>
    /// True when the buffer is already little-endian float32 interleaved PCM matching <paramref name="frameBytes"/>.
    /// </summary>
    internal static bool IsAlreadyFloat32LittleEndian(
        bool isFloat,
        bool isBigEndian,
        int frameBytes,
        int channels)
    {
        return isFloat && !isBigEndian && channels > 0 && frameBytes == channels * sizeof(float);
    }

    /// <summary>
    /// Converts interleaved PCM to float32 interleaved PCM (new array). Pass-through copies when already float32 LE.
    /// </summary>
    internal static byte[] ToInterleavedFloat32LittleEndian(
        ReadOnlySpan<byte> pcm,
        int frameBytes,
        int channels,
        bool isFloat,
        bool isBigEndian,
        uint formatFlags,
        uint bitsPerChannel)
    {
        if (frameBytes <= 0 || channels <= 0 || pcm.Length == 0)
        {
            return Array.Empty<byte>();
        }

        if (pcm.Length % frameBytes != 0)
        {
            int usable = pcm.Length - (pcm.Length % frameBytes);
            if (usable <= 0)
            {
                return Array.Empty<byte>();
            }

            pcm = pcm[..usable];
        }

        if (IsAlreadyFloat32LittleEndian(isFloat, isBigEndian, frameBytes, channels))
        {
            return pcm.ToArray();
        }

        int bytesPerChannel = frameBytes / channels;
        if (bytesPerChannel <= 0 || bytesPerChannel * channels != frameBytes)
        {
            return Array.Empty<byte>();
        }

        int frames = pcm.Length / frameBytes;
        byte[] dst = new byte[frames * channels * sizeof(float)];
        Span<float> dstF = MemoryMarshal.Cast<byte, float>(dst.AsSpan());

        int di = 0;
        for (int f = 0; f < frames; f++)
        {
            ReadOnlySpan<byte> frame = pcm.Slice(f * frameBytes, frameBytes);
            for (int c = 0; c < channels; c++)
            {
                ReadOnlySpan<byte> ch = frame.Slice(c * bytesPerChannel, bytesPerChannel);
                dstF[di++] = ReadSampleAsFloat(ch, isFloat, isBigEndian, formatFlags, bitsPerChannel);
            }
        }

        return dst;
    }

    private static float ReadSampleAsFloat(
        ReadOnlySpan<byte> channelBytes,
        bool isFloat,
        bool isBigEndian,
        uint formatFlags,
        uint bitsPerChannel)
    {
        if (isFloat)
        {
            if (channelBytes.Length >= sizeof(float))
            {
                uint u = ReadUInt32LittleEndianFromSpan(channelBytes[..sizeof(float)]);
                if (isBigEndian)
                {
                    u = BinaryPrimitives.ReverseEndianness(u);
                }

                return BitConverter.UInt32BitsToSingle(u);
            }

            return 0;
        }

        bool alignedHigh = (formatFlags & MacOsCoreAudioConstants.kAudioFormatFlagIsAlignedHigh) != 0;

        return channelBytes.Length switch
        {
            2 => ReadInt16AsFloat(channelBytes, isBigEndian),
            3 => ReadPackedInt24AsFloat(channelBytes, isBigEndian),
            >= 4 => ReadInt32AsFloat(channelBytes[..4], isBigEndian, bitsPerChannel, alignedHigh),
            _ => 0
        };
    }

    private static float ReadInt16AsFloat(ReadOnlySpan<byte> b2, bool bigEndian)
    {
        short s = bigEndian ? BinaryPrimitives.ReadInt16BigEndian(b2) : BinaryPrimitives.ReadInt16LittleEndian(b2);
        return IntegerToFloat(s, 16);
    }

    private static float ReadPackedInt24AsFloat(ReadOnlySpan<byte> b3, bool bigEndian)
    {
        int v = bigEndian
            ? (b3[0] << 16) | (b3[1] << 8) | b3[2]
            : (b3[2] << 16) | (b3[1] << 8) | b3[0];
        if ((v & 0x800000) != 0)
        {
            v |= unchecked((int)0xff000000);
        }

        return IntegerToFloat(v, 24);
    }

    private static float ReadInt32AsFloat(
        ReadOnlySpan<byte> b4,
        bool bigEndian,
        uint bitsPerChannel,
        bool alignedHigh)
    {
        int raw = bigEndian ? BinaryPrimitives.ReadInt32BigEndian(b4) : BinaryPrimitives.ReadInt32LittleEndian(b4);
        int bits = bitsPerChannel is >= 1 and <= 32 ? (int)bitsPerChannel : 32;
        int v;
        if (bits < 32)
        {
            int shift = 32 - bits;
            v = alignedHigh ? raw >> shift : (raw << shift) >> shift;
        }
        else
        {
            v = raw;
        }

        return IntegerToFloat(v, bits);
    }

    private static float IntegerToFloat(int v, int bits)
    {
        if (bits < 2 || bits > 32)
        {
            return 0;
        }

        float scale = bits >= 32 ? 1f / 2147483648f : 1f / (float)(1L << (bits - 1));
        return Math.Clamp(v * scale, -1f, 1f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ReadUInt32LittleEndianFromSpan(ReadOnlySpan<byte> b4) =>
        (uint)(b4[0] | (b4[1] << 8) | (b4[2] << 16) | (b4[3] << 24));
}
