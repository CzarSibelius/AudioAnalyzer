using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Data-only viewport: label and a getter that supplies the current value.
/// Used by layouts to compose UI; rendering (scrolling, formatting) is done by <see cref="HorizontalRowComponent"/> and <see cref="ScrollingTextComponent"/>.
/// </summary>
public sealed class Viewport
{
    /// <summary>Label text (e.g. "Device", "Now"). Displayed as "Label:" or "Label(K):" when <see cref="Hotkey"/> is set.</summary>
    public string Label { get; }

    /// <summary>Optional hotkey for the label (e.g. "D" for "Device(D):").</summary>
    public string? Hotkey { get; }

    /// <summary>Returns the current display value for this viewport. Called each frame by the row renderer.</summary>
    public Func<IDisplayText> GetValue { get; }

    /// <summary>Optional label color. When null, the row renderer uses the palette label color.</summary>
    public PaletteColor? LabelColor { get; }

    /// <summary>Optional text color. When null, the row renderer uses the palette normal color.</summary>
    public PaletteColor? TextColor { get; }

    /// <summary>When true, the value is preformatted (e.g. <see cref="Display.AnsiText"/>); the renderer does not apply palette colors and uses truncate-with-ellipsis instead of scrolling.</summary>
    public bool PreformattedAnsi { get; }

    /// <summary>Creates a viewport with the given label and value getter.</summary>
    /// <param name="label">Label text (e.g. "Device", "Now").</param>
    /// <param name="getValue">Getter for the current value. Must not be null.</param>
    /// <param name="hotkey">Optional hotkey to show in the label (e.g. "Label(K):").</param>
    /// <param name="labelColor">Optional label color; when null, renderer uses palette.</param>
    /// <param name="textColor">Optional text color; when null, renderer uses palette.</param>
    /// <param name="preformattedAnsi">When true, value is rendered as-is (no palette wrap) with truncate-with-ellipsis.</param>
    public Viewport(string label, Func<IDisplayText> getValue, string? hotkey = null,
        PaletteColor? labelColor = null, PaletteColor? textColor = null, bool preformattedAnsi = false)
    {
        Label = label ?? "";
        GetValue = getValue ?? throw new ArgumentNullException(nameof(getValue));
        Hotkey = hotkey;
        LabelColor = labelColor;
        TextColor = textColor;
        PreformattedAnsi = preformattedAnsi;
    }
}
