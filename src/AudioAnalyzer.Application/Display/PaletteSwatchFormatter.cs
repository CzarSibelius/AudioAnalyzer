using System.Globalization;
using System.Text;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Display;

/// <summary>
/// Builds ANSI palette preview: per-grapheme colors for palette display names (toolbar, settings modal).
/// </summary>
public static class PaletteSwatchFormatter
{
    private const long TickMsForPhaseStep = 200;

    /// <summary>
    /// Renders <paramref name="text"/> with each text element colored from <paramref name="colors"/> in rotation,
    /// using <c>colors[(i + phaseOffset) % colors.Count]</c>. Returns plain <paramref name="text"/> when colors are null or empty.
    /// </summary>
    public static string FormatPaletteColoredName(string text, IReadOnlyList<PaletteColor>? colors, int phaseOffset)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        if (colors is not { Count: > 0 })
        {
            return text;
        }

        int n = colors.Count;
        var si = new StringInfo(text);
        int elementCount = si.LengthInTextElements;
        if (elementCount == 0)
        {
            return text;
        }

        var sb = new StringBuilder(text.Length + elementCount * 24);
        for (int e = 0; e < elementCount; e++)
        {
            string el = si.SubstringByTextElements(e, 1);
            int colorIndex = Mod(e + phaseOffset, n);
            sb.Append(AnsiConsole.ColorCode(colors[colorIndex]));
            sb.Append(el);
        }

        sb.Append(AnsiConsole.ResetCode);
        return sb.ToString();
    }

    /// <summary>
    /// Toolbar / main view: beat-driven phase when BPM is plausible; otherwise tick-based rotation.
    /// </summary>
    public static int ComputeToolbarPhaseOffset(AnalysisSnapshot snapshot, int colorCount)
    {
        if (colorCount <= 0)
        {
            return 0;
        }

        if (snapshot.CurrentBpm < 1.0)
        {
            return (int)(Environment.TickCount64 / TickMsForPhaseStep) % colorCount;
        }

        return Mod(snapshot.BeatCount, colorCount);
    }

    private static int Mod(int a, int n)
    {
        int r = a % n;
        return r < 0 ? r + n : r;
    }
}
