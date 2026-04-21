namespace AudioAnalyzer.Visualizers;

/// <summary>Runtime animation state for <see cref="MandalaRingPulseLayer"/>.</summary>
public sealed class MandalaRingPulseState
{
    /// <summary>Low-pass smoothed broadband energy (0..1) for thickness modulation.</summary>
    public double SmoothedEnergy { get; set; }

    /// <summary>Phase for breathing rings, radians; advances with tempo and <see cref="MandalaRingPulseSettings.PulsesPerBeat"/>.</summary>
    public double PhaseRadians { get; set; }

    /// <summary>Rotates angular mandala modulation (radians).</summary>
    public double AngularOffset { get; set; }
}
