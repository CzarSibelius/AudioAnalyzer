using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace AudioAnalyzer.Platform.macOS.Audio.CoreAudio;

/// <summary>
/// Reads payload pointer/size from <c>AudioQueueBuffer</c>. Clang may lay out <c>AudioQueueBuffer</c> with either:
/// packed fields (<c>#pragma pack(4)</c>: pointer at 4, byte size at 12) or inserted padding before <c>mAudioData</c>
/// so the pointer aligns to 8 bytes on 64-bit Darwin (pointer at 8, byte size at 16). Wrong offsets yield bogus pointers and AV on copy.
/// Candidates must pass <see cref="IsPayloadEmbeddedInBufferAllocation"/> so we never <see cref="Marshal.Copy"/> from arbitrary addresses.
/// </summary>
internal static class MacOsAudioQueueBufferInterop
{
    /// <summary>
    /// AudioQueueAllocateBuffer uses one allocation: the struct at <paramref name="bufferRef"/> and PCM bytes reachable from
    /// <c>mAudioData</c> inside that block. Reject pointers outside a plausible embedded window so bogus layouts do not AV.
    /// </summary>
    internal static bool IsPayloadEmbeddedInBufferAllocation(
        IntPtr bufferRef,
        IntPtr audioData,
        uint audioByteSize,
        uint capacityBytes)
    {
        if (bufferRef == IntPtr.Zero || audioData == IntPtr.Zero || capacityBytes == 0 || audioByteSize == 0)
        {
            return false;
        }

        if (audioByteSize > capacityBytes)
        {
            return false;
        }

        long basePtr = bufferRef.ToInt64();
        long payloadPtr = audioData.ToInt64();
        long delta = payloadPtr - basePtr;

        // 64-bit AudioQueueBuffer is typically 32–48 bytes before PCM; extended layouts / packet metadata can push this higher.
        const long minDeltaBytes = 16;
        const long maxDeltaBytes = 2048;
        if (delta < minDeltaBytes || delta > maxDeltaBytes)
        {
            return false;
        }

        long copyEndExclusive = payloadPtr + audioByteSize;
        long allocationEndExclusive = basePtr + delta + capacityBytes;
        return copyEndExclusive <= allocationEndExclusive;
    }

    private static IntPtr ReadPointerLittleEndian(IntPtr bufferRef, int offset)
    {
        Span<byte> bits = stackalloc byte[sizeof(long)];
        for (int i = 0; i < sizeof(long); i++)
        {
            bits[i] = Marshal.ReadByte(bufferRef, offset + i);
        }

        return new IntPtr(BinaryPrimitives.ReadInt64LittleEndian(bits));
    }

    private static bool TryCandidate(
        IntPtr bufferRef,
        uint maxPayload,
        int ptrOffset,
        int sizeOffset,
        out IntPtr audioData,
        out uint audioByteSize)
    {
        audioData = IntPtr.Zero;
        audioByteSize = 0;

        IntPtr p = ReadPointerLittleEndian(bufferRef, ptrOffset);
        uint sz = unchecked((uint)Marshal.ReadInt32(bufferRef, sizeOffset));
        if (p == IntPtr.Zero || sz == 0)
        {
            return false;
        }

        sz = Math.Min(sz, maxPayload);
        if (sz == 0)
        {
            return false;
        }

        uint capacityField = unchecked((uint)Marshal.ReadInt32(bufferRef, 0));
        if (!IsPayloadEmbeddedInBufferAllocation(bufferRef, p, sz, capacityField > 0 ? capacityField : maxPayload))
        {
            return false;
        }

        audioData = p;
        audioByteSize = sz;
        return true;
    }

    /// <summary>Tries to obtain audio bytes metadata from an input callback buffer.</summary>
    internal static bool TryReadAudioPayload(
        IntPtr bufferRef,
        uint allocatedBufferCapacity,
        out IntPtr audioData,
        out uint audioByteSize)
    {
        audioData = IntPtr.Zero;
        audioByteSize = 0;

        uint capField = unchecked((uint)Marshal.ReadInt32(bufferRef, 0));
        uint maxPayload = allocatedBufferCapacity > 0 ? allocatedBufferCapacity : capField;
        if (capField > 0 && maxPayload > capField)
        {
            maxPayload = capField;
        }

        if (maxPayload == 0)
        {
            return false;
        }

        if (TryCandidate(bufferRef, maxPayload, 8, 16, out audioData, out audioByteSize))
        {
            return true;
        }

        return TryCandidate(bufferRef, maxPayload, 4, 12, out audioData, out audioByteSize);
    }
}
