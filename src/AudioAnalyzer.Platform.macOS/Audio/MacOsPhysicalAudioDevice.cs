namespace AudioAnalyzer.Platform.macOS.Audio;

/// <summary>
/// Represents an input-capable Core Audio endpoint on macOS.
/// </summary>
/// <param name="DisplayName">Operator-visible label (includes icon and optional hints).</param>
/// <param name="Uid">Stable Core Audio device UID string.</param>
/// <param name="HardwareName">Raw Core Audio device name (no icon), used for desktop-mix heuristics.</param>
public sealed record MacOsPhysicalAudioDevice(string DisplayName, string Uid, string HardwareName);
