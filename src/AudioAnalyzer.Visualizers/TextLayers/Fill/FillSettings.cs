using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Layer-specific settings for Fill. Only FillLayer reads these.</summary>
public sealed class FillSettings
{
    /// <summary>Which character to use for the fill (block, shades, space, or custom).</summary>
    [Setting("FillType", "Fill type")]
    public FillType FillType { get; set; } = FillType.FullBlock;

    /// <summary>Single character used when FillType is Custom. Ignored otherwise.</summary>
    [Setting("CustomChar", "Custom character")]
    public string CustomChar { get; set; } = "#";
}
