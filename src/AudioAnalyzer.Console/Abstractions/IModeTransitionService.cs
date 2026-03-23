namespace AudioAnalyzer.Console;

/// <summary>
/// Cycles and updates persisted <see cref="Domain.ApplicationMode"/> (Tab) and related show/preset state.
/// </summary>
internal interface IModeTransitionService
{
    /// <summary>Advance to the next mode in the Tab cycle and persist settings.</summary>
    void CycleToNextMode();
}
