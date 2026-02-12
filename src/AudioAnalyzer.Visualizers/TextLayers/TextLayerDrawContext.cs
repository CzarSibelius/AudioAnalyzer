using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>State for one falling letter: column, vertical position, character.</summary>
public struct FallingLetterState
{
    public int Col { get; set; }
    public double Y { get; set; }
    public char Character { get; set; }
}

/// <summary>Context passed to layer renderers for drawing. Contains buffer, snapshot, palette, dimensions, and per-layer state.</summary>
public sealed class TextLayerDrawContext
{
    public required ViewportCellBuffer Buffer { get; init; }
    public required AnalysisSnapshot Snapshot { get; init; }
    public required IReadOnlyList<PaletteColor> Palette { get; init; }
    public required double SpeedBurst { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required int LayerIndex { get; init; }
    /// <summary>Falling letter particles for the current layer. Only used by FallingLettersLayer.</summary>
    public required List<FallingLetterState> FallingLettersForLayer { get; init; }

    /// <summary>State for the current layer when it is AsciiImage. Only used by AsciiImageLayer.</summary>
    public required AsciiImageState AsciiImageStateForLayer { get; init; }
}
