using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Layer-specific settings for <see cref="FractalZoomLayer"/>.</summary>
public sealed class FractalZoomSettings
{
    /// <summary>Mandelbrot or Julia sampling.</summary>
    [Setting("FractalMode", "Fractal mode")]
    public FractalZoomMode FractalMode { get; set; } = FractalZoomMode.Mandelbrot;

    /// <summary>Julia parameter re (used when <see cref="FractalMode"/> is <see cref="FractalZoomMode.Julia"/>).</summary>
    [Setting("JuliaRe", "Julia re")]
    [SettingRange(-2.0, 2.0, 0.05)]
    public double JuliaRe { get; set; } = -0.8;

    /// <summary>Julia parameter im (used when <see cref="FractalMode"/> is <see cref="FractalZoomMode.Julia"/>).</summary>
    [Setting("JuliaIm", "Julia im")]
    [SettingRange(-2.0, 2.0, 0.05)]
    public double JuliaIm { get; set; } = 0.156;

    /// <summary>Maximum escape-time iterations (higher = sharper boundary, more CPU).</summary>
    [Setting("MaxIterations", "Max iterations")]
    [SettingRange(4, 32, 1)]
    public int MaxIterations { get; set; } = 16;

    /// <summary>More negative = more zoomed out (larger view in complex plane). Must stay below <see cref="LogScaleMax"/>.</summary>
    [Setting("LogScaleMin", "Log scale min")]
    [SettingRange(-14.0, -2.0, 0.1)]
    public double LogScaleMin { get; set; } = -10.0;

    /// <summary>Less negative = more zoomed in. Must stay above <see cref="LogScaleMin"/>.</summary>
    [Setting("LogScaleMax", "Log scale max")]
    [SettingRange(-12.0, -1.0, 0.1)]
    public double LogScaleMax { get; set; } = -2.6;

    /// <summary>How phase maps to zoom: linear, or plateau curves that dwell longer in mid-zoom (detail).</summary>
    [Setting("Dwell", "Zoom dwell")]
    public FractalZoomDwell Dwell { get; set; } = FractalZoomDwell.Mild;

    /// <summary>Radians added to orbit angle when zoom phase wraps (smaller = gentler pan between cycles).</summary>
    [Setting("OrbitStep", "Orbit step")]
    [SettingRange(0.02, 0.20, 0.01)]
    public double OrbitStep { get; set; } = 0.06;

    /// <summary>Multiplier for zoom phase advance (combined with layer <c>SpeedMultiplier</c>).</summary>
    [Setting("ZoomSpeed", "Zoom speed")]
    [SettingRange(0.0005, 0.02, 0.0005)]
    public double ZoomSpeed { get; set; } = 0.003;

    /// <summary>How a beat affects this layer.</summary>
    [Setting("BeatReaction", "Beat reaction")]
    public FractalZoomBeatReaction BeatReaction { get; set; } = FractalZoomBeatReaction.None;
}
