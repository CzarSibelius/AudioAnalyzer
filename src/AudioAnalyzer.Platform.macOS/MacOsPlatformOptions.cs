using AudioAnalyzer.Platform.macOS.Audio;

namespace AudioAnalyzer.Platform.macOS;

/// <summary>
/// Optional macOS platform overrides for tests (Core Audio enumeration / tap factory). Passed to
/// <see cref="MacOsPlatformServiceCollectionExtensions.AddMacOsPlatform"/> through a platform-agnostic slot.
/// </summary>
public sealed class MacOsPlatformOptions
{
    /// <summary>Override Core Audio enumeration for tests.</summary>
    public IMacOsAudioEnumerator? AudioEnumerator { get; init; }

    /// <summary>Override the Core Audio tap system-audio factory for tests.</summary>
    public IMacOsCoreAudioTapSystemAudioInputFactory? TapSystemAudioInputFactory { get; init; }
}
