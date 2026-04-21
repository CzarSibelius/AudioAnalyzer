using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Mutable context for <see cref="LayerPickerKeyHandlerConfig"/>.</summary>
internal sealed class LayerPickerKeyContext : IKeyHandlerContext
{
    /// <summary>Every pickable <see cref="TextLayerType"/> (stable order).</summary>
    public required IReadOnlyList<TextLayerType> PickableTypes { get; init; }

    /// <summary>Highlighted row index into <see cref="PickableTypes"/>. Mutated by the handler.</summary>
    public int SelectedIndex { get; set; }

    /// <summary>Set when the user confirms with Enter so the host can apply <see cref="SelectedIndex"/>.</summary>
    public bool ConfirmSelection { get; set; }
}
