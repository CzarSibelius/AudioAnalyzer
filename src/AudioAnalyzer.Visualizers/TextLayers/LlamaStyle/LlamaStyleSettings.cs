using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Layer-specific settings for LlamaStyle. Only LlamaStyleLayer reads these.</summary>
public sealed class LlamaStyleSettings
{
    /// <summary>Show volume bar at top. Default false.</summary>
    public bool ShowVolumeBar { get; set; }

    /// <summary>Show row labels (100%, 75%, etc.). Default false.</summary>
    public bool ShowRowLabels { get; set; }

    /// <summary>Show frequency labels (Hz) at bottom. Default false.</summary>
    [Setting("ShowFreqLabels", "Show freq labels")]
    public bool ShowFrequencyLabels { get; set; }

    /// <summary>Color scheme: "Winamp" (green→red) or "Spectrum" (red→blue). Default "Winamp".</summary>
    [SettingChoices("Winamp", "Spectrum")]
    public string ColorScheme { get; set; } = "Winamp";

    /// <summary>Peak marker style: "Blocks" (▀▀) or "DoubleLine" (══). Default "Blocks".</summary>
    [SettingChoices("Blocks", "DoubleLine")]
    public string PeakMarkerStyle { get; set; } = "Blocks";

    /// <summary>Chars per band: 2 or 3. Default 3.</summary>
    [SettingRange(2, 3)]
    public int BarWidth { get; set; } = 3;
}
