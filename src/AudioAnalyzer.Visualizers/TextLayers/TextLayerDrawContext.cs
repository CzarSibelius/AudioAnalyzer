using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

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

    /// <summary>State for the current layer when it is GeissBackground. Only used by GeissBackgroundLayer.</summary>
    public required GeissBackgroundState GeissBackgroundStateForLayer { get; init; }

    /// <summary>State for the current layer when it is BeatCircles. Only used by BeatCirclesLayer.</summary>
    public required BeatCirclesState BeatCirclesStateForLayer { get; init; }

    /// <summary>State for the current layer when it is UnknownPleasures. Only used by UnknownPleasuresLayer.</summary>
    public required UnknownPleasuresState UnknownPleasuresStateForLayer { get; init; }

    /// <summary>State for the current layer when it is Maschine. Only used by MaschineLayer.</summary>
    public required MaschineState MaschineStateForLayer { get; init; }
}
