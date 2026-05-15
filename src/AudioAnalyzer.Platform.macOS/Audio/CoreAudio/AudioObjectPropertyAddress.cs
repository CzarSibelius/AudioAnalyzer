using System.Runtime.InteropServices;

namespace AudioAnalyzer.Platform.macOS.Audio.CoreAudio;

[StructLayout(LayoutKind.Sequential)]
internal struct AudioObjectPropertyAddress
{
    public uint mSelector;
    public uint mScope;
    public uint mElement;

    internal AudioObjectPropertyAddress(uint selector, uint scope, uint element)
    {
        mSelector = selector;
        mScope = scope;
        mElement = element;
    }
}
