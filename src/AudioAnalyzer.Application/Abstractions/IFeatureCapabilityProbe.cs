namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Contributes zero or more <see cref="FeatureCapabilityStatus"/> entries to the aggregated
/// <see cref="IFeatureCapabilityReport"/>. Implemented per platform (and cross-platform for managed
/// features) and composed via dependency injection; probes must never trigger permission prompts.
/// </summary>
public interface IFeatureCapabilityProbe
{
    /// <summary>Returns the capability statuses this probe is responsible for (possibly empty).</summary>
    IReadOnlyList<FeatureCapabilityStatus> Probe();
}
