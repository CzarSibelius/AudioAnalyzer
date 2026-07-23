using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Application.Diagnostics;

/// <summary>
/// Default <see cref="IFeatureCapabilityReport"/>: concatenates the registered probes in
/// dependency-injection order (each probe's statuses preserved), caching the result. The order is
/// therefore deterministic and driven by probe registration; see <see cref="GetStatuses"/>.
/// </summary>
public sealed class FeatureCapabilityReport : IFeatureCapabilityReport
{
    private readonly IReadOnlyList<IFeatureCapabilityProbe> _probes;
    private readonly object _gate = new();
    private IReadOnlyList<FeatureCapabilityStatus>? _cache;

    /// <summary>Initializes a new instance of the <see cref="FeatureCapabilityReport"/> class.</summary>
    /// <param name="probes">Capability contributors, in the desired report order.</param>
    public FeatureCapabilityReport(IEnumerable<IFeatureCapabilityProbe> probes)
    {
        ArgumentNullException.ThrowIfNull(probes);
        _probes = probes.ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<FeatureCapabilityStatus> GetStatuses()
    {
        lock (_gate)
        {
            return _cache ??= Build();
        }
    }

    /// <inheritdoc />
    public void Refresh()
    {
        lock (_gate)
        {
            _cache = Build();
        }
    }

    private List<FeatureCapabilityStatus> Build()
    {
        var result = new List<FeatureCapabilityStatus>();
        foreach (var probe in _probes)
        {
            var statuses = probe.Probe();
            if (statuses == null)
            {
                continue;
            }

            foreach (var status in statuses)
            {
                if (status != null)
                {
                    result.Add(status);
                }
            }
        }

        return result;
    }
}
