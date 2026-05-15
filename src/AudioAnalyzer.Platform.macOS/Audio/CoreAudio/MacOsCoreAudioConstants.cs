namespace AudioAnalyzer.Platform.macOS.Audio.CoreAudio;

internal static class MacOsCoreAudioConstants
{
    internal const uint kAudioObjectSystemObject = 1;
    internal const uint kAudioObjectPropertyElementMain = 0;

    internal static readonly uint kAudioHardwarePropertyDevices =
        MacOsCoreAudioFourCc.FourCC('d', 'e', 'v', '#');

    internal static readonly uint kAudioHardwarePropertyDefaultInputDevice =
        MacOsCoreAudioFourCc.FourCC('d', 'I', 'n', ' ');

    internal static readonly uint kAudioDevicePropertyDeviceUID =
        MacOsCoreAudioFourCc.FourCC('u', 'i', 'd', ' ');

    internal static readonly uint kAudioDevicePropertyDeviceNameCFString =
        MacOsCoreAudioFourCc.FourCC('l', 'c', 'n', 'm');

    internal static readonly uint kAudioDevicePropertyStreamFormat =
        MacOsCoreAudioFourCc.FourCC('s', 'f', 'm', 't');

    internal static readonly uint kAudioObjectPropertyScopeGlobal =
        MacOsCoreAudioFourCc.FourCC('g', 'l', 'o', 'b');

    internal static readonly uint kAudioDevicePropertyScopeInput =
        MacOsCoreAudioFourCc.FourCC('i', 'n', 'p', 't');

    internal static readonly uint kAudioFormatLinearPCM =
        MacOsCoreAudioFourCc.FourCC('l', 'p', 'c', 'm');

    internal const uint kAudioFormatFlagIsFloat = 1U << 0;
    internal const uint kAudioFormatFlagIsBigEndian = 1U << 1;
    internal const uint kAudioFormatFlagIsSignedInteger = 1U << 2;
    internal const uint kAudioFormatFlagIsPacked = 1U << 3;
    internal const uint kAudioFormatFlagIsAlignedHigh = 1U << 4;
    internal const uint kAudioFormatFlagIsNonInterleaved = 1U << 5;

    internal const int noErr = 0;

    /// <summary>Mono layout tag (<c>kAudioChannelLayoutTag_Mono</c>).</summary>
    internal const uint kAudioChannelLayoutTag_Mono = (100u << 16) | 1u;

    /// <summary>Stereo layout tag (<c>kAudioChannelLayoutTag_Stereo</c>).</summary>
    internal const uint kAudioChannelLayoutTag_Stereo = (101u << 16) | 2u;
}
