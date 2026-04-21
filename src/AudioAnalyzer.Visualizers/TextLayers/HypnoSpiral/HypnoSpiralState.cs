namespace AudioAnalyzer.Visualizers;

/// <summary>Runtime animation state for <see cref="HypnoSpiralLayer"/>.</summary>
public sealed class HypnoSpiralState
{
    /// <summary>Accumulated twist (radians) applied to the primary spiral phase.</summary>
    public double TwistRadians { get; set; }

    /// <summary>Slow drift for the secondary (moiré) pattern.</summary>
    public double MoireDrift { get; set; }
}
