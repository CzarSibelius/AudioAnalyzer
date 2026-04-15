using System.Globalization;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Charset list for the settings modal (ADR-0080).</summary>
internal static class SettingsSurfacesCharsetDrawing
{
    /// <summary>Draws the charset picker in the right column.</summary>
    public static void DrawPicker(
        ICharsetRepository charsetRepo,
        SettingsModalState state,
        int leftColWidth,
        int contentStartRow,
        int visibleRows,
        int rightColWidth,
        PaletteColor selBg,
        PaletteColor selFg,
        bool includeLegacySnippetsRow)
    {
        var list = BuildEntryList(charsetRepo, includeLegacySnippetsRow);
        int count = list.Count;
        if (count == 0)
        {
            return;
        }

        int selected = Math.Clamp(state.CharsetPickerSelectedIndex, 0, count - 1);
        int scroll = count <= visibleRows
            ? 0
            : Math.Clamp(selected - (visibleRows - 1), 0, count - visibleRows);

        for (int vi = 0; vi < visibleRows; vi++)
        {
            int gi = scroll + vi;
            if (gi >= count)
            {
                break;
            }

            bool rowSel = gi == selected;
            string aff = MenuSelectionAffordance.GetPrefix(rowSel);
            string lineText = $"{aff}{list[gi].Label}";
            string line = StaticTextViewport.TruncateWithEllipsis(new PlainText(lineText), rightColWidth);
            string linePadded = AnsiConsole.PadToDisplayWidth(line, rightColWidth);
            System.Console.SetCursorPosition(leftColWidth + 1, contentStartRow + vi);
            System.Console.Write(MenuSelectionAffordance.ApplyRowHighlight(rowSel, linePadded, selBg, selFg));
        }
    }

    /// <summary>Rows for settings list preview (label:value style).</summary>
    public static string FormatCharsetSettingRow(
        string displayValue,
        int rightColWidth,
        bool rowSelected,
        PaletteColor selBg,
        PaletteColor selFg)
    {
        string aff = MenuSelectionAffordance.GetPrefix(rowSelected);
        string lineText = $"{aff}Charset:{displayValue}";
        string line = StaticTextViewport.TruncateWithEllipsis(new PlainText(lineText), rightColWidth);
        string linePadded = AnsiConsole.PadToDisplayWidth(line, rightColWidth);
        return MenuSelectionAffordance.ApplyRowHighlight(rowSelected, linePadded, selBg, selFg);
    }

    internal static IReadOnlyList<(string? Id, string Label)> BuildEntryList(
        ICharsetRepository charsetRepo,
        bool includeLegacySnippetsRow)
    {
        var rows = new List<(string? Id, string Label)>();
        if (includeLegacySnippetsRow)
        {
            rows.Add((null, "Legacy (TextSnippets)"));
        }

        foreach (var c in charsetRepo.GetAll())
        {
            rows.Add((c.Id, string.Format(CultureInfo.InvariantCulture, "{0} — {1}", c.Id, c.Name)));
        }

        return rows;
    }

    internal static int GetEntryCount(ICharsetRepository charsetRepo, bool includeLegacySnippetsRow) =>
        BuildEntryList(charsetRepo, includeLegacySnippetsRow).Count;

    internal static void ApplySelectionIndex(
        ICharsetRepository charsetRepo,
        bool includeLegacySnippetsRow,
        int index,
        Action<string?> setCharsetId)
    {
        var list = BuildEntryList(charsetRepo, includeLegacySnippetsRow);
        if (index < 0 || index >= list.Count)
        {
            return;
        }

        setCharsetId(list[index].Id);
    }

    internal static int FindIndexForCharsetId(
        ICharsetRepository charsetRepo,
        bool includeLegacySnippetsRow,
        string? charsetId,
        string defaultIdWhenNullWithoutLegacy)
    {
        var list = BuildEntryList(charsetRepo, includeLegacySnippetsRow);
        if (list.Count == 0)
        {
            return 0;
        }

        string? needle = charsetId;
        if (!includeLegacySnippetsRow && string.IsNullOrWhiteSpace(needle))
        {
            needle = defaultIdWhenNullWithoutLegacy;
        }

        if (includeLegacySnippetsRow && string.IsNullOrWhiteSpace(charsetId))
        {
            return 0;
        }

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Id != null && string.Equals(list[i].Id, needle, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return 0;
    }
}
