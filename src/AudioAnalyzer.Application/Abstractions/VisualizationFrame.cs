using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// All data needed to render one frame. Filled by AnalysisEngine, consumed by IVisualizationRenderer.
/// </summary>
public sealed class VisualizationFrame
{
    public VisualizationMode Mode { get; set; }
    public int DisplayStartRow { get; set; }
    public int TerminalWidth { get; set; }
    public int TerminalHeight { get; set; }

    public float Volume { get; set; }
    public double CurrentBpm { get; set; }
    public double BeatSensitivity { get; set; }
    public bool BeatFlashActive { get; set; }
    public string ModeName { get; set; } = "";

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

    public double GeissPhase { get; set; }
    public double GeissColorPhase { get; set; }
    public double GeissBassIntensity { get; set; }
    public double GeissTrebleIntensity { get; set; }
    public bool ShowBeatCircles { get; set; }
    public IReadOnlyList<BeatCircle> BeatCircles { get; set; } = Array.Empty<BeatCircle>();
}
