namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Aggregates the registered <see cref="IFeatureCapabilityProbe"/> instances into an ordered,
/// cached snapshot of <see cref="FeatureCapabilityStatus"/> entries. The snapshot is computed once
/// (lazily) and reused; call <see cref="Refresh"/> to re-probe (e.g. on entering General Settings).
/// </summary>
public interface IFeatureCapabilityReport
{
    /// <summary>Returns the cached, ordered capability snapshot, probing once on first access.</summary>
    IReadOnlyList<FeatureCapabilityStatus> GetStatuses();

    /// <summary>Re-probes every contributor and replaces the cached snapshot.</summary>
    void Refresh();
}
