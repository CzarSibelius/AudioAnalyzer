using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Frame context for rendering. Contains analysis data (layout, FFT, waveform, volume, beats) filled by the engine.
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

    public float LeftChannel { get; set; }
    public float RightChannel { get; set; }
    public float LeftPeakHold { get; set; }
    public float RightPeakHold { get; set; }

    /// <summary>
    /// When show-render-FPS is enabled in UI settings, smoothed full main render FPS (ADR-0067); otherwise unset.
    /// </summary>
    public double? MeasuredMainRenderFps { get; set; }

    /// <summary>
    /// When layer render timing is enabled, per sorted-layer-index <c>Draw</c> duration in milliseconds from the last main render (ADR-0073).
    /// Length is <see cref="TextLayersLimits.MaxLayerCount"/>; <c>null</c> entries mean not timed. Not produced by <see cref="AnalysisEngine"/>; merged in <c>GetSnapshotForUi</c> for modals.
    /// </summary>
    public double?[]? LayerRenderTimeMs { get; set; }

    /// <summary>
    /// Wall-clock seconds since the previous full main render tick, set by the visualization orchestrator (ADR-0072).
    /// Not set by <see cref="AnalysisEngine"/>; used for delta-time animation in the visualizer and UI scrolling.
    /// </summary>
    public double FrameDeltaSeconds { get; set; }
}
