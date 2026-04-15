namespace AudioAnalyzer.Visualizers;

/// <summary>How a beat affects the FallingLetters layer.</summary>
public enum FallingLettersBeatReaction
{
    None,
    SpawnMore,
    SpeedBurst,

    /// <summary>Discrete phase nudge on beat (column rain); particles get a spawn-phase jitter.</summary>
    Flash
}
