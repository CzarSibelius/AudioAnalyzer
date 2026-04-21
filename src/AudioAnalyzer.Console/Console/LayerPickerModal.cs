using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;

namespace AudioAnalyzer.Console;

/// <summary>Keyboard-driven layer type list overlay (L) in Preset editor per ADR-0069 list affordance.</summary>
internal sealed class LayerPickerModal : ILayerPickerModal
{
    private const int BreadcrumbRow = 0;
    private const int HintRow = 1;
    private const int FirstListRow = 2;

    private readonly IVisualizationOrchestrator _orchestrator;
    private readonly IKeyHandler<LayerPickerKeyContext> _keyHandler;
    private readonly IUiThemeResolver _uiThemeResolver;
    private readonly IConsoleDimensions _consoleDimensions;
    private readonly ITitleBarNavigationContext _navigation;
    private readonly ITitleBarBreadcrumbFormatter _breadcrumbFormatter;

    public LayerPickerModal(
        IVisualizationOrchestrator orchestrator,
        IKeyHandler<LayerPickerKeyContext> keyHandler,
        IUiThemeResolver uiThemeResolver,
        IConsoleDimensions consoleDimensions,
        ITitleBarNavigationContext navigation,
        ITitleBarBreadcrumbFormatter breadcrumbFormatter)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _keyHandler = keyHandler ?? throw new ArgumentNullException(nameof(keyHandler));
        _uiThemeResolver = uiThemeResolver ?? throw new ArgumentNullException(nameof(uiThemeResolver));
        _consoleDimensions = consoleDimensions ?? throw new ArgumentNullException(nameof(consoleDimensions));
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        _breadcrumbFormatter = breadcrumbFormatter ?? throw new ArgumentNullException(nameof(breadcrumbFormatter));
    }

    /// <inheritdoc />
    public void Show(
        object consoleLock,
        Action<bool> setModalOpen,
        IVisualizationRenderer renderer,
        VisualizerSettings visualizerSettings,
        IVisualizer visualizer,
        Action persistAndRedraw,
        TitleBarViewKind? restoreTitleBarViewWhenClosed = null,
        int? restoreOverlayRowCountWhenClosed = null)
    {
        var sorted = TextLayersSortedLayers.BuildSortedByZOrderCopy(visualizerSettings.TextLayers);
        if (sorted is not { Count: > 0 })
        {
            return;
        }

        IReadOnlyList<TextLayerType> pickable = TextLayerPickerCatalog.OrderedTypes;
        int activeSlot = Math.Clamp(visualizer.GetActiveLayerZIndex(), 0, sorted.Count - 1);
        TextLayerType currentType = sorted[activeSlot].LayerType;
        int selected = 0;
        for (int i = 0; i < pickable.Count; i++)
        {
            if (pickable[i] == currentType)
            {
                selected = i;
                break;
            }
        }

        int overlayRowCount = FirstListRow + pickable.Count;
        var context = new LayerPickerKeyContext
        {
            PickableTypes = pickable,
            SelectedIndex = selected,
            ConfirmSelection = false
        };

        int width = _consoleDimensions.GetConsoleWidth();
        var palette = _uiThemeResolver.GetEffectiveUiPalette();
        var (selBg, selFg) = MenuSelectionAffordance.GetSelectionColors(palette);
        string dimCode = AnsiConsole.ColorCode(palette.Dimmed);
        string reset = AnsiConsole.ResetCode;

        void DrawContent()
        {
            try
            {
                TitleBarBreadcrumbRow.Write(0, width, _breadcrumbFormatter);
                System.Console.SetCursorPosition(0, HintRow);
                int slotOneBased = activeSlot + 1;
                string hint =
                    $"  Slot {slotOneBased} of {sorted.Count}: pick layer type. Use \u2191/\u2193 or +/-, ENTER to apply, ESC to cancel";
                System.Console.Write(StaticTextViewport.TruncateWithEllipsis(new PlainText(hint), width).PadRight(width));

                for (int i = 0; i < pickable.Count; i++)
                {
                    int row = FirstListRow + i;
                    System.Console.SetCursorPosition(0, row);
                    TextLayerType type = pickable[i];
                    bool rowSel = i == context.SelectedIndex;
                    string typeLabel = type.ToString();
                    string suffix = type == currentType ? dimCode + " (current)" + reset : "";
                    string prefix = MenuSelectionAffordance.GetPrefix(rowSel);
                    string inner = $"{prefix}{typeLabel}{suffix}";
                    inner = StaticTextViewport.TruncateWithEllipsis(new PlainText(inner), width);
                    string line = MenuSelectionAffordance.ApplyRowHighlight(rowSel, inner, selBg, selFg);
                    System.Console.Write(AnsiConsole.PadToDisplayWidth(line, width).PadRight(width));
                }
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }

        bool HandleKey(ConsoleKeyInfo key) => _keyHandler.Handle(key, context);

        ModalSystem.RunOverlayModal(
            overlayRowCount,
            width,
            DrawContent,
            HandleKey,
            consoleLock,
            onClose: () =>
            {
                if (context.ConfirmSelection)
                {
                    TextLayerType chosen = pickable[context.SelectedIndex];
                    renderer.ApplyLayerTypeToActiveSortedSlot(chosen);
                    persistAndRedraw();
                }

                if (restoreOverlayRowCountWhenClosed is int parentRows && parentRows > 0)
                {
                    _navigation.View = restoreTitleBarViewWhenClosed ?? TitleBarViewKind.PresetSettingsModal;
                    _orchestrator.SetOverlayActive(true, parentRows);
                }
                else
                {
                    _navigation.View = TitleBarViewKind.Main;
                    _orchestrator.SetOverlayActive(false);
                }

                setModalOpen(false);
            },
            onEnter: () =>
            {
                setModalOpen(true);
                _navigation.View = TitleBarViewKind.LayerPickerModal;
                _orchestrator.SetOverlayActive(true, overlayRowCount);
            },
            onIdleVisualizationTick: _orchestrator.Redraw);
    }
}
