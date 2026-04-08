using System.Diagnostics;
using System.IO.Abstractions;
using System.Text;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Palette;
using AudioAnalyzer.Application.Viewports;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>
/// Layered text visualizer: composites multiple independent layers (e.g. ScrollingColors, Marquee)
/// with configurable text snippets and beat-reactive behavior. Uses a cell buffer for z-order compositing.
/// </summary>
public sealed class TextLayersVisualizer : IVisualizer
{
    public bool SupportsPaletteCycling => true;

    private readonly TextLayersVisualizerSettings? _settings;
    private readonly IPaletteRepository _paletteRepo;
    private readonly IConsoleWriter _consoleWriter;
    private readonly UiSettings _uiSettings;
    private readonly Dictionary<TextLayerType, TextLayerRendererBase> _renderers;
    private readonly IKeyHandler<TextLayersKeyContext> _keyHandler;
    private readonly ITextLayersToolbarBuilder _toolbarBuilder;
    private readonly ITextLayerStateStore _stateStore;
    private readonly ITextLayerBoundsEditSession? _boundsEditSession;
    private readonly VisualizerSettings _visualizerSettings;
    private readonly IFileSystem _fileSystem;
    private readonly IShowPlayToolbarInfo? _showPlayToolbarInfo;
    /// <summary>Index of the layer whose palette P cycles. Updated when user presses 1–<see cref="TextLayersLimits.MaxLayerCount"/>.</summary>
    private int _paletteCycleLayerIndex;

    public TextLayersVisualizer(
        TextLayersVisualizerSettings? settings,
        IPaletteRepository paletteRepo,
        IEnumerable<TextLayerRendererBase> renderers,
        IConsoleWriter consoleWriter,
        IKeyHandler<TextLayersKeyContext> keyHandler,
        ITextLayersToolbarBuilder toolbarBuilder,
        ITextLayerStateStore stateStore,
        VisualizerSettings visualizerSettings,
        IFileSystem fileSystem,
        IShowPlayToolbarInfo? showPlayToolbarInfo = null,
        UiSettings? uiSettings = null,
        ITextLayerBoundsEditSession? boundsEditSession = null)
    {
        _settings = settings;
        _paletteRepo = paletteRepo;
        _consoleWriter = consoleWriter;
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _uiSettings = uiSettings ?? new UiSettings();
        _renderers = renderers.ToDictionary(r => r.LayerType);
        _keyHandler = keyHandler ?? throw new ArgumentNullException(nameof(keyHandler));
        _toolbarBuilder = toolbarBuilder ?? throw new ArgumentNullException(nameof(toolbarBuilder));
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _visualizerSettings = visualizerSettings ?? throw new ArgumentNullException(nameof(visualizerSettings));
        _showPlayToolbarInfo = showPlayToolbarInfo;
        _boundsEditSession = boundsEditSession;
    }

    private readonly ViewportCellBuffer _buffer = new();
    /// <summary>Per-layer state: (offset for scroll/marquee/wave, snippet index). Index matches sorted layer list.</summary>
    private readonly List<(double Offset, int SnippetIndex)> _layerStates = new();
    private readonly double?[] _layerRenderTimeScratch = new double?[TextLayersLimits.MaxLayerCount];
    private int _lastBeatCount = -1;

    public void Render(VisualizationFrameContext frame, VisualizerViewport viewport)
    {
        var sortedLayers = TryGetSortedLayersSnapshot(_settings);
        if (sortedLayers is not { Count: > 0 })
        {
            frame.LayerRenderTimeMs = null;
            RenderEmpty(viewport);
            return;
        }

        int w = viewport.Width;
        int h = viewport.MaxLines;
        if (w < 10 || h < 3)
        {
            frame.LayerRenderTimeMs = null;
            return;
        }

        _boundsEditSession?.SetLastViewport(w, h);

        var config = _settings;
        var defaultColors = ResolvePaletteForLayer(sortedLayers.Count > 0 ? sortedLayers[0] : null, config);
        var defaultColor = defaultColors[0];
        _buffer.EnsureSize(w, h);
        _buffer.Clear(defaultColor);
        _buffer.ClearClipStack();
        while (_layerStates.Count < sortedLayers.Count)
        {
            _layerStates.Add((0, 0));
        }

        while (_layerStates.Count > sortedLayers.Count)
        {
            _layerStates.RemoveAt(_layerStates.Count - 1);
        }

        _stateStore.EnsureCapacity(sortedLayers.Count);

        double frameDelta = frame.FrameDeltaSeconds > 0 ? frame.FrameDeltaSeconds : 1.0 / 60.0;

        if (frame.Analysis.BeatCount != _lastBeatCount)
        {
            _lastBeatCount = frame.Analysis.BeatCount;
        }

        double speedBurst = frame.Analysis.BeatFlashActive ? 2.0 : 1.0;

        if (_uiSettings.ShowLayerRenderTime)
        {
            Array.Clear(_layerRenderTimeScratch, 0, TextLayersLimits.MaxLayerCount);
            frame.LayerRenderTimeMs = _layerRenderTimeScratch;
        }
        else
        {
            frame.LayerRenderTimeMs = null;
        }

        for (int i = 0; i < sortedLayers.Count; i++)
        {
            var layer = sortedLayers[i];
            if (!layer.Enabled || !_renderers.TryGetValue(layer.LayerType, out var renderer))
            {
                continue;
            }

            var state = _layerStates[i];
            var layerColors = ResolvePaletteForLayer(layer, config);
            var (clipL, clipT, clipW, clipH) = TextLayerRenderBounds.ToPixelRect(layer.RenderBounds, w, h);
            _buffer.PushClip(clipL, clipT, clipW, clipH);
            try
            {
                bool hasBounds = layer.RenderBounds != null;
                var ctx = new TextLayerDrawContext
                {
                    Buffer = _buffer,
                    Frame = frame,
                    Palette = layerColors,
                    SpeedBurst = speedBurst,
                    ViewportWidth = w,
                    ViewportHeight = h,
                    Width = hasBounds ? clipW : w,
                    Height = hasBounds ? clipH : h,
                    BufferOriginX = hasBounds ? clipL : 0,
                    BufferOriginY = hasBounds ? clipT : 0,
                    LayerIndex = i,
                    FrameDeltaSeconds = frameDelta
                };
                if (_uiSettings.ShowLayerRenderTime)
                {
                    long t0 = Stopwatch.GetTimestamp();
                    state = renderer.Draw(layer, ref state, ctx);
                    long t1 = Stopwatch.GetTimestamp();
                    _layerRenderTimeScratch[i] = (t1 - t0) * 1000.0 / Stopwatch.Frequency;
                }
                else
                {
                    state = renderer.Draw(layer, ref state, ctx);
                }
            }
            finally
            {
                _buffer.PopClip();
            }

            _layerStates[i] = state;
        }

        if (_boundsEditSession?.IsActive == true
            && _boundsEditSession.EditingSortedLayerIndex is int editIdx
            && editIdx >= 0
            && editIdx < sortedLayers.Count)
        {
            var editLayer = sortedLayers[editIdx];
            var (bl, bt, bw, bh) = TextLayerRenderBounds.ToPixelRect(editLayer.RenderBounds, w, h);
            var hl = ResolvePaletteForLayer(editLayer, config);
            var borderColor = hl.Count > 1 ? hl[1] : hl[0];
            DrawRenderBoundsOverlay(_buffer, bl, bt, bw, bh, borderColor);
        }

        _buffer.FlushTo(_consoleWriter, viewport.StartRow);
    }

    private static void DrawRenderBoundsOverlay(ViewportCellBuffer buffer, int left, int top, int width, int height, PaletteColor color)
    {
        if (width < 1 || height < 1)
        {
            return;
        }

        int right = left + width - 1;
        int bottom = top + height - 1;

        if (width == 1 && height == 1)
        {
            buffer.Set(left, top, '+', color);
            return;
        }

        if (height == 1)
        {
            for (int x = left; x <= right; x++)
            {
                buffer.Set(x, top, '─', color);
            }

            return;
        }

        if (width == 1)
        {
            for (int y = top; y <= bottom; y++)
            {
                buffer.Set(left, y, '│', color);
            }

            return;
        }

        buffer.Set(left, top, '┌', color);
        buffer.Set(right, top, '┐', color);
        buffer.Set(left, bottom, '└', color);
        buffer.Set(right, bottom, '┘', color);
        for (int x = left + 1; x < right; x++)
        {
            buffer.Set(x, top, '─', color);
            buffer.Set(x, bottom, '─', color);
        }

        for (int y = top + 1; y < bottom; y++)
        {
            buffer.Set(left, y, '│', color);
            buffer.Set(right, y, '│', color);
        }
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

    public string? GetToolbarSuffix(VisualizationFrameContext frame)
    {
        var context = CreateToolbarContext(frame);
        return _toolbarBuilder.BuildSuffix(context);
    }

    /// <inheritdoc />
    public IReadOnlyList<LabeledValueDescriptor>? GetToolbarViewports(VisualizationFrameContext frame)
    {
        var context = CreateToolbarContext(frame);
        var descriptors = _toolbarBuilder.BuildViewports(context);
        return descriptors.Count > 0 ? descriptors : null;
    }

    private TextLayersToolbarContext CreateToolbarContext(VisualizationFrameContext frame)
    {
        var sortedLayers = TryGetSortedLayersSnapshot(_settings);
        var list = sortedLayers ?? [];
        int idx = list.Count > 0 ? Math.Clamp(_paletteCycleLayerIndex, 0, list.Count - 1) : 0;
        var layer = list.Count > 0 ? list[idx] : null;
        int entryCount = 0;
        int entryIndex = 0;
        if (_visualizerSettings.ApplicationMode == ApplicationMode.ShowPlay && _showPlayToolbarInfo != null)
        {
            entryCount = _showPlayToolbarInfo.GetActiveShowEntryCount();
            entryIndex = _showPlayToolbarInfo.CurrentEntryIndex;
        }

        int snippetIndex = list.Count > 0 && idx < _layerStates.Count ? _layerStates[idx].SnippetIndex : 0;
        IReadOnlyList<LayerToolbarContextualRow> contextualRows = layer != null
            ? LayerToolbarContextualRows.Resolve(layer, snippetIndex, _fileSystem, _uiSettings)
            : [];

        return new TextLayersToolbarContext
        {
            Frame = frame,
            SortedLayers = list,
            Settings = _settings,
            PaletteCycleLayerIndex = _paletteCycleLayerIndex,
            PaletteRepo = _paletteRepo,
            UiSettings = _uiSettings,
            ActiveLayerContextualRows = contextualRows,
            ApplicationMode = _visualizerSettings.ApplicationMode,
            ActiveShowName = _visualizerSettings.ActiveShowName,
            ShowEntryIndex = entryIndex,
            ShowEntryCount = entryCount
        };
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

    /// <inheritdoc />
    public void OnTextLayersStructureChanged()
    {
        var sortedLayers = TryGetSortedLayersSnapshot(_settings);
        int n = sortedLayers?.Count ?? 0;
        _paletteCycleLayerIndex = n == 0 ? 0 : Math.Clamp(_paletteCycleLayerIndex, 0, n - 1);
        while (_layerStates.Count > n)
        {
            _layerStates.RemoveAt(_layerStates.Count - 1);
        }
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
    /// Handles keys 1–<see cref="TextLayersLimits.MaxLayerCount"/> to select the active layer; Left/Right to cycle the active layer's type;
    /// Shift+1–<see cref="TextLayersLimits.MaxLayerCount"/> to toggle layer enabled/disabled; I to cycle to the next asset in AsciiImage / AsciiModel layers.
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
            UiSettings = _uiSettings,
            FileSystem = _fileSystem,
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
        _ = previousType;
        _stateStore.ClearState(layerIndex);
    }
}
