using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Console;
using Xunit;

namespace AudioAnalyzer.Tests.Console.GeneralSettingsHub;

/// <summary>
/// Tests the read-only Feature status section behavior: NotApplicable rows are hidden and the menu
/// selection never lands on the (non-selectable) status section (ADR-0095).
/// </summary>
public sealed class GeneralSettingsHubFeatureStatusTests
{
    private static FeatureCapabilityStatus Status(string id, FeatureAvailability availability) =>
        new(id, id, availability, "", FeatureCapabilityCategory.Integration);

    [Fact]
    public void FilterVisibleStatuses_HidesNotApplicable_PreservingOrder()
    {
        IReadOnlyList<FeatureCapabilityStatus> statuses =
        [
            Status("a", FeatureAvailability.Available),
            Status("b", FeatureAvailability.NotApplicable),
            Status("c", FeatureAvailability.Unavailable),
            Status("d", FeatureAvailability.NotApplicable)
        ];

        var visible = GeneralSettingsHubMenuLines.FilterVisibleStatuses(statuses);

        Assert.Equal(["a", "c"], visible.Select(s => s.Id).ToArray());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void WrapSelection_AlwaysStaysWithinSelectableRows(int delta)
    {
        int index = 0;
        for (int step = 0; step < GeneralSettingsHubMenuRows.Count * 4 + 1; step++)
        {
            index = GeneralSettingsHubMenuRows.WrapSelection(index, delta);
            Assert.InRange(index, 0, GeneralSettingsHubMenuRows.Count - 1);
        }
    }

    [Fact]
    public void WrapSelection_WrapsAroundMenuRowsOnly()
    {
        Assert.Equal(0, GeneralSettingsHubMenuRows.WrapSelection(GeneralSettingsHubMenuRows.Count - 1, 1));
        Assert.Equal(GeneralSettingsHubMenuRows.Count - 1, GeneralSettingsHubMenuRows.WrapSelection(0, -1));
    }
}
