using System.Globalization;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Display;

/// <summary>Compact display strings and UI tier colors for per-layer <c>Draw</c> durations (ADR-0073).</summary>
public static class LayerRenderTimeFormatting
{
    /// <summary>60 Hz frame budget in milliseconds (reference main-loop target, ADR-0067 / ADR-0072).</summary>
    public const double FrameBudgetMsAt60Fps = 1000.0 / 60.0;

    /// <summary>
    /// Fair-share <c>Draw</c> budget per enabled layer at 60 FPS: <see cref="FrameBudgetMsAt60Fps"/> divided by enabled layer count (minimum divisor 1).
    /// </summary>
    public static double PerLayerBudgetMsFor60Fps(int enabledLayerCount) =>
        FrameBudgetMsAt60Fps / Math.Max(1, enabledLayerCount);

    /// <summary>
    /// Maps measured time vs per-layer budget to <see cref="UiPalette"/> foreground: over budget → <see cref="UiPalette.Highlighted"/>,
    /// between 25% and 100% of budget → <see cref="UiPalette.Normal"/>, at or below 25% → <see cref="UiPalette.Dimmed"/>.
    /// No measurement (disabled / skipped) uses <see cref="UiPalette.Dimmed"/> for the em dash.
    /// </summary>
    public static PaletteColor GetTierForeground(UiPalette palette, double? measuredMs, double perLayerBudgetMs)
    {
        if (measuredMs == null || perLayerBudgetMs <= 0)
        {
            return palette.Dimmed;
        }

        double ratio = measuredMs.Value / perLayerBudgetMs;
        if (ratio > 1.0)
        {
            return palette.Highlighted;
        }

        if (ratio > 0.25)
        {
            return palette.Normal;
        }

        return palette.Dimmed;
    }

    /// <summary>Returns a leading-space + timing or em dash for S modal suffix (e.g. <c> 0.12ms</c>, <c> —</c>).</summary>
    public static string FormatEntrySuffix(double? milliseconds)
    {
        if (milliseconds == null)
        {
            return " \u2014";
        }

        double v = milliseconds.Value;
        if (v < 0.01)
        {
            return " <0.01ms";
        }

        if (v < 10)
        {
            return " " + v.ToString("0.##", CultureInfo.InvariantCulture) + "ms";
        }

        return " " + ((int)Math.Round(v)).ToString(CultureInfo.InvariantCulture) + "ms";
    }
}
