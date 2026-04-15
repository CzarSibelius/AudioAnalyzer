namespace AudioAnalyzer.Visualizers;

/// <summary>Per-layer state for FallingLetters: holds the list of falling letter particles (column, position, character) for one layer.</summary>
public sealed class FallingLettersLayerState
{
    /// <summary>Mutable list of falling letter particles for this layer.</summary>
    public List<FallingLetterState> Particles { get; } = new();

    /// <summary>Last animation mode applied for this slot; when settings change mode, particles are cleared.</summary>
    public FallingLettersAnimationMode? LastSyncedAnimationMode { get; set; }
}
