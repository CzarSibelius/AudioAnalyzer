namespace AudioAnalyzer.Domain;

/// <summary>
/// Unit for per-preset duration within a Show.
/// </summary>
public enum DurationUnit
{
    /// <summary>Wall-clock seconds.</summary>
    Seconds,

    /// <summary>Beats at current detected BPM.</summary>
    Beats
}
