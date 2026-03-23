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

    /// <summary>Draws the device list starting at <paramref name="startRow"/> (full width). Rows use ► / spaces prefix and optional (current) suffix.</summary>
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
            string prefix = i == selectedIndex ? " ► " : "   ";
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
                lineToWrite = AnsiConsole.BackgroundCode(selBg) + AnsiConsole.ColorCode(selFg) + line + AnsiConsole.ResetCode;
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
}
