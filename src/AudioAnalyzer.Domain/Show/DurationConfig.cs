namespace AudioAnalyzer.Domain;

/// <summary>
/// Configuration for how long a preset plays within a Show.
/// </summary>
public class DurationConfig
{
    /// <summary>Unit of duration: wall-clock seconds or beats (music tempo).</summary>
    public DurationUnit Unit { get; set; } = DurationUnit.Seconds;

    /// <summary>Duration value. For Seconds: wall-clock seconds. For Beats: number of beats at current BPM.</summary>
    public double Value { get; set; } = 30;
}
