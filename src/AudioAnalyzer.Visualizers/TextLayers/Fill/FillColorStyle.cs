namespace AudioAnalyzer.Visualizers;

/// <summary>Whether the Fill layer uses a solid palette color or a gradient between two palette indices.</summary>
public enum FillColorStyle
{
    /// <summary>Single color from common <c>ColorIndex</c>.</summary>
    Solid,

    /// <summary>Linear gradient from <c>ColorIndex</c> to <see cref="FillSettings.GradientEndColorIndex"/>.</summary>
    Gradient
}
