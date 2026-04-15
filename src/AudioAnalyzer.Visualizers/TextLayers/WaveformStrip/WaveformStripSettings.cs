using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Layer-specific settings for <see cref="TextLayerType.WaveformStrip"/>.</summary>
public sealed class WaveformStripSettings
{
    /// <summary>Amplitude gain (1.0–10.0). Default 2.5.</summary>
    [SettingRange(1.0, 10.0, 0.5)]
    public double Gain { get; set; } = 2.5;

    /// <summary>When true, fill from the strip center line to min/max per overview bucket (or center to sample in scope fallback); when false, overview uses midpoint columns with short-range connectors only (see waveform-strip spec). Default true (dense envelope).</summary>
    public bool Filled { get; set; } = true;

    /// <summary>Color mapping for columns.</summary>
    [SettingChoices("PaletteDistance", "SpectralApprox", "SpectralGoertzel")]
    public WaveformStripColorMode ColorMode { get; set; } = WaveformStripColorMode.SpectralApprox;

    /// <summary>Mono strip vs stacked L/R overview strips (needs enough layer height).</summary>
    [SettingChoices("Mono", "StereoStacked")]
    public WaveformStripStereoLayout StereoLayout { get; set; } = WaveformStripStereoLayout.Mono;

    /// <summary>Trailing wall seconds mapped across the strip width (1–120). Default 15. The engine retains longer mono history per General Settings; the decimated overview is built over the largest <see cref="FixedVisibleSeconds"/> among enabled strips.</summary>
    [SettingRange(1.0, 120.0, 1.0)]
    public double FixedVisibleSeconds { get; set; } = 15.0;

    /// <summary>Beats per bar for bar markers when using stored beat marks (1–16). Default 4.</summary>
    [SettingRange(1, 16, 1)]
    public int BeatsPerBar { get; set; } = 4;

    /// <summary>Horizontal nudge for beat grid columns (negative = left).</summary>
    [SettingRange(-16, 16, 1)]
    public int BeatGridOffsetColumns { get; set; }

    /// <summary>When true, draw a subtle vertical tick on beat columns (stored beat marks when available; else BPM spacing).</summary>
    public bool ShowBeatGrid { get; set; } = true;

    /// <summary>When true, draw cue-style markers at bar boundaries from stored beats (or BPM fallback).</summary>
    public bool ShowBeatMarkers { get; set; } = true;

    /// <summary>When true and there is room above the layer, show overview span in seconds.</summary>
    public bool ShowTimeLabel { get; set; }
}
