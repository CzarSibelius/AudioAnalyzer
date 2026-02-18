namespace AudioAnalyzer.Domain;

/// <summary>
/// Semantic color slots for UI text. Structure differs from visualizer palettes (named slots vs indexed array).
/// Each slot supports 16-color or 24-bit RGB via <see cref="PaletteColor"/>.
/// </summary>
public class UiPalette
{
    /// <summary>Default UI text color.</summary>
    public PaletteColor Normal { get; set; } = PaletteColor.FromRgb(220, 220, 220);

    /// <summary>Highlighted/active UI text (e.g. selected layer number, now-playing).</summary>
    public PaletteColor Highlighted { get; set; } = PaletteColor.FromConsoleColor(ConsoleColor.Yellow);

    /// <summary>Dimmed or disabled UI text.</summary>
    public PaletteColor Dimmed { get; set; } = PaletteColor.FromRgb(100, 100, 100);

    /// <summary>Label and header color (e.g. "Device:", "Now:").</summary>
    public PaletteColor Label { get; set; } = PaletteColor.FromConsoleColor(ConsoleColor.DarkCyan);

    /// <summary>Optional background color for future use (modals, etc.).</summary>
    public PaletteColor? Background { get; set; }
}
