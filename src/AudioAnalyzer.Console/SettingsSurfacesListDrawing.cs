using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Shared vertical selectable-list drawing for settings surfaces (e.g. device selection modal).</summary>
internal static class SettingsSurfacesListDrawing
{
    /// <summary>Scroll offset so the selected index stays visible in a window of <paramref name="visibleCount"/> lines.</summary>
    public static int ComputeListScrollOffset(int selectedIndex, int totalCount, int visibleCount)
    {
        if (totalCount <= visibleCount)
        {
            return 0;
        }

        return Math.Clamp(selectedIndex - (visibleCount - 1), 0, totalCount - visibleCount);
    }

    /// <summary>Draws the device list starting at <paramref name="startRow"/> (full width). Rows use <see cref="MenuSelectionAffordance"/> prefix and optional (current) suffix.</summary>
    public static void DrawAudioDeviceList(
        int startRow,
        int width,
        IReadOnlyList<AudioDeviceEntry> devices,
        int selectedIndex,
        string? currentDeviceName,
        PaletteColor selBg,
        PaletteColor selFg,
        PaletteColor currentColor)
    {
        for (int i = 0; i < devices.Count; i++)
        {
            bool isCurrent = currentDeviceName != null && devices[i].Name == currentDeviceName;
            string prefix = MenuSelectionAffordance.GetPrefix(i == selectedIndex);
            string suffix = isCurrent ? " (current)" : "";
            string line = $"{prefix}{devices[i].Name}{suffix}";
            if (line.Length < width - 1)
            {
                line = line.PadRight(width - 1);
            }
            else
            {
                line = line[..(width - 1)];
            }

            string lineToWrite;
            if (i == selectedIndex)
            {
                lineToWrite = MenuSelectionAffordance.ApplyRowHighlight(true, line, selBg, selFg);
            }
            else if (isCurrent)
            {
                lineToWrite = AnsiConsole.ColorCode(currentColor) + line + AnsiConsole.ResetCode;
            }
            else
            {
                lineToWrite = line;
            }

            try
            {
                System.Console.SetCursorPosition(0, startRow + i);
                System.Console.WriteLine(lineToWrite);
            }
            catch (Exception ex)
            {
                _ = ex; /* Console write unavailable: swallow to avoid crash */
            }
        }

        try
        {
            System.Console.SetCursorPosition(0, startRow + devices.Count);
            System.Console.WriteLine(new string(' ', width - 1));
        }
        catch (Exception ex)
        {
            _ = ex;
        }
    }

    /// <summary>Scrollable UI theme list: row 0 is (Custom), then repository palettes with colored names.</summary>
    public static void DrawUiThemePaletteList(
        int startRow,
        int width,
        IPaletteRepository paletteRepo,
        IReadOnlyList<PaletteInfo> palettes,
        int selectedIndex,
        int scrollOffset,
        int visibleRows,
        string? currentThemePaletteId,
        PaletteColor selBg,
        PaletteColor selFg,
        PaletteColor currentHighlightColor,
        AnalysisSnapshot analysisSnapshot)
    {
        int total = 1 + palettes.Count;
        int rowWidth = Math.Max(8, width - 1);
        for (int vi = 0; vi < visibleRows; vi++)
        {
            int globalIndex = scrollOffset + vi;
            if (globalIndex >= total)
            {
                try
                {
                    System.Console.SetCursorPosition(0, startRow + vi);
                    System.Console.Write(new string(' ', Math.Max(0, width - 1)));
                }
                catch (Exception ex)
                {
                    _ = ex;
                }

                continue;
            }

            bool rowSelected = globalIndex == selectedIndex;
            string lineToWrite;
            if (globalIndex == 0)
            {
                bool customCurrent = string.IsNullOrWhiteSpace(currentThemePaletteId);
                string core = "(Custom)" + (customCurrent ? " (current)" : "");
                string prefix = MenuSelectionAffordance.GetPrefix(rowSelected);
                string inner = prefix + core;
                if (rowSelected)
                {
                    lineToWrite = MenuSelectionAffordance.FormatAnsiSelectableRow(true, inner, rowWidth, selBg, selFg);
                }
                else if (customCurrent)
                {
                    string colored = AnsiConsole.ColorCode(currentHighlightColor) + inner + AnsiConsole.ResetCode;
                    lineToWrite = AnsiConsole.PadToDisplayWidth(colored, rowWidth);
                }
                else
                {
                    lineToWrite = AnsiConsole.PadToDisplayWidth(inner, rowWidth);
                }
            }
            else
            {
                PaletteInfo p = palettes[globalIndex - 1];
                string displayName = !string.IsNullOrWhiteSpace(p.Name?.Trim()) ? p.Name!.Trim() : p.Id;
                bool isCurrent = !string.IsNullOrWhiteSpace(currentThemePaletteId)
                    && string.Equals(p.Id, currentThemePaletteId.Trim(), StringComparison.OrdinalIgnoreCase);
                lineToWrite = SettingsSurfacesPaletteDrawing.FormatPickerPaletteRow(
                    paletteRepo,
                    p.Id,
                    displayName + (isCurrent ? " (current)" : ""),
                    rowWidth,
                    rowSelected,
                    selBg,
                    selFg,
                    analysisSnapshot);
            }

            try
            {
                System.Console.SetCursorPosition(0, startRow + vi);
                if (AnsiConsole.GetDisplayWidth(lineToWrite) > width - 1)
                {
                    System.Console.Write(StaticTextViewport.TruncateToWidth(new AnsiText(lineToWrite), width - 1));
                }
                else
                {
                    System.Console.Write(lineToWrite);
                }
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }
    }
}
