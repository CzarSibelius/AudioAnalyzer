using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using Xunit;

namespace AudioAnalyzer.Tests.Application;

/// <summary>Tests for <see cref="ToolbarSegmentPackedWidths"/> toolbar segment measurement and 8-column packing.</summary>
public sealed class ToolbarSegmentPackedWidthsTests
{
    [Fact]
    public void GetPackedWidths_matches_measure_and_compute()
    {
        var descriptors = new[]
        {
            new LabeledValueDescriptor("", () => new PlainText(new string('x', 14)), preformattedAnsi: true),
            new LabeledValueDescriptor("", () => new PlainText("z"), preformattedAnsi: true),
        };
        int[] expectedN = ToolbarSegmentPackedWidths.MeasureNaturalWidths(descriptors);
        int[] expectedW = ToolbarSegmentPackedWidths.ComputePackedWidths(descriptors, expectedN, totalWidth: 200);
        int[] actualW = ToolbarSegmentPackedWidths.GetPackedWidths(descriptors, totalWidth: 200);
        Assert.Equal(expectedW, actualW);
    }

    [Fact]
    public void ComputePackedWidths_snapsEndsToEightColumns()
    {
        var descriptors = new[]
        {
            new LabeledValueDescriptor("", () => new PlainText(new string('x', 14)), preformattedAnsi: true),
            new LabeledValueDescriptor("", () => new PlainText(new string('y', 10)), preformattedAnsi: true),
        };
        int[] naturals = ToolbarSegmentPackedWidths.MeasureNaturalWidths(descriptors);
        Assert.Equal(14, naturals[0]);
        Assert.Equal(10, naturals[1]);

        int[] widths = ToolbarSegmentPackedWidths.ComputePackedWidths(descriptors, naturals, totalWidth: 200);
        Assert.Equal(16, widths[0]);
        Assert.Equal(10, widths[1]);
    }

    [Fact]
    public void ComputePackedWidths_middleSegmentGetsSnapPad_beforeLast()
    {
        var descriptors = new[]
        {
            new LabeledValueDescriptor("", () => new PlainText(new string('x', 14)), preformattedAnsi: true),
            new LabeledValueDescriptor("", () => new PlainText(new string('y', 10)), preformattedAnsi: true),
            new LabeledValueDescriptor("", () => new PlainText("z"), preformattedAnsi: true),
        };
        int[] naturals = ToolbarSegmentPackedWidths.MeasureNaturalWidths(descriptors);
        int[] widths = ToolbarSegmentPackedWidths.ComputePackedWidths(descriptors, naturals, totalWidth: 200);
        Assert.Equal(16, widths[0]);
        Assert.Equal(16, widths[1]);
        Assert.Equal(1, widths[2]);
    }

    [Fact]
    public void ComputePackedWidths_lastSegmentHasNoTrailingSnapPad()
    {
        var descriptors = new[]
        {
            new LabeledValueDescriptor("A", () => new PlainText("b"), preformattedAnsi: false),
        };
        int[] naturals = ToolbarSegmentPackedWidths.MeasureNaturalWidths(descriptors);
        int[] widths = ToolbarSegmentPackedWidths.ComputePackedWidths(descriptors, naturals, totalWidth: 120);
        Assert.Equal(naturals[0], widths[0]);
    }

    [Fact]
    public void MeasureNaturalWidths_labeledCell_isLabelPlusValueDisplayWidth()
    {
        var descriptors = new[]
        {
            new LabeledValueDescriptor("Layers", () => new PlainText("12")),
        };
        int[] naturals = ToolbarSegmentPackedWidths.MeasureNaturalWidths(descriptors);
        int labelCols = DisplayWidth.GetDisplayWidth("Layers:");
        Assert.Equal(labelCols + 2, naturals[0]);
    }

    [Fact]
    public void MeasureNaturalWidths_preformatted_usesAnsiDisplayWidth()
    {
        var descriptors = new[]
        {
            new LabeledValueDescriptor(
                "Layers",
                () => new AnsiText("\x1b[31mLayers:\x1b[0m12"),
                preformattedAnsi: true),
        };
        int[] naturals = ToolbarSegmentPackedWidths.MeasureNaturalWidths(descriptors);
        Assert.Equal(9, naturals[0]);
    }

    [Fact]
    public void ComputePackedWidths_shrinksWhenSumExceedsTotalWidth()
    {
        var descriptors = new[]
        {
            new LabeledValueDescriptor("Long", () => new PlainText(new string('z', 30)), preformattedAnsi: false),
            new LabeledValueDescriptor("X", () => new PlainText("y"), preformattedAnsi: false),
        };
        int[] naturals = ToolbarSegmentPackedWidths.MeasureNaturalWidths(descriptors);
        int[] widths = ToolbarSegmentPackedWidths.ComputePackedWidths(descriptors, naturals, totalWidth: 20);
        int sum = widths[0] + widths[1];
        Assert.True(sum <= 20, $"expected sum <= 20, got {sum}");
    }
}
