namespace AudioAnalyzer.Visualizers;

/// <summary>How a beat affects the <see cref="StarfieldLayer"/>.</summary>
public enum StarfieldBeatReaction
{
    /// <summary>No beat-driven change.</summary>
    None,

    /// <summary>Briefly doubles travel speed while the beat flash is active.</summary>
    SpeedBurst,

    /// <summary>Shifts palette index by one while the beat flash is active.</summary>
    Flash
}
