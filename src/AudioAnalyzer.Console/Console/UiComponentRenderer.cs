using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>
/// Dispatches rendering to the correct concrete renderer based on component type.
/// Writes line content from leaf renderers at context.StartRow. No component-type-specific logic; only resolves IUiComponentRenderer&lt;T&gt; per leaf.
/// </summary>
internal sealed class UiComponentRenderer : IUiComponentRenderer<IUiComponent>
{
    private readonly IUiComponentRenderer<LabeledRowComponent> _labeledRow;
    private readonly IUiComponentRenderer<VisualizerAreaComponent> _visualizerArea;

    public UiComponentRenderer(
        IUiComponentRenderer<LabeledRowComponent> labeledRow,
        IUiComponentRenderer<VisualizerAreaComponent> visualizerArea)
    {
        _labeledRow = labeledRow ?? throw new ArgumentNullException(nameof(labeledRow));
        _visualizerArea = visualizerArea ?? throw new ArgumentNullException(nameof(visualizerArea));
    }

    /// <inheritdoc />
    public ComponentRenderResult Render(IUiComponent component, RenderContext context)
    {
        if (context == null)
        {
            return ComponentRenderResult.Written(0);
        }

        if (context.InvalidateWriteCache)
        {
            context.InvalidateWriteCache = false;
        }

        IReadOnlyList<IUiComponent>? children = component.GetChildren(context);
        if (children != null && children.Count > 0)
        {
            int total = 0;
            foreach (IUiComponent child in children)
            {
                ComponentRenderResult result = Render(child, context);
                WriteResult(context.StartRow, result);
                context.StartRow += result.LinesConsumed;
                total += result.LinesConsumed;
            }
            return new ComponentRenderResult(total, null);
        }

        return component switch
        {
            LabeledRowComponent => _labeledRow.Render((LabeledRowComponent)component, context),
            VisualizerAreaComponent => _visualizerArea.Render((VisualizerAreaComponent)component, context),
            _ => ComponentRenderResult.Written(0)
        };
    }

    private static void WriteResult(int startRow, ComponentRenderResult result)
    {
        if (result.LineContents == null || result.LineContents.Count == 0)
        {
            return;
        }
        for (int i = 0; i < result.LineContents.Count; i++)
        {
            WriteLine(startRow + i, result.LineContents[i]);
        }
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

    /// <summary>Resets the visualizer area cleared flag (e.g. when switching fullscreen).</summary>
    public void ResetVisualizerAreaCleared()
    {
        _visualizerArea.ResetVisualizerAreaCleared();
    }
}
