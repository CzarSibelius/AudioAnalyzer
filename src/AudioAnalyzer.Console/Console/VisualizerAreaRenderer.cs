using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Renders the visualizer area (spectrum/layers) via the component tree. Clears the region once when needed; delegates to IVisualizer.</summary>
internal sealed class VisualizerAreaRenderer : IUiComponentRenderer<VisualizerAreaComponent>
{
    private readonly IVisualizer _visualizer;
    private bool _visualizerAreaCleared;

    public VisualizerAreaRenderer(IVisualizer visualizer)
    {
        _visualizer = visualizer ?? throw new ArgumentNullException(nameof(visualizer));
    }

    /// <inheritdoc />
    public ComponentRenderResult Render(VisualizerAreaComponent component, RenderContext context)
    {
        if (context.Frame == null)
        {
            return ComponentRenderResult.Written(0);
        }

        int startRow = context.StartRow;
        int maxLines = context.MaxLines;
        int width = context.Width;

        if (maxLines <= 0 || width <= 0)
        {
            return ComponentRenderResult.Written(0);
        }

        if (!_visualizerAreaCleared)
        {
            ClearRegion(startRow, maxLines, width);
            _visualizerAreaCleared = true;
        }

        var viewport = new VisualizerViewport(startRow, maxLines, width);

        try
        {
            _visualizer.Render(context.Frame, viewport);
        }
        catch (Exception ex)
        {
            string message = !string.IsNullOrWhiteSpace(ex.Message) ? ex.Message : "Visualization error";
            try
            {
                System.Console.SetCursorPosition(0, startRow);
                System.Console.Write(StaticTextViewport.TruncateWithEllipsis(new PlainText(message), viewport.Width));
            }
            catch (Exception writeEx)
            {
                _ = writeEx; /* Console write failed */
            }
        }

        return ComponentRenderResult.Written(maxLines);
    }

    /// <inheritdoc />
    public void ResetVisualizerAreaCleared()
    {
        _visualizerAreaCleared = false;
    }

    private static void ClearRegion(int startRow, int lineCount, int width)
    {
        if (width <= 0 || lineCount <= 0)
        {
            return;
        }

        string blank = new string(' ', width);
        try
        {
            for (int i = 0; i < lineCount; i++)
            {
                System.Console.SetCursorPosition(0, startRow + i);
                System.Console.Write(blank);
            }
        }
        catch (Exception ex)
        {
            _ = ex; /* Console write failed in ClearRegion */
        }
    }
}
