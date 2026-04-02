using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;

namespace AudioAnalyzer.Console;

/// <summary>TextLayers settings overlay modal (S key). Layer list, settings panel, preset rename and create per ADR-0023.</summary>
internal sealed class SettingsModal : ISettingsModal
{
    private const int OverlayRowCount = 16;

    private readonly IVisualizationOrchestrator _orchestrator;
    private readonly VisualizerSettings _visualizerSettings;
    private readonly IPresetRepository _presetRepository;
    private readonly ISettingsModalRenderer _renderer;
    private readonly IKeyHandler<SettingsModalKeyContext> _keyHandler;
    private readonly IConsoleDimensions _consoleDimensions;
    private readonly ITextLayerBoundsEditSession _boundsEditSession;
    private readonly ITitleBarNavigationContext _navigation;
    private readonly IVisualizer _visualizer;
    private readonly IDefaultTextLayersSettingsFactory _defaultTextLayersFactory;
    private readonly ITextLayerStateStore _layerStateStore;

    public SettingsModal(
        IVisualizationOrchestrator orchestrator,
        VisualizerSettings visualizerSettings,
        IPresetRepository presetRepository,
        ISettingsModalRenderer renderer,
        IKeyHandler<SettingsModalKeyContext> keyHandler,
        IConsoleDimensions consoleDimensions,
        ITextLayerBoundsEditSession boundsEditSession,
        ITitleBarNavigationContext navigation,
        IVisualizer visualizer,
        IDefaultTextLayersSettingsFactory defaultTextLayersFactory,
        ITextLayerStateStore layerStateStore)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _visualizerSettings = visualizerSettings ?? throw new ArgumentNullException(nameof(visualizerSettings));
        _presetRepository = presetRepository ?? throw new ArgumentNullException(nameof(presetRepository));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _keyHandler = keyHandler ?? throw new ArgumentNullException(nameof(keyHandler));
        _consoleDimensions = consoleDimensions ?? throw new ArgumentNullException(nameof(consoleDimensions));
        _boundsEditSession = boundsEditSession ?? throw new ArgumentNullException(nameof(boundsEditSession));
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        _visualizer = visualizer ?? throw new ArgumentNullException(nameof(visualizer));
        _defaultTextLayersFactory = defaultTextLayersFactory ?? throw new ArgumentNullException(nameof(defaultTextLayersFactory));
        _layerStateStore = layerStateStore ?? throw new ArgumentNullException(nameof(layerStateStore));
    }

    /// <inheritdoc />
    public void Show(object consoleLock, Action saveSettings)
    {
        var state = new SettingsModalState();
        var textLayers = _visualizerSettings.TextLayers ?? new TextLayersVisualizerSettings();
        var layers = textLayers.Layers ?? new List<TextLayerSettings>();
        var sortedLayers = layers.OrderBy(l => l.ZOrder).ToList();
        if (sortedLayers.Count == 0)
        {
            sortedLayers = new List<TextLayerSettings>();
        }

        int activeSorted = _visualizer.GetActiveLayerZIndex();
        if (activeSorted >= 0 && sortedLayers.Count > 0)
        {
            state.SelectedLayerIndex = Math.Clamp(activeSorted, 0, sortedLayers.Count - 1);
        }

        SettingsModalKeyContext context = null!;
        context = new SettingsModalKeyContext
        {
            State = state,
            SortedLayers = sortedLayers,
            TextLayers = textLayers,
            VisualizerSettings = _visualizerSettings,
            PresetRepository = _presetRepository,
            SaveSettings = saveSettings,
            DefaultTextLayersFactory = _defaultTextLayersFactory,
            LayerStateStore = _layerStateStore,
            NotifyLayersStructureChanged = () =>
            {
                textLayers.Layers ??= new List<TextLayerSettings>();
                context.SortedLayers = textLayers.Layers.OrderBy(l => l.ZOrder).ToList();
                _visualizer.OnTextLayersStructureChanged();
                saveSettings();
            },
            RequestVisualBoundsEdit = idx => _boundsEditSession.BeginEdit(idx, textLayers)
        };

        void DrawSettingsContent()
        {
            int width = _consoleDimensions.GetConsoleWidth();
            _renderer.Draw(state, context.SortedLayers, width, _orchestrator.GetSnapshotForUi());
        }

        bool HandleSettingsKey(ConsoleKeyInfo key)
        {
            return _keyHandler.Handle(key, context);
        }

        void OnScrollTick()
        {
            int width = _consoleDimensions.GetConsoleWidth();
            _renderer.DrawIdleOverlayTick(state, context.SortedLayers, width, _orchestrator.GetSnapshotForUi());
        }

        ModalSystem.RunOverlayModal(
            OverlayRowCount,
            _consoleDimensions.GetConsoleWidth(),
            DrawSettingsContent,
            HandleSettingsKey,
            consoleLock,
            onClose: () =>
            {
                saveSettings();
                _navigation.View = TitleBarViewKind.Main;
                _navigation.PresetSettingsPalettePickerActive = false;
                _navigation.PresetSettingsLayerOneBased = null;
                _navigation.PresetSettingsLayerTypeRaw = null;
                _navigation.PresetSettingsFocusedSettingId = null;
                _orchestrator.SetOverlayActive(false);
            },
            onEnter: () =>
            {
                _navigation.View = TitleBarViewKind.PresetSettingsModal;
                _orchestrator.SetOverlayActive(true, OverlayRowCount);
            },
            onScrollTick: OnScrollTick);
    }
}
