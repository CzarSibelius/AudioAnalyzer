namespace AudioAnalyzer.Application.Abstractions;

/// <summary>One optional label/value pair for the TextLayers toolbar when the palette-cycled layer has extra context (e.g. gain, active file name).</summary>
public sealed class LayerToolbarContextualRow
{
    /// <summary>Initializes a new instance of the <see cref="LayerToolbarContextualRow"/> class.</summary>
    /// <param name="label">Toolbar label (e.g. Gain, Image).</param>
    /// <param name="value">Display value (may be truncated by the builder).</param>
    public LayerToolbarContextualRow(string label, string value)
    {
        Label = label ?? throw new ArgumentNullException(nameof(label));
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Toolbar label.</summary>
    public string Label { get; }

    /// <summary>Display value.</summary>
    public string Value { get; }
}
