using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>
/// Renders a <see cref="HorizontalRowComponent"/> as one line by laying out each child cell with the ScrollingTextComponent renderer and concatenating results.
/// </summary>
internal sealed class HorizontalRowComponentRenderer : IUiComponentRenderer<HorizontalRowComponent>
{
    private readonly IUiComponentRenderer<ScrollingTextComponent> _cellRenderer;

    public HorizontalRowComponentRenderer(IUiComponentRenderer<ScrollingTextComponent> cellRenderer)
    {
        _cellRenderer = cellRenderer ?? throw new ArgumentNullException(nameof(cellRenderer));
    }

    /// <inheritdoc />
    public ComponentRenderResult Render(HorizontalRowComponent component, RenderContext context)
    {
        int count = Math.Min(component.Children.Count, component.Widths.Count);
        if (count == 0)
        {
            string empty = context.Width > 0 ? new string(' ', context.Width) : "";
            WriteLine(context.StartRow, empty);
            return ComponentRenderResult.Written(1);
        }

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < count; i++)
        {
            int cellWidth = i < component.Widths.Count ? Math.Max(0, component.Widths[i]) : 0;
            var childContext = new RenderContext
            {
                Width = cellWidth,
                StartRow = context.StartRow,
                MaxLines = context.MaxLines,
                Palette = context.Palette,
                ScrollSpeed = context.ScrollSpeed,
                DeviceName = context.DeviceName,
                Snapshot = context.Snapshot,
                PaletteDisplayName = context.PaletteDisplayName,
                InvalidateWriteCache = context.InvalidateWriteCache
            };
            ComponentRenderResult result = _cellRenderer.Render(component.Children[i], childContext);
            string part = result.LineContents is { Count: > 0 } ? result.LineContents[0] : (cellWidth > 0 ? new string(' ', cellWidth) : "");
            sb.Append(part);
        }

        string line = sb.ToString();
        int displayWidth = AnsiConsole.GetDisplayWidth(line);
        if (displayWidth > context.Width)
        {
            line = AnsiConsole.GetDisplaySubstring(line, 0, context.Width);
        }
        else if (displayWidth < context.Width)
        {
            line = AnsiConsole.PadToDisplayWidth(line, context.Width);
        }

        WriteLine(context.StartRow, line);
        return ComponentRenderResult.Written(1);
    }

    private static void WriteLine(int row, string line)
    {
        try
        {
            System.Console.SetCursorPosition(0, row);
            System.Console.Write(line);
        }
        catch (Exception ex)
        {
            _ = ex; /* Console write unavailable: swallow to avoid crash */
        }
    }
}
