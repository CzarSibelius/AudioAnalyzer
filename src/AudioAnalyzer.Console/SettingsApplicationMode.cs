using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>
/// General settings mode: minimal header (title breadcrumb only), hub toolbar hint, settings hub main area.
/// </summary>
internal sealed class SettingsApplicationMode : IApplicationMode
{
    /// <inheritdoc />
    public ApplicationMode Key => ApplicationMode.Settings;

    /// <inheritdoc />
    public string DisplayName => "General settings";

    /// <inheritdoc />
    public int HeaderLineCount => 1;

    /// <inheritdoc />
    public bool AllowsVisualizerFullscreen => false;

    /// <inheritdoc />
    public bool UsesGeneralSettingsHubKeyHandling => true;

    /// <inheritdoc />
    public bool TryHandleVisualizerKeys(ConsoleKeyInfo key, ITextLayerBoundsEditSession bounds, IVisualizer visualizer)
    {
        _ = key;
        _ = bounds;
        _ = visualizer;
        return false;
    }

    /// <inheritdoc />
    public IReadOnlyList<IUiComponent> GetMainComponents(MainContentRenderArgs args)
    {
        int width = args.Context.Snapshot?.TerminalWidth ?? args.Context.Width;
        if (width < 30)
        {
            width = args.Context.Width;
        }

        (IReadOnlyList<LabeledValueDescriptor> descriptors, IReadOnlyList<int> widths) =
            MainContentToolbarLayout.BuildGeneralSettingsToolbarRowData(
                args.Context,
                args.UiSettings,
                args.EffectiveUiPalette,
                args.Visualizer,
                args.PaletteForSwatch,
                args.PaletteDisplayName,
                width);
        args.ToolbarRow.SetRowData(descriptors, widths);
        return [args.ToolbarRow, GeneralSettingsHubAreaComponent.Instance];
    }
}
