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

    /// <summary>Solid color or palette gradient.</summary>
    [Setting("FillColorStyle", "Color style")]
    public FillColorStyle FillColorStyle { get; set; } = FillColorStyle.Solid;

    /// <summary>Palette index for the gradient end; modulo layer palette size. Ignored when not gradient.</summary>
    [Setting("GradientEndColorIndex", "Gradient end color index")]
    [SettingRange(0, 99, 1)]
    public int GradientEndColorIndex { get; set; }

    /// <summary>Axis for the gradient. Ignored when <see cref="FillColorStyle"/> is not <see cref="FillColorStyle.Gradient"/>.</summary>
    [Setting("FillGradientDirection", "Gradient direction")]
    public FillGradientDirection FillGradientDirection { get; set; } = FillGradientDirection.LeftToRight;

    /// <summary>Replace buffer pixels or blend the fill color over them (e.g. dimming overlay).</summary>
    [Setting("FillCompositeMode", "Composite")]
    public FillCompositeMode FillCompositeMode { get; set; } = FillCompositeMode.Replace;

    /// <summary>Blend amount when <see cref="FillCompositeMode"/> is <see cref="FillCompositeMode.BlendOver"/>; 0 = no change, 1 = full fill color.</summary>
    [Setting("BlendStrength", "Blend strength")]
    [SettingRange(0.0, 1.0, 0.05)]
    public double BlendStrength { get; set; } = 1.0;

    /// <summary>
    /// When true and composite is <see cref="FillCompositeMode.BlendOver"/>, cells whose character is space (U+0020)
    /// use RGB black as the blend &quot;under&quot; color instead of the stored palette color (ADR-0059).
    /// If another layer deliberately uses space with a non-black color, enabling this still blends as if under were black (opt-in trade-off).
    /// </summary>
    [Setting("BlendSpaceAsBlack", "Blend space as black")]
    public bool BlendSpaceAsBlack { get; set; }
}
