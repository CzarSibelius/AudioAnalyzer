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

    /// <summary>State for the current layer when it is GeissBackground. Only used by GeissBackgroundLayer.</summary>
    public required GeissBackgroundState GeissBackgroundStateForLayer { get; init; }

    /// <summary>State for the current layer when it is BeatCircles. Only used by BeatCirclesLayer.</summary>
    public required BeatCirclesState BeatCirclesStateForLayer { get; init; }
}

/// <summary>State for GeissBackground layer: phase, colorPhase, bass/treble intensity.</summary>
public sealed class GeissBackgroundState
{
    public double Phase { get; set; }
    public double ColorPhase { get; set; }
    public double BassIntensity { get; set; }
    public double TrebleIntensity { get; set; }
}

/// <summary>State for BeatCircles layer: circle list, last beat count for spawn detection, and smoothed bass intensity.</summary>
public sealed class BeatCirclesState
{
    public List<BeatCircle> Circles { get; } = new();
    public int LastBeatCount { get; set; } = -1;
    public double BassIntensity { get; set; }
}
