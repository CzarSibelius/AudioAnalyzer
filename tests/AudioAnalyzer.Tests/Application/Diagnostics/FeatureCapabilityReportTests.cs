using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Diagnostics;
using Xunit;

namespace AudioAnalyzer.Tests.Application.Diagnostics;

/// <summary>Tests aggregation, ordering, caching, and refresh of <see cref="FeatureCapabilityReport"/>.</summary>
public sealed class FeatureCapabilityReportTests
{
    private sealed class FakeProbe : IFeatureCapabilityProbe
    {
        private readonly IReadOnlyList<FeatureCapabilityStatus> _statuses;

        public FakeProbe(params FeatureCapabilityStatus[] statuses)
        {
            _statuses = statuses;
        }

        public int ProbeCallCount { get; private set; }

        public IReadOnlyList<FeatureCapabilityStatus> Probe()
        {
            ProbeCallCount++;
            return _statuses;
        }
    }

    private static FeatureCapabilityStatus Status(string id, FeatureAvailability availability = FeatureAvailability.Available) =>
        new(id, id, availability, "", FeatureCapabilityCategory.Integration);

    [Fact]
    public void GetStatuses_ConcatenatesProbesInRegistrationOrder()
    {
        var report = new FeatureCapabilityReport(
        [
            new FakeProbe(Status("a"), Status("b")),
            new FakeProbe(Status("c")),
            new FakeProbe(Status("d"), Status("e"))
        ]);

        var ids = report.GetStatuses().Select(s => s.Id).ToArray();

        Assert.Equal(["a", "b", "c", "d", "e"], ids);
    }

    [Fact]
    public void GetStatuses_IsCached_ProbesOnlyOnce()
    {
        var probe = new FakeProbe(Status("a"));
        var report = new FeatureCapabilityReport([probe]);

        var first = report.GetStatuses();
        var second = report.GetStatuses();

        Assert.Same(first, second);
        Assert.Equal(1, probe.ProbeCallCount);
    }

    [Fact]
    public void Refresh_ReProbesAndReplacesSnapshot()
    {
        var probe = new FakeProbe(Status("a"));
        var report = new FeatureCapabilityReport([probe]);

        var first = report.GetStatuses();
        report.Refresh();
        var second = report.GetStatuses();

        Assert.NotSame(first, second);
        Assert.Equal(2, probe.ProbeCallCount);
    }

    [Fact]
    public void GetStatuses_WithNoProbes_ReturnsEmpty()
    {
        var report = new FeatureCapabilityReport([]);

        Assert.Empty(report.GetStatuses());
    }
}
