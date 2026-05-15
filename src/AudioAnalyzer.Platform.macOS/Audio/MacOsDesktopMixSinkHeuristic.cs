namespace AudioAnalyzer.Platform.macOS.Audio;

/// <summary>
/// Detects Core Audio inputs that are commonly used to carry “what you hear” after operator routing
/// (BlackHole, Soundflower, Rogue Amoeba Loopback, etc.).
/// </summary>
internal static class MacOsDesktopMixSinkHeuristic
{
    private static readonly string[] s_nameMarkers =
    {
        "BLACKHOLE",
        "SOUNDFLOWER",
        "WAVTAP",
        "GROUNDCONTROL",
        "EXISTENTIAL",
        "LOOPBACK",
        "OBS VIRTUAL",
        "VB-CABLE",
    };

    /// <summary>Returns true when the device name or UID suggests a virtual sink fed from desktop output routing.</summary>
    internal static bool LooksLikeDesktopMixSink(string hardwareName, string uid)
    {
        return ContainsAnyMarker(hardwareName) || ContainsAnyMarker(uid);
    }

    private static bool ContainsAnyMarker(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        foreach (string marker in s_nameMarkers)
        {
            if (text.Contains(marker, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
