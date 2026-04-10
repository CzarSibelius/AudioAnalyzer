using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Domain;
using Microsoft.Extensions.Logging;

namespace AudioAnalyzer.Console;

/// <summary>Renders the visualizer area (spectrum/layers) via the component tree. Clears the region once when needed; delegates to IVisualizer.</summary>
internal sealed partial class VisualizerAreaRenderer : IUiComponentRenderer<VisualizerAreaComponent>
{
    private readonly IVisualizer _visualizer;
    private readonly ILogger<VisualizerAreaRenderer> _logger;
    private bool _visualizerAreaCleared;

    public VisualizerAreaRenderer(IVisualizer visualizer, ILogger<VisualizerAreaRenderer> logger)
    {
        _visualizer = visualizer ?? throw new ArgumentNullException(nameof(visualizer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            ClearRegionInternal(startRow, maxLines, width);
            _visualizerAreaCleared = true;
        }

        var viewport = new VisualizerViewport(startRow, maxLines, width);

        try
        {
            _visualizer.Render(context.Frame, viewport);
        }
        catch (Exception ex)
        {
            LogVisualizerRenderFailed(ex);
            string message = !string.IsNullOrWhiteSpace(ex.Message) ? ex.Message : "Visualization error";
            try
            {
                System.Console.SetCursorPosition(0, startRow);
                System.Console.Write(StaticTextViewport.TruncateWithEllipsis(new PlainText(message), viewport.Width));
            }
            catch (Exception writeEx)
            {
                LogVisualizerConsoleWriteFailed(writeEx);
            }
        }

        return ComponentRenderResult.Written(maxLines);
    }

    /// <inheritdoc />
    public void ResetVisualizerAreaCleared()
    {
        _visualizerAreaCleared = false;
    }

    private void ClearRegionInternal(int startRow, int lineCount, int width)
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
            LogClearRegionConsoleWriteFailed(ex);
        }
    }

    [LoggerMessage(EventId = 7601, Level = LogLevel.Error, Message = "Visualizer render failed")]
    private partial void LogVisualizerRenderFailed(Exception ex);

    [LoggerMessage(EventId = 7602, Level = LogLevel.Warning, Message = "Failed to write visualizer error to console")]
    private partial void LogVisualizerConsoleWriteFailed(Exception ex);

    [LoggerMessage(EventId = 7603, Level = LogLevel.Warning, Message = "ClearRegion console write failed")]
    private partial void LogClearRegionConsoleWriteFailed(Exception ex);
}
