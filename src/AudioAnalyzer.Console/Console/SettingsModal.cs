using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;

namespace AudioAnalyzer.Console;

/// <summary>TextLayers settings overlay modal (S key). Layer list, settings panel, preset rename and create per ADR-0023.</summary>
internal sealed class SettingsModal : ISettingsModal
{
    private const int OverlayRowCount = 18;

    private readonly AnalysisEngine _analysisEngine;
    private readonly VisualizerSettings _visualizerSettings;
    private readonly IPresetRepository _presetRepository;
    private readonly ISettingsModalRenderer _renderer;
    private readonly IKeyHandler<SettingsModalKeyContext> _keyHandler;

    public SettingsModal(
        AnalysisEngine analysisEngine,
        VisualizerSettings visualizerSettings,
        IPresetRepository presetRepository,
        ISettingsModalRenderer renderer,
        IKeyHandler<SettingsModalKeyContext> keyHandler)
    {
        _analysisEngine = analysisEngine ?? throw new ArgumentNullException(nameof(analysisEngine));
        _visualizerSettings = visualizerSettings ?? throw new ArgumentNullException(nameof(visualizerSettings));
        _presetRepository = presetRepository ?? throw new ArgumentNullException(nameof(presetRepository));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _keyHandler = keyHandler ?? throw new ArgumentNullException(nameof(keyHandler));
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

        var context = new SettingsModalKeyContext
        {
            State = state,
            SortedLayers = sortedLayers,
            TextLayers = textLayers,
            VisualizerSettings = _visualizerSettings,
            PresetRepository = _presetRepository,
            SaveSettings = saveSettings
        };

        void DrawSettingsContent()
        {
            int width = ConsoleHeader.GetConsoleWidth();
            _renderer.Draw(state, context.SortedLayers, width);
        }

        void DrawHintLineOnly()
        {
            int width = ConsoleHeader.GetConsoleWidth();
            _renderer.DrawHintLine(state, width);
        }

        bool HandleSettingsKey(ConsoleKeyInfo key)
        {
            return _keyHandler.Handle(key, context);
        }

        ModalSystem.RunOverlayModal(
            OverlayRowCount,
            DrawSettingsContent,
            HandleSettingsKey,
            consoleLock,
            onClose: () =>
            {
                saveSettings();
                _analysisEngine.SetOverlayActive(false);
            },
            onEnter: () => _analysisEngine.SetOverlayActive(true, OverlayRowCount),
            onScrollTick: DrawHintLineOnly);
    }
}
