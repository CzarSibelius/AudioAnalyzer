using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>
/// Canonical selectable-row affordance per <c>docs/adr/0069-unified-menu-selection-affordance.md</c>:
/// leading <see cref="SelectedPrefix"/> / <see cref="UnselectedPrefix"/> and full-width background + foreground from the effective UI theme.
/// </summary>
internal static class MenuSelectionAffordance
{
    /// <summary>Three-column prefix for the selected row (space, U+25BA BLACK RIGHT-POINTING POINTER, space).</summary>
    public const string SelectedPrefix = " ► ";

    /// <summary>Three spaces so rows align with <see cref="SelectedPrefix"/>.</summary>
    public const string UnselectedPrefix = "   ";

    /// <summary>Display width of <see cref="SelectedPrefix"/> (matches <see cref="UnselectedPrefix"/>).</summary>
    public static int PrefixDisplayWidth => DisplayWidth.GetDisplayWidth(SelectedPrefix);

    /// <summary>Returns selection background (with default fallback) and foreground from <paramref name="palette"/>.</summary>
    public static (PaletteColor Background, PaletteColor Foreground) GetSelectionColors(UiPalette palette)
    {
        var bg = palette.Background ?? PaletteColor.FromConsoleColor(ConsoleColor.DarkBlue);
        return (bg, palette.Highlighted);
    }

    /// <summary>Leading prefix for a row based on selection.</summary>
    public static string GetPrefix(bool selected) => selected ? SelectedPrefix : UnselectedPrefix;

    /// <summary>
    /// When <paramref name="selected"/> is true, wraps <paramref name="paddedRow"/> (already padded to target width) with selection ANSI.
    /// </summary>
    public static string ApplyRowHighlight(bool selected, string paddedRow, PaletteColor selBg, PaletteColor selFg) =>
        selected
            ? AnsiConsole.BackgroundCode(selBg) + AnsiConsole.ColorCode(selFg) + paddedRow + AnsiConsole.ResetCode
            : paddedRow;

    /// <summary>
    /// Pads plain or ANSI content to <paramref name="targetDisplayWidth"/>; when selected, applies selection background across the full width including padding spaces.
    /// </summary>
    public static string FormatAnsiSelectableRow(
        bool selected,
        string innerContent,
        int targetDisplayWidth,
        PaletteColor selBg,
        PaletteColor selFg)
    {
        if (!selected)
        {
            return AnsiConsole.PadToDisplayWidth(innerContent, targetDisplayWidth);
        }

        var open = AnsiConsole.BackgroundCode(selBg) + AnsiConsole.ColorCode(selFg);
        int w = AnsiConsole.GetDisplayWidth(innerContent);
        int pad = Math.Max(0, targetDisplayWidth - w);
        return open + innerContent + new string(' ', pad) + AnsiConsole.ResetCode;
    }
}
