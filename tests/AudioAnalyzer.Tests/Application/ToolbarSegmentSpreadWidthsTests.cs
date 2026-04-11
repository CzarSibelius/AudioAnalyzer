using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using Xunit;

namespace AudioAnalyzer.Tests.Application;

/// <summary>Tests for <see cref="ToolbarSegmentSpreadWidths"/>.</summary>
public sealed class ToolbarSegmentSpreadWidthsTests
{
    [Fact]
    public void SpreadTwo_uses_full_width_and_aligns_split_to_eight()
    {
        var descriptors = new[]
        {
            new LabeledValueDescriptor("A", () => new PlainText("x"), preformattedAnsi: false),
            new LabeledValueDescriptor("B", () => new PlainText("yy"), preformattedAnsi: false),
        };
        int[] naturals = ToolbarSegmentPackedWidths.MeasureNaturalWidths(descriptors);
        int[] w = ToolbarSegmentSpreadWidths.GetSpreadWidths(descriptors, naturals, totalWidth: 80);
        Assert.Equal(2, w.Length);
        Assert.Equal(80, w[0] + w[1]);
        Assert.Equal(0, w[0] % 8);
    }

    [Fact]
    public void SpreadThree_middle_near_center_starts_on_grid()
    {
        var descriptors = new[]
        {
            new LabeledValueDescriptor("", () => new PlainText(new string('x', 8)), preformattedAnsi: true),
            new LabeledValueDescriptor("", () => new PlainText(new string('y', 8)), preformattedAnsi: true),
            new LabeledValueDescriptor("", () => new PlainText(new string('z', 8)), preformattedAnsi: true),
        };
        int[] naturals = [8, 8, 8];
        int[] w = ToolbarSegmentSpreadWidths.GetSpreadWidths(descriptors, naturals, totalWidth: 80);
        Assert.Equal(80, w[0] + w[1] + w[2]);
        Assert.Equal(0, w[0] % 8);
        Assert.Equal(0, (w[0] + w[1]) % 8);
    }

    [Fact]
    public void ReservedBeatSegmentDisplayWidth_is_stable()
    {
        int w = ToolbarBeatSegmentLayout.ReservedBeatSegmentDisplayWidth();
        Assert.True(w >= 16);
    }
}
