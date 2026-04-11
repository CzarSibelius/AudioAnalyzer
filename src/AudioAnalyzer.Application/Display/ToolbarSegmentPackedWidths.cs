using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Application.Display;

/// <summary>
/// Computes toolbar segment display widths: natural measure per <see cref="LabeledValueDescriptor"/>,
/// then packed cell widths with trailing padding so each segment ends on an 8-column boundary (ADR-0050).
/// </summary>
public static class ToolbarSegmentPackedWidths
{
    /// <summary>Measures each descriptor the same way <see cref="ScrollingTextComponentRenderer"/> lays out one cell.</summary>
    public static int[] MeasureNaturalWidths(IReadOnlyList<LabeledValueDescriptor> descriptors)
    {
        int n = descriptors.Count;
        var naturals = new int[n];
        for (int i = 0; i < n; i++)
        {
            naturals[i] = MeasureOne(descriptors[i]);
        }

        return naturals;
    }

    /// <summary>
    /// Measures each descriptor and returns packed cell widths for one <see cref="HorizontalRowComponent"/> row
    /// (main content toolbar, header Device/Now, header BPM/Beat/Volume, etc.).
    /// </summary>
    /// <param name="descriptors">Cells left to right.</param>
    /// <param name="totalWidth">Terminal width for overflow shrink.</param>
    public static int[] GetPackedWidths(IReadOnlyList<LabeledValueDescriptor> descriptors, int totalWidth)
    {
        if (descriptors.Count == 0)
        {
            return [];
        }

        int[] naturals = MeasureNaturalWidths(descriptors);
        return ComputePackedWidths(descriptors, naturals, totalWidth);
    }

    /// <summary>
    /// Builds cell widths: each segment is natural width plus trailing spaces so the cell ends on a multiple of 8,
    /// except the last segment (no trailing snap). Shrinks when the sum exceeds <paramref name="totalWidth"/>.
    /// </summary>
    public static int[] ComputePackedWidths(
        IReadOnlyList<LabeledValueDescriptor> descriptors,
        IReadOnlyList<int> naturals,
        int totalWidth)
    {
        int n = descriptors.Count;
        if (n == 0)
        {
            return [];
        }

        if (naturals.Count != n)
        {
            throw new ArgumentException("naturals.Count must match descriptors.Count.", nameof(naturals));
        }

        var widths = new int[n];
        int cursor = 0;
        for (int i = 0; i < n; i++)
        {
            int natural = Math.Max(0, naturals[i]);
            int pad = 0;
            if (i < n - 1)
            {
                int end = cursor + natural;
                pad = (8 - (end % 8)) % 8;
            }

            widths[i] = natural + pad;
            cursor += widths[i];
        }

        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            sum += widths[i];
        }

        if (sum > totalWidth && totalWidth > 0)
        {
            ShrinkToFit(descriptors, naturals, widths, sum - totalWidth);
        }

        return widths;
    }

    private static int MeasureOne(LabeledValueDescriptor d)
    {
        if (d.PreformattedAnsi)
        {
            IDisplayText v = d.GetValue();
            return string.IsNullOrEmpty(v.Value) ? 0 : AnsiConsole.GetDisplayWidth(v.Value);
        }

        string effectiveLabel = LabelFormatting.FormatLabel(d.Label);
        int labelW = string.IsNullOrEmpty(effectiveLabel) ? 0 : DisplayWidth.GetDisplayWidth(effectiveLabel);
        IDisplayText text = d.GetValue();
        return labelW + text.GetDisplayWidth();
    }

    private static int MinWidthForShrink(LabeledValueDescriptor d, int natural)
    {
        if (d.PreformattedAnsi)
        {
            if (natural <= 0)
            {
                return 0;
            }

            return Math.Max(1, Math.Min(natural, 8));
        }

        string effectiveLabel = LabelFormatting.FormatLabel(d.Label);
        int labelW = string.IsNullOrEmpty(effectiveLabel) ? 0 : DisplayWidth.GetDisplayWidth(effectiveLabel);
        return Math.Min(natural, labelW + 4);
    }

    private static void ShrinkToFit(
        IReadOnlyList<LabeledValueDescriptor> descriptors,
        IReadOnlyList<int> naturals,
        int[] widths,
        int excess)
    {
        int n = widths.Length;
        while (excess > 0)
        {
            int best = -1;
            int bestScore = -1;
            for (int i = 0; i < n; i++)
            {
                int minShrink = MinWidthForShrink(descriptors[i], naturals[i]);
                int room = widths[i] - minShrink;
                if (room <= 0)
                {
                    continue;
                }

                int score = room * 1000 + (descriptors[i].PreformattedAnsi ? 0 : 1);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = i;
                }
            }

            if (best < 0)
            {
                break;
            }

            int minW = MinWidthForShrink(descriptors[best], naturals[best]);
            int take = Math.Min(excess, widths[best] - minW);
            widths[best] -= take;
            excess -= take;
        }
    }
}
