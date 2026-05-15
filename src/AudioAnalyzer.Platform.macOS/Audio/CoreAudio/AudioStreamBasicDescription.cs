using System.Runtime.InteropServices;

namespace AudioAnalyzer.Platform.macOS.Audio.CoreAudio;

[StructLayout(LayoutKind.Sequential)]
internal struct AudioStreamBasicDescription
{
    public double mSampleRate;
    public uint mFormatID;
    public uint mFormatFlags;
    public uint mBytesPerPacket;
    public uint mFramesPerPacket;
    public uint mBytesPerFrame;
    public uint mChannelsPerFrame;
    public uint mBitsPerChannel;
    public uint mReserved;
}
