using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Application.Display;

/// <summary>
/// Distributes toolbar segment widths across the full terminal width: each segment start aligns to an
/// 8-column grid; the first field is left-aligned, the last is flush right, and interior fields are spaced
/// toward the row center (ADR-0050 toolbar region).
/// </summary>
public static class ToolbarSegmentSpreadWidths
{
    /// <summary>Default column grid (8).</summary>
    public const int DefaultGrid = 8;

    /// <summary>
    /// Measures naturals then returns spread widths; falls back to <see cref="ToolbarSegmentPackedWidths.ComputePackedWidths"/>
    /// if spread constraints cannot be satisfied.
    /// </summary>
    public static int[] GetSpreadWidths(
        IReadOnlyList<LabeledValueDescriptor> descriptors,
        IReadOnlyList<int> naturals,
        int totalWidth,
        int grid = DefaultGrid)
    {
        if (descriptors.Count == 0)
        {
            return [];
        }

        if (naturals.Count != descriptors.Count)
        {
            throw new ArgumentException("naturals.Count must match descriptors.Count.", nameof(naturals));
        }

        int[]? spread = TryComputeSpread(naturals, totalWidth, grid);
        if (spread != null)
        {
            return spread;
        }

        return ToolbarSegmentPackedWidths.ComputePackedWidths(descriptors, naturals, totalWidth);
    }

    private static int[]? TryComputeSpread(IReadOnlyList<int> naturals, int totalWidth, int g)
    {
        int n = naturals.Count;
        if (totalWidth <= 0)
        {
            return [];
        }

        if (n == 1)
        {
            return [totalWidth];
        }

        var ns = new int[n];
        for (int i = 0; i < n; i++)
        {
            ns[i] = Math.Max(0, naturals[i]);
        }

        if (!CanFit(ns, totalWidth, g))
        {
            return null;
        }

        if (n == 2)
        {
            return SpreadTwo(ns, totalWidth, g);
        }

        if (n == 3)
        {
            return SpreadThree(ns, totalWidth, g);
        }

        return SpreadFourOrMore(ns, totalWidth, g);
    }

    private static bool CanFit(int[] ns, int w, int g)
    {
        int sum = 0;
        for (int i = 0; i < ns.Length; i++)
        {
            sum += ns[i];
        }

        return sum <= w;
    }

    private static int[]? SpreadTwo(int[] ns, int w, int g)
    {
        int n0 = ns[0];
        int n1 = ns[1];
        int s1 = AlignDown(w - n1, g);
        int low = AlignUp(n0, g);
        if (s1 < low)
        {
            s1 = low;
        }

        if (s1 + n1 > w)
        {
            return null;
        }

        return [s1, w - s1];
    }

    private static int[]? SpreadThree(int[] ns, int w, int g)
    {
        int n0 = ns[0];
        int n1 = ns[1];
        int n2 = ns[2];
        int s2 = AlignDown(w - n2, g);
        int minS2 = AlignUp(n0 + n1, g);
        if (s2 < minS2)
        {
            s2 = minS2;
        }

        if (s2 + n2 > w)
        {
            return null;
        }

        int ideal1 = (int)Math.Round(w / 2.0 - n1 / 2.0);
        int s1 = RoundToGrid(ideal1, g);
        int low1 = AlignUp(n0, g);
        int high1 = s2 - n1;
        if (low1 > high1)
        {
            return null;
        }

        if (s1 < low1)
        {
            s1 = low1;
        }

        if (s1 > high1)
        {
            s1 = high1;
        }

        s1 = RoundToGrid(s1, g);
        while (s1 < low1)
        {
            s1 += g;
        }

        while (s1 > high1)
        {
            s1 -= g;
        }

        if (s1 < low1 || s1 > high1)
        {
            s1 = low1;
        }

        int w0 = s1;
        int w1 = s2 - s1;
        int w2 = w - s2;
        if (w0 < n0 || w1 < n1 || w2 < n2)
        {
            return null;
        }

        return [w0, w1, w2];
    }

    private static int[]? SpreadFourOrMore(int[] ns, int w, int g)
    {
        int n = ns.Length;
        var s = new int[n];
        s[0] = 0;
        s[n - 1] = AlignDown(w - ns[n - 1], g);
        if (s[n - 1] + ns[n - 1] > w)
        {
            return null;
        }

        for (int i = 1; i < n - 1; i++)
        {
            double t = i / (double)(n - 1);
            int ideal = (int)Math.Round(s[n - 1] * t);
            s[i] = RoundToGrid(ideal, g);
        }

        for (int pass = 0; pass < n + 8; pass++)
        {
            for (int i = 1; i < n; i++)
            {
                int minSi = AlignUp(s[i - 1] + ns[i - 1], g);
                if (s[i] < minSi)
                {
                    s[i] = minSi;
                }
            }

            int lastStart = AlignDown(w - ns[n - 1], g);
            int minFromPrev = AlignUp(s[n - 2] + ns[n - 2], g);
            s[n - 1] = Math.Max(lastStart, minFromPrev);
            if (s[n - 1] + ns[n - 1] > w)
            {
                return null;
            }

            for (int i = n - 2; i >= 1; i--)
            {
                int maxSi = s[i + 1] - ns[i];
                int alignedMax = AlignDown(maxSi, g);
                if (s[i] > alignedMax)
                {
                    s[i] = alignedMax;
                }

                int minSi = AlignUp(s[i - 1] + ns[i - 1], g);
                if (s[i] < minSi)
                {
                    s[i] = minSi;
                }
            }
        }

        var widths = new int[n];
        for (int i = 0; i < n - 1; i++)
        {
            widths[i] = s[i + 1] - s[i];
            if (widths[i] < ns[i])
            {
                return null;
            }
        }

        widths[n - 1] = w - s[n - 1];
        if (widths[n - 1] < ns[n - 1])
        {
            return null;
        }

        return widths;
    }

    private static int AlignDown(int x, int g)
    {
        if (g <= 1)
        {
            return Math.Max(0, x);
        }

        return Math.Max(0, (x / g) * g);
    }

    private static int AlignUp(int x, int g)
    {
        if (g <= 1)
        {
            return Math.Max(0, x);
        }

        return ((Math.Max(0, x) + g - 1) / g) * g;
    }

    private static int RoundToGrid(int x, int g)
    {
        if (g <= 1)
        {
            return x;
        }

        return (int)Math.Round((double)x / g) * g;
    }
}
