using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Layer-specific settings for <see cref="BufferDistortionLayer"/>.</summary>
public sealed class BufferDistortionSettings
{
    /// <summary>Distortion style.</summary>
    [Setting("Mode", "Mode")]
    public BufferDistortionMode Mode { get; set; } = BufferDistortionMode.PlaneWaves;

    /// <summary>Orientation for plane waves.</summary>
    [Setting("PlaneOrientation", "Plane orientation")]
    public BufferDistortionPlaneOrientation PlaneOrientation { get; set; } = BufferDistortionPlaneOrientation.WaveAlongX;

    /// <summary>Peak displacement in cells for plane waves (0 disables).</summary>
    [Setting("PlaneAmplitudeCells", "Plane amplitude (cells)")]
    [SettingRange(0, 6, 1)]
    public int PlaneAmplitudeCells { get; set; } = 2;

    /// <summary>Approximate wavelength along the varying axis, in cells.</summary>
    [Setting("PlaneWavelengthCells", "Plane wavelength (cells)")]
    [SettingRange(2, 48, 1)]
    public int PlaneWavelengthCells { get; set; } = 12;

    /// <summary>Phase advance per second at 1× speed (radians); scaled by layer speed and frame delta.</summary>
    [Setting("PlanePhaseSpeed", "Plane phase speed")]
    [SettingRange(0.0, 6.0, 0.05)]
    public double PlanePhaseSpeed { get; set; } = 1.2;

    /// <summary>When true, a new ripple is spawned each time the global beat count advances (drop mode).</summary>
    [Setting("SpawnOnBeat", "Spawn on beat")]
    public bool SpawnOnBeat { get; set; } = true;

    /// <summary>Maximum simultaneous ripples; oldest removed when exceeded.</summary>
    [Setting("MaxRipples", "Max ripples")]
    [SettingRange(1, 16, 1)]
    public int MaxRipples { get; set; } = 8;

    /// <summary>Radial wave number (higher = tighter rings).</summary>
    [Setting("RippleWaveNumber", "Ripple wave number")]
    [SettingRange(0.1, 3.0, 0.05)]
    public double RippleWaveNumber { get; set; } = 0.9;

    /// <summary>Temporal frequency inside the sine (radians per second).</summary>
    [Setting("RippleTimeSpeed", "Ripple time speed")]
    [SettingRange(0.5, 12.0, 0.25)]
    public double RippleTimeSpeed { get; set; } = 4.0;

    /// <summary>Amplitude scale for each ripple before radial falloff (cell units).</summary>
    [Setting("RippleAmplitudeCells", "Ripple amplitude (cells)")]
    [SettingRange(0, 6, 1)]
    public int RippleAmplitudeCells { get; set; } = 2;

    /// <summary>Exponential decay per second for ripple strength.</summary>
    [Setting("RippleDecayPerSecond", "Ripple decay / sec")]
    [SettingRange(0.05, 3.0, 0.05)]
    public double RippleDecayPerSecond { get; set; } = 0.45;

    /// <summary>Ripples older than this are removed.</summary>
    [Setting("RippleMaxAgeSeconds", "Ripple max age (s)")]
    [SettingRange(0.5, 12.0, 0.5)]
    public double RippleMaxAgeSeconds { get; set; } = 4.0;

    /// <summary>Clamp for combined displacement (cells) to limit tearing and cost.</summary>
    [Setting("MaxDisplacementCells", "Max displacement (cells)")]
    [SettingRange(1, 8, 1)]
    public int MaxDisplacementCells { get; set; } = 6;
}
