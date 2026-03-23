using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Display;

/// <summary>
/// Maps an indexed layer palette (same JSON as TextLayers) onto <see cref="UiPalette"/> and <see cref="TitleBarPalette"/> slots.
/// </summary>
public static class UiThemePaletteMapper
{
    /// <summary>
    /// Maps colors by index: UI uses 0–4, title bar uses 5–10; indices wrap with <c>i % K</c> when <c>K &lt; 11</c>.
    /// </summary>
    /// <param name="colors">Non-empty list of colors.</param>
    public static (UiPalette Ui, TitleBarPalette TitleBar) Map(IReadOnlyList<PaletteColor> colors)
    {
        ArgumentNullException.ThrowIfNull(colors);
        if (colors.Count == 0)
        {
            throw new ArgumentException("At least one color is required.", nameof(colors));
        }

        int k = colors.Count;
        PaletteColor At(int i) => colors[i % k];

        var ui = new UiPalette
        {
            Normal = At(0),
            Highlighted = At(1),
            Dimmed = At(2),
            Label = At(3),
            Background = At(4)
        };

        var titleBar = new TitleBarPalette
        {
            AppName = At(5),
            Mode = At(6),
            Preset = At(7),
            Layer = At(8),
            Separator = At(9),
            Frame = At(10)
        };

        return (ui, titleBar);
    }
}
