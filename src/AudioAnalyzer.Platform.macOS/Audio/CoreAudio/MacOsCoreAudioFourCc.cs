namespace AudioAnalyzer.Platform.macOS.Audio.CoreAudio;

internal static class MacOsCoreAudioFourCc
{
    internal static uint FourCC(char a, char b, char c, char d) =>
        ((uint)a << 24) | ((uint)b << 16) | ((uint)c << 8) | d;
}
