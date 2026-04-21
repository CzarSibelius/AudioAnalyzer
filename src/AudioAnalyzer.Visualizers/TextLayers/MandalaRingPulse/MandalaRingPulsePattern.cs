namespace AudioAnalyzer.Visualizers;

/// <summary>How <see cref="MandalaRingPulseLayer"/> lays out radial structure.</summary>
public enum MandalaRingPulsePattern
{
    /// <summary>Concentric rings only.</summary>
    ConcentricRings,

    /// <summary>Rings plus radial spokes at <see cref="MandalaRingPulseSettings.Symmetry"/> angles (mandala web).</summary>
    RingAndSpoke
}
