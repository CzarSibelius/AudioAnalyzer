using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Mode-agnostic analysis data produced by the engine. Contains only analysis-derived data and layout (dimensions).
/// Consumed by IVisualizationRenderer and IVisualizer implementations.
/// </summary>
public sealed class AnalysisSnapshot
{
    public int DisplayStartRow { get; set; }
    public int TerminalWidth { get; set; }
    public int TerminalHeight { get; set; }

    public float Volume { get; set; }
    public double CurrentBpm { get; set; }
    public double BeatSensitivity { get; set; }
    public bool BeatFlashActive { get; set; }
    /// <summary>Incremented each time a beat is detected; visualizers can react to changes (e.g. spawn circles).</summary>
    public int BeatCount { get; set; }

    public int NumBands { get; set; }
    public double[] SmoothedMagnitudes { get; set; } = Array.Empty<double>();
    public double[] PeakHold { get; set; } = Array.Empty<double>();
    public double TargetMaxMagnitude { get; set; }

    public float[] Waveform { get; set; } = Array.Empty<float>();
    public int WaveformPosition { get; set; }
    public int WaveformSize { get; set; }
    /// <summary>Amplitude gain for oscilloscope display (user-adjustable, e.g. 1.0–10.0).</summary>
    public double OscilloscopeGain { get; set; } = 2.5;

    public float LeftChannel { get; set; }
    public float RightChannel { get; set; }
    public float LeftPeakHold { get; set; }
    public float RightPeakHold { get; set; }

    /// <summary>Resolved palette for palette-aware visualizers (e.g. Unknown Pleasures). Set by the renderer from selected palette.</summary>
    public IReadOnlyList<PaletteColor>? UnknownPleasuresPalette { get; set; }

    /// <summary>Display name of the current palette (for toolbar). Set by the renderer when palette is applied.</summary>
    public string? CurrentPaletteName { get; set; }
}
