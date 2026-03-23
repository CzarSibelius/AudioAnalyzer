using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;

namespace AudioAnalyzer.Console;

/// <summary>Writes the universal title breadcrumb on a console row (ADR-0060).</summary>
internal static class TitleBarBreadcrumbRow
{
    /// <summary>Writes a truncated ANSI breadcrumb at (0, <paramref name="row"/>).</summary>
    public static void Write(int row, int width, ITitleBarBreadcrumbFormatter formatter)
    {
        string ansi = formatter.BuildAnsiLine();
        string line = StaticTextViewport.TruncateWithEllipsis(new AnsiText(ansi), width);
        try
        {
            System.Console.SetCursorPosition(0, row);
            System.Console.Write(line.PadRight(width));
        }
        catch (Exception ex)
        {
            _ = ex; /* Console write failed */
        }
    }
}
