namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Last published main or header display frame delta for UI that renders without an <see cref="AnalysisSnapshot"/> (e.g. header rows). Set by the visualization orchestrator (ADR-0072).
/// </summary>
public interface IDisplayFrameClock
{
    /// <summary>Elapsed seconds for the current display tick; positive.</summary>
    double FrameDeltaSeconds { get; }

    /// <summary>Updates <see cref="FrameDeltaSeconds"/> for the upcoming draw.</summary>
    void SetFrameDeltaSeconds(double seconds);
}
