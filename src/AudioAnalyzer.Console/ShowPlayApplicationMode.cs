using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>
/// Show play mode: full header, compact TextLayers toolbar, visualizer below.
/// </summary>
internal sealed class ShowPlayApplicationMode : IApplicationMode
{
    /// <inheritdoc />
    public ApplicationMode Key => ApplicationMode.ShowPlay;

    /// <inheritdoc />
    public string DisplayName => "Show play";

    /// <inheritdoc />
    public int HeaderLineCount => 3;

    /// <inheritdoc />
    public bool AllowsVisualizerFullscreen => true;

    /// <inheritdoc />
    public bool UsesGeneralSettingsHubKeyHandling => false;

    /// <inheritdoc />
    public bool TryHandleVisualizerKeys(ConsoleKeyInfo key, ITextLayerBoundsEditSession bounds, IVisualizer visualizer)
    {
        if (bounds.IsActive && bounds.HandleKey(key))
        {
            return true;
        }

        return visualizer.HandleKey(key);
    }

    /// <inheritdoc />
    public IReadOnlyList<IUiComponent> GetMainComponents(MainContentRenderArgs args)
    {
        bool layoutFullScreen = args.DisplayState.FullScreen;
        if (layoutFullScreen)
        {
            return [VisualizerAreaComponent.Instance];
        }

        int width = args.Context.Frame?.TerminalWidth ?? args.Context.Width;
        if (width < 30)
        {
            width = args.Context.Width;
        }

        (IReadOnlyList<LabeledValueDescriptor> descriptors, IReadOnlyList<int> widths) =
            MainContentToolbarLayout.BuildVisualizerToolbarRowData(
                args.Context,
                args.UiSettings,
                args.EffectiveUiPalette,
                args.Visualizer,
                args.PaletteForSwatch,
                args.PaletteDisplayName,
                width);
        args.ToolbarRow.SetRowData(descriptors, widths);
        return [args.ToolbarRow, VisualizerAreaComponent.Instance];
    }
}
