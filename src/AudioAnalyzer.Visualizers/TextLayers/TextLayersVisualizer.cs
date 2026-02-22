using System.Text;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>
/// Layered text visualizer: composites multiple independent layers (e.g. ScrollingColors, Marquee)
/// with configurable text snippets and beat-reactive behavior. Uses a cell buffer for z-order compositing.
/// </summary>
public sealed class TextLayersVisualizer : IVisualizer
{
    public string TechnicalName => "textlayers";
    public string DisplayName => "Layered text";
    public bool SupportsPaletteCycling => true;

    private readonly TextLayersVisualizerSettings? _settings;
    private readonly IPaletteRepository _paletteRepo;
    private readonly IConsoleWriter _consoleWriter;
    private readonly UiSettings _uiSettings;
    private readonly Dictionary<TextLayerType, ITextLayerRenderer> _renderers;
    private readonly IKeyHandler<TextLayersKeyContext> _keyHandler;
    private readonly ITextLayersToolbarBuilder _toolbarBuilder;
    /// <summary>Index of the layer whose palette P cycles. Updated when user presses 1–9.</summary>
    private int _paletteCycleLayerIndex;

    public TextLayersVisualizer(TextLayersVisualizerSettings? settings, IPaletteRepository paletteRepo, IEnumerable<ITextLayerRenderer> renderers, IConsoleWriter consoleWriter, IKeyHandler<TextLayersKeyContext> keyHandler, ITextLayersToolbarBuilder toolbarBuilder, UiSettings? uiSettings = null)
    {
        _settings = settings;
        _paletteRepo = paletteRepo;
        _consoleWriter = consoleWriter;
        _uiSettings = uiSettings ?? new UiSettings();
        _renderers = renderers.ToDictionary(r => r.LayerType);
        _keyHandler = keyHandler ?? throw new ArgumentNullException(nameof(keyHandler));
        _toolbarBuilder = toolbarBuilder ?? throw new ArgumentNullException(nameof(toolbarBuilder));
    }

    private readonly ViewportCellBuffer _buffer = new();
    /// <summary>Per-layer state: (offset for scroll/marquee/wave, snippet index). Index matches sorted layer list.</summary>
    private readonly List<(double Offset, int SnippetIndex)> _layerStates = new();
    /// <summary>Falling letter particles per layer index (only for FallingLetters layers).</summary>
    private readonly List<List<FallingLetterState>> _fallingLettersByLayer = new();
    /// <summary>ASCII image state per layer index (only for AsciiImage layers).</summary>
    private readonly List<AsciiImageState> _asciiImageStateByLayer = new();
    /// <summary>Geiss background state per layer index (only for GeissBackground layers).</summary>
    private readonly List<GeissBackgroundState> _geissBackgroundStateByLayer = new();
    /// <summary>Beat circles state per layer index (only for BeatCircles layers).</summary>
    private readonly List<BeatCirclesState> _beatCirclesStateByLayer = new();
    /// <summary>Unknown Pleasures state per layer index (only for UnknownPleasures layers).</summary>
    private readonly List<UnknownPleasuresState> _unknownPleasuresStateByLayer = new();
    private int _lastBeatCount = -1;
    private int _beatFlashFrames;

    public void Render(AnalysisSnapshot snapshot, VisualizerViewport viewport)
    {
        var sortedLayers = TryGetSortedLayersSnapshot(_settings);
        if (sortedLayers is not { Count: > 0 })
        {
            RenderEmpty(viewport);
            return;
        }

        int w = viewport.Width;
        int h = viewport.MaxLines;
        if (w < 10 || h < 3)
        {
            return;
        }

        var config = _settings;
        var defaultColors = ResolvePaletteForLayer(sortedLayers.Count > 0 ? sortedLayers[0] : null, config);
        var defaultColor = defaultColors[0];
        _buffer.EnsureSize(w, h);
        _buffer.Clear(defaultColor);
        while (_layerStates.Count < sortedLayers.Count)
        {
            _layerStates.Add((0, 0));
        }
        while (_fallingLettersByLayer.Count < sortedLayers.Count)
        {
            _fallingLettersByLayer.Add(new List<FallingLetterState>());
        }
        while (_asciiImageStateByLayer.Count < sortedLayers.Count)
        {
            _asciiImageStateByLayer.Add(new AsciiImageState());
        }
        while (_geissBackgroundStateByLayer.Count < sortedLayers.Count)
        {
            _geissBackgroundStateByLayer.Add(new GeissBackgroundState());
        }
        while (_beatCirclesStateByLayer.Count < sortedLayers.Count)
        {
            _beatCirclesStateByLayer.Add(new BeatCirclesState());
        }
        while (_unknownPleasuresStateByLayer.Count < sortedLayers.Count)
        {
            _unknownPleasuresStateByLayer.Add(new UnknownPleasuresState());
        }

        if (snapshot.BeatCount != _lastBeatCount)
        {
            _lastBeatCount = snapshot.BeatCount;
            _beatFlashFrames = 3;
        }

        if (_beatFlashFrames > 0)
        {
            _beatFlashFrames--;
        }

        double speedBurst = snapshot.BeatFlashActive ? 2.0 : 1.0;

        for (int i = 0; i < sortedLayers.Count; i++)
        {
            var layer = sortedLayers[i];
            if (!layer.Enabled || !_renderers.TryGetValue(layer.LayerType, out var renderer))
            {
                continue;
            }

            var state = _layerStates[i];
            var layerColors = ResolvePaletteForLayer(layer, config);
            var ctx = new TextLayerDrawContext
            {
                Buffer = _buffer,
                Snapshot = snapshot,
                Palette = layerColors,
                SpeedBurst = speedBurst,
                Width = w,
                Height = h,
                LayerIndex = i,
                FallingLettersForLayer = _fallingLettersByLayer[i],
                AsciiImageStateForLayer = _asciiImageStateByLayer[i],
                GeissBackgroundStateForLayer = _geissBackgroundStateByLayer[i],
                BeatCirclesStateForLayer = _beatCirclesStateByLayer[i],
                UnknownPleasuresStateForLayer = _unknownPleasuresStateByLayer[i]
            };
            state = renderer.Draw(layer, ref state, ctx);
            _layerStates[i] = state;
        }

        _buffer.FlushTo(_consoleWriter, viewport.StartRow);
    }

    private void RenderEmpty(VisualizerViewport viewport)
    {
        _buffer.EnsureSize(viewport.Width, viewport.MaxLines);
        _buffer.Clear(PaletteColor.FromConsoleColor(ConsoleColor.DarkGray));
        _buffer.FlushTo(_consoleWriter, viewport.StartRow);
    }

    private IReadOnlyList<PaletteColor> ResolvePaletteForLayer(TextLayerSettings? layer, TextLayersVisualizerSettings? config)
    {
        var paletteId = layer?.PaletteId ?? config?.PaletteId ?? "default";
        if (string.IsNullOrWhiteSpace(paletteId))
        {
            return GetDefaultPalette();
        }
        var def = _paletteRepo.GetById(paletteId);
        if (def != null && ColorPaletteParser.Parse(def) is { } palette && palette.Count > 0)
        {
            return palette;
        }
        return GetDefaultPalette();
    }

    private static IReadOnlyList<PaletteColor> GetDefaultPalette()
    {
        return
        [
            PaletteColor.FromConsoleColor(ConsoleColor.DarkBlue),
            PaletteColor.FromConsoleColor(ConsoleColor.DarkMagenta),
            PaletteColor.FromConsoleColor(ConsoleColor.DarkCyan),
            PaletteColor.FromConsoleColor(ConsoleColor.Blue),
            PaletteColor.FromConsoleColor(ConsoleColor.Magenta),
            PaletteColor.FromConsoleColor(ConsoleColor.Cyan)
        ];
    }

    public string? GetToolbarSuffix(AnalysisSnapshot snapshot)
    {
        var sortedLayers = TryGetSortedLayersSnapshot(_settings);
        var context = new TextLayersToolbarContext
        {
            Snapshot = snapshot,
            SortedLayers = sortedLayers ?? [],
            Settings = _settings,
            PaletteCycleLayerIndex = _paletteCycleLayerIndex,
            PaletteRepo = _paletteRepo,
            UiSettings = _uiSettings
        };
        return _toolbarBuilder.BuildSuffix(context);
    }

    /// <inheritdoc />
    public string? GetActiveLayerDisplayName()
    {
        var sortedLayers = TryGetSortedLayersSnapshot(_settings);
        if (sortedLayers is not { Count: > 0 })
        {
            return null;
        }
        int idx = Math.Clamp(_paletteCycleLayerIndex, 0, sortedLayers.Count - 1);
        var layer = sortedLayers[idx];
        return ToSnakeCase(layer.LayerType.ToString());
    }

    /// <inheritdoc />
    public int GetActiveLayerZIndex()
    {
        var sortedLayers = TryGetSortedLayersSnapshot(_settings);
        if (sortedLayers is not { Count: > 0 })
        {
            return -1;
        }
        return Math.Clamp(_paletteCycleLayerIndex, 0, sortedLayers.Count - 1);
    }

    /// <summary>Gets a snapshot of layers sorted by ZOrder, or null if config is empty or the collection was modified during copy (e.g. during show-mode switch or shutdown).</summary>
    private static List<TextLayerSettings>? TryGetSortedLayersSnapshot(TextLayersVisualizerSettings? config)
    {
        if (config?.Layers is not { Count: > 0 })
        {
            return null;
        }
        try
        {
            var snapshot = new List<TextLayerSettings>(config.Layers);
            return snapshot.OrderBy(l => l.ZOrder).ToList();
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static string ToSnakeCase(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }
        var sb = new StringBuilder();
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (char.IsUpper(c) && i > 0)
            {
                sb.Append('_');
            }
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }

    /// <summary>
    /// Handles keys 1–9 to select the active layer; Left/Right to cycle the active layer's type;
    /// Shift+1–9 to toggle layer enabled/disabled; I to cycle to the next picture in AsciiImage layers.
    /// 1 = layer 1 (back), 9 = layer 9 (front). Returns true if the key was handled.
    /// </summary>
    public bool HandleKey(ConsoleKeyInfo key)
    {
        var sortedLayers = TryGetSortedLayersSnapshot(_settings);
        if (sortedLayers is not { Count: > 0 })
        {
            return false;
        }

        var context = new TextLayersKeyContext
        {
            SortedLayers = sortedLayers,
            Settings = _settings,
            PaletteCycleLayerIndex = _paletteCycleLayerIndex,
            PaletteRepo = _paletteRepo,
            AdvanceSnippetIndex = AdvanceSnippetIndexAt,
            ClearLayerState = ClearLayerStateWhenSwitching
        };
        bool handled = _keyHandler.Handle(key, context);
        if (handled)
        {
            _paletteCycleLayerIndex = context.PaletteCycleLayerIndex;
        }
        return handled;
    }

    private void AdvanceSnippetIndexAt(int layerIndex)
    {
        if (layerIndex >= 0 && layerIndex < _layerStates.Count)
        {
            var state = _layerStates[layerIndex];
            state.SnippetIndex++;
            _layerStates[layerIndex] = state;
        }
    }

    private void ClearLayerStateWhenSwitching(int layerIndex, TextLayerType previousType)
    {
        if (previousType == TextLayerType.FallingLetters && layerIndex < _fallingLettersByLayer.Count)
        {
            _fallingLettersByLayer[layerIndex].Clear();
        }
        if (previousType == TextLayerType.AsciiImage && layerIndex < _asciiImageStateByLayer.Count)
        {
            _asciiImageStateByLayer[layerIndex] = new AsciiImageState();
        }
        if (previousType == TextLayerType.GeissBackground && layerIndex < _geissBackgroundStateByLayer.Count)
        {
            _geissBackgroundStateByLayer[layerIndex] = new GeissBackgroundState();
        }
        if (previousType == TextLayerType.BeatCircles && layerIndex < _beatCirclesStateByLayer.Count)
        {
            _beatCirclesStateByLayer[layerIndex] = new BeatCirclesState();
        }
        if (previousType == TextLayerType.UnknownPleasures && layerIndex < _unknownPleasuresStateByLayer.Count)
        {
            _unknownPleasuresStateByLayer[layerIndex] = new UnknownPleasuresState();
        }
    }
}
