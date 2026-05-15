using System.Runtime.InteropServices;

namespace AudioAnalyzer.Platform.macOS.Audio.CoreAudio;

internal static class MacOsCoreAudioInterop
{
    private const string CoreAudioLib =
        "/System/Library/Frameworks/CoreAudio.framework/CoreAudio";

    private const string AudioToolboxLib =
        "/System/Library/Frameworks/AudioToolbox.framework/AudioToolbox";

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void AudioQueueInputCallback(
        IntPtr inUserData,
        IntPtr inAQ,
        IntPtr inBuffer,
        IntPtr inStartTime,
        uint inNumPackets,
        IntPtr inPacketDesc);

    [DllImport(CoreAudioLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int AudioObjectGetPropertyDataSize(
        uint inObjectID,
        ref AudioObjectPropertyAddress inAddress,
        uint inQualifierDataSize,
        IntPtr inQualifierData,
        ref uint outDataSize);

    [DllImport(CoreAudioLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int AudioObjectGetPropertyData(
        uint inObjectID,
        ref AudioObjectPropertyAddress inAddress,
        uint inQualifierDataSize,
        IntPtr inQualifierData,
        ref uint ioDataSize,
        IntPtr outData);

    [DllImport(AudioToolboxLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int AudioQueueNewInput(
        ref AudioStreamBasicDescription inFormat,
        AudioQueueInputCallback inCallback,
        IntPtr inUserData,
        IntPtr inCallbackRunLoop,
        IntPtr inCallbackRunLoopMode,
        uint inFlags,
        out IntPtr outAQ);

    [DllImport(AudioToolboxLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int AudioQueueDispose(IntPtr inAQ, byte immediate);

    [DllImport(AudioToolboxLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int AudioQueueAllocateBuffer(IntPtr inAQ, uint inBufferByteSize, out IntPtr outBuffer);

    [DllImport(AudioToolboxLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int AudioQueueEnqueueBuffer(
        IntPtr inAQ,
        IntPtr inBuffer,
        uint inNumPackets,
        IntPtr inPacketDesc);

    [DllImport(AudioToolboxLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int AudioQueueStart(IntPtr inAQ, IntPtr inStartTime);

    [DllImport(AudioToolboxLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int AudioQueueStop(IntPtr inAQ, byte immediate);

    [DllImport(AudioToolboxLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int AudioQueueSetProperty(
        IntPtr inAQ,
        uint inPropertyID,
        IntPtr inData,
        uint inDataSize);

    /// <summary>Resolves property value byte count for <see cref="AudioQueueSetProperty"/> (exported as AudioQueueGetPropertySize).</summary>
    [DllImport(AudioToolboxLib, EntryPoint = "AudioQueueGetPropertySize", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int AudioQueueGetPropertySize(IntPtr inAQ, uint inPropertyID, ref uint outDataSize);

    internal static readonly uint kAudioQueueProperty_CurrentDevice =
        MacOsCoreAudioFourCc.FourCC('c', 'q', 'c', 'd');

    internal static readonly uint kAudioQueueProperty_ChannelLayout =
        MacOsCoreAudioFourCc.FourCC('c', 'l', 'a', 'y');
}
