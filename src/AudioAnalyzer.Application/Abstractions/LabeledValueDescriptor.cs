using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Data-only descriptor for a labeled value: label and a getter that supplies the current value.
/// Used by layouts to compose UI; rendering (scrolling, formatting) is done by <see cref="HorizontalRowComponent"/> and <see cref="ScrollingTextComponent"/>.
/// </summary>
public sealed class LabeledValueDescriptor
{
    /// <summary>Label text (e.g. "Device", "Now"). Displayed as "Label:" (no space before value).</summary>
    public string Label { get; }

    /// <summary>Returns the current display value for this cell. Called each frame by the row renderer.</summary>
    public Func<IDisplayText> GetValue { get; }

    /// <summary>Optional label color. When null, the row renderer uses the palette label color.</summary>
    public PaletteColor? LabelColor { get; }

    /// <summary>Optional text color. When null, the row renderer uses the palette normal color.</summary>
    public PaletteColor? TextColor { get; }

    /// <summary>When true, the value is preformatted (e.g. <see cref="Display.AnsiText"/>); the renderer does not apply palette colors and uses truncate-with-ellipsis instead of scrolling.</summary>
    public bool PreformattedAnsi { get; }

    /// <summary>Creates a descriptor with the given label and value getter.</summary>
    /// <param name="label">Label text (e.g. "Device", "Now").</param>
    /// <param name="getValue">Getter for the current value. Must not be null.</param>
    /// <param name="labelColor">Optional label color; when null, renderer uses palette.</param>
    /// <param name="textColor">Optional text color; when null, renderer uses palette.</param>
    /// <param name="preformattedAnsi">When true, value is rendered as-is (no palette wrap) with truncate-with-ellipsis.</param>
    public LabeledValueDescriptor(string label, Func<IDisplayText> getValue,
        PaletteColor? labelColor = null, PaletteColor? textColor = null, bool preformattedAnsi = false)
    {
        Label = label ?? "";
        GetValue = getValue ?? throw new ArgumentNullException(nameof(getValue));
        LabelColor = labelColor;
        TextColor = textColor;
        PreformattedAnsi = preformattedAnsi;
    }
}
