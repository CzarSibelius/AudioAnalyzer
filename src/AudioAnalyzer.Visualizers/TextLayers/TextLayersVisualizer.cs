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
    private static readonly Dictionary<TextLayerType, ITextLayerRenderer> s_renderers = CreateRenderers();

    public TextLayersVisualizer(TextLayersVisualizerSettings? settings)
    {
        _settings = settings;
    }

    private readonly ViewportCellBuffer _buffer = new();
    /// <summary>Per-layer state: (offset for scroll/marquee/wave, snippet index). Index matches sorted layer list.</summary>
    private readonly List<(double Offset, int SnippetIndex)> _layerStates = new();
    /// <summary>Falling letter particles per layer index (only for FallingLetters layers).</summary>
    private readonly List<List<FallingLetterState>> _fallingLettersByLayer = new();
    private int _lastBeatCount = -1;
    private int _beatFlashFrames;

    private static Dictionary<TextLayerType, ITextLayerRenderer> CreateRenderers()
    {
        var list = new ITextLayerRenderer[]
        {
            new ScrollingColorsLayer(),
            new MarqueeLayer(),
            new FallingLettersLayer(),
            new WaveTextLayer(),
            new StaticTextLayer(),
            new MatrixRainLayer()
        };
        return list.ToDictionary(r => r.LayerType);
    }

    public void Render(AnalysisSnapshot snapshot, VisualizerViewport viewport)
    {
        var config = _settings;
        if (config?.Layers is not { Count: > 0 })
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

        var palette = snapshot.Palette;
        var colors = palette is { Count: > 0 }
            ? palette
            : (IReadOnlyList<PaletteColor>)GetDefaultPalette();

        var defaultColor = colors[0];
        _buffer.EnsureSize(w, h);
        _buffer.Clear(defaultColor);

        var sortedLayers = config.Layers.OrderBy(l => l.ZOrder).ToList();
        while (_layerStates.Count < sortedLayers.Count)
        {
            _layerStates.Add((0, 0));
        }
        while (_fallingLettersByLayer.Count < sortedLayers.Count)
        {
            _fallingLettersByLayer.Add(new List<FallingLetterState>());
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
            if (!s_renderers.TryGetValue(layer.LayerType, out var renderer))
            {
                continue;
            }

            var state = _layerStates[i];
            var ctx = new TextLayerDrawContext
            {
                Buffer = _buffer,
                Snapshot = snapshot,
                Palette = colors,
                SpeedBurst = speedBurst,
                Width = w,
                Height = h,
                LayerIndex = i,
                FallingLettersForLayer = _fallingLettersByLayer[i]
            };
            state = renderer.Draw(layer, ref state, ctx);
            _layerStates[i] = state;
        }

        _buffer.WriteToConsole(viewport.StartRow);
    }

    private void RenderEmpty(VisualizerViewport viewport)
    {
        _buffer.EnsureSize(viewport.Width, viewport.MaxLines);
        _buffer.Clear(PaletteColor.FromConsoleColor(ConsoleColor.DarkGray));
        _buffer.WriteToConsole(viewport.StartRow);
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
        var config = _settings;
        if (config?.Layers is not { Count: > 0 })
        {
            return "Layers: (config in settings)";
        }
        return $"Layers: {config.Layers.Count} (1–9: cycle layer type)";
    }

    /// <summary>
    /// Handles keys 1–9 to cycle the layer type for the corresponding layer slot.
    /// 1 = layer 1 (back), 9 = layer 9 (front). Returns true if the key was handled.
    /// </summary>
    public bool HandleKey(ConsoleKey key)
    {
        var config = _settings;
        int digit = key switch
        {
            ConsoleKey.D1 or ConsoleKey.NumPad1 => 1,
            ConsoleKey.D2 or ConsoleKey.NumPad2 => 2,
            ConsoleKey.D3 or ConsoleKey.NumPad3 => 3,
            ConsoleKey.D4 or ConsoleKey.NumPad4 => 4,
            ConsoleKey.D5 or ConsoleKey.NumPad5 => 5,
            ConsoleKey.D6 or ConsoleKey.NumPad6 => 6,
            ConsoleKey.D7 or ConsoleKey.NumPad7 => 7,
            ConsoleKey.D8 or ConsoleKey.NumPad8 => 8,
            ConsoleKey.D9 or ConsoleKey.NumPad9 => 9,
            _ => 0
        };
        if (digit == 0 || config?.Layers is not { Count: > 0 })
        {
            return false;
        }

        var sortedLayers = config.Layers.OrderBy(l => l.ZOrder).ToList();
        int layerIndex = digit - 1; // Key 1 -> index 0, Key 9 -> index 8
        if (layerIndex >= sortedLayers.Count)
        {
            return false;
        }

        var layer = sortedLayers[layerIndex];
        var types = Enum.GetValues<TextLayerType>();
        int currentIndex = Array.IndexOf(types, layer.LayerType);
        int nextIndex = (currentIndex + 1) % types.Length;
        var previousType = layer.LayerType;
        layer.LayerType = types[nextIndex];

        // Clear per-layer state when switching away from FallingLetters to avoid artifacts
        if (previousType == TextLayerType.FallingLetters && layerIndex < _fallingLettersByLayer.Count)
        {
            _fallingLettersByLayer[layerIndex].Clear();
        }

        return true;
    }
}
