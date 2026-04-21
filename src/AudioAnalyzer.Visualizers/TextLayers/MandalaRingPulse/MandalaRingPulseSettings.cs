using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Layer-specific settings for <see cref="TextLayerType.MandalaRingPulse"/>.</summary>
public sealed class MandalaRingPulseSettings
{
    /// <summary>Rings only, or rings with radial spokes.</summary>
    [Setting("Pattern", "Pattern")]
    public MandalaRingPulsePattern Pattern { get; set; } = MandalaRingPulsePattern.RingAndSpoke;

    /// <summary>Number of concentric ring bands.</summary>
    [Setting("RingCount", "Ring count")]
    [SettingRange(3, 16, 1)]
    public int RingCount { get; set; } = 7;

    /// <summary>Spoke count and angular modulation lobes (used when <see cref="Pattern"/> is <see cref="MandalaRingPulsePattern.RingAndSpoke"/>).</summary>
    [Setting("Symmetry", "Symmetry")]
    [SettingRange(3, 16, 1)]
    public int Symmetry { get; set; } = 8;

    /// <summary>Tempo pulses per beat (e.g. 4 for four pulses per quarter note at the detected BPM).</summary>
    [Setting("PulsesPerBeat", "Pulses per beat")]
    [SettingRange(1, 8, 1)]
    public int PulsesPerBeat { get; set; } = 4;

    /// <summary>How far ring radii breathe (0 = static geometry).</summary>
    [Setting("PulseDepth", "Pulse depth")]
    [SettingRange(0, 0.45, 0.05)]
    public double PulseDepth { get; set; } = 0.22;

    /// <summary>Angular modulation spin in radians per second (independent of BPM).</summary>
    [Setting("AngularMotion", "Angular motion")]
    [SettingRange(0, 3.0, 0.1)]
    public double AngularMotion { get; set; } = 0.35;

    /// <summary>Blend of spectrum energy into ring thickness (0 = pulse only).</summary>
    [Setting("EnergyMix", "Energy mix")]
    [SettingRange(0, 1, 0.05)]
    public double EnergyMix { get; set; } = 0.35;

    /// <summary>Optional beat flash / speed boost.</summary>
    [Setting("BeatReaction", "Beat reaction")]
    public MandalaRingPulseBeatReaction BeatReaction { get; set; } = MandalaRingPulseBeatReaction.None;
}
