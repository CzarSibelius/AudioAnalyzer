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
    /// <summary>Index of the layer whose palette P cycles. Updated when user presses 1–9.</summary>
    private int _paletteCycleLayerIndex;

    public TextLayersVisualizer(TextLayersVisualizerSettings? settings, IPaletteRepository paletteRepo, IEnumerable<ITextLayerRenderer> renderers, IConsoleWriter consoleWriter, UiSettings? uiSettings = null)
    {
        _settings = settings;
        _paletteRepo = paletteRepo;
        _consoleWriter = consoleWriter;
        _uiSettings = uiSettings ?? new UiSettings();
        _renderers = renderers.ToDictionary(r => r.LayerType);
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

        var sortedLayers = config.Layers.OrderBy(l => l.ZOrder).ToList();
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
        var config = _settings;
        if (config?.Layers is not { Count: > 0 })
        {
            var emptyPalette = _uiSettings.Palette ?? new UiPalette();
            return AnsiConsole.ColorCode(emptyPalette.Label) + "Layers:" + AnsiConsole.ResetCode + AnsiConsole.ColorCode(emptyPalette.Dimmed) + "(config in settings, S: settings)" + AnsiConsole.ResetCode;
        }
        var sortedLayers = config.Layers.OrderBy(l => l.ZOrder).ToList();
        int idx = Math.Clamp(_paletteCycleLayerIndex, 0, sortedLayers.Count - 1);
        var layer = sortedLayers[idx];
        var paletteId = layer.PaletteId ?? config.PaletteId ?? "default";
        var paletteDef = _paletteRepo.GetById(paletteId);
        var paletteName = paletteDef?.Name?.Trim() ?? paletteId;
        if (string.IsNullOrWhiteSpace(paletteName))
        {
            paletteName = "Default";
        }

        var palette = _uiSettings.Palette ?? new UiPalette();
        var sb = new StringBuilder();
        AnsiConsole.AppendColored(sb, "Layers:", palette.Label);
        for (int i = 0; i < 9; i++)
        {
            char digit = (char)('1' + i);
            if (i >= sortedLayers.Count)
            {
                AnsiConsole.AppendColored(sb, digit, palette.Dimmed);
            }
            else
            {
                var l = sortedLayers[i];
                if (!l.Enabled)
                {
                    AnsiConsole.AppendColored(sb, digit, palette.Dimmed);
                }
                else if (i == idx)
                {
                    AnsiConsole.AppendColored(sb, digit, palette.Highlighted);
                }
                else
                {
                    AnsiConsole.AppendColored(sb, digit, palette.Normal);
                }
            }
        }
        sb.Append(" (1-9 select, \u2190\u2192 type, Shift+1-9 toggle");
        var hasAscii = config.Layers.Any(l => l.LayerType == TextLayerType.AsciiImage);
        if (hasAscii)
        {
            sb.Append(", I: next image");
        }
        sb.Append(", S: settings)");
        if (layer.LayerType == TextLayerType.Oscilloscope)
        {
            var osc = layer.GetCustom<OscilloscopeSettings>() ?? new OscilloscopeSettings();
            sb.Append(" | Gain:");
            sb.Append(osc.Gain.ToString("F1", System.Globalization.CultureInfo.InvariantCulture));
            sb.Append(" ([ ])");
        }
        sb.Append(" | Palette(L");
        sb.Append(idx + 1);
        sb.Append("):");
        sb.Append(paletteName);
        sb.Append(" (P)");
        return sb.ToString();
    }

    /// <inheritdoc />
    public string? GetActiveLayerDisplayName()
    {
        var config = _settings;
        if (config?.Layers is not { Count: > 0 })
        {
            return null;
        }
        var sortedLayers = config.Layers.OrderBy(l => l.ZOrder).ToList();
        int idx = Math.Clamp(_paletteCycleLayerIndex, 0, sortedLayers.Count - 1);
        var layer = sortedLayers[idx];
        return ToSnakeCase(layer.LayerType.ToString());
    }

    /// <inheritdoc />
    public int GetActiveLayerZIndex()
    {
        var config = _settings;
        if (config?.Layers is not { Count: > 0 })
        {
            return -1;
        }
        return Math.Clamp(_paletteCycleLayerIndex, 0, int.MaxValue);
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
        var config = _settings;
        if (config?.Layers is not { Count: > 0 })
        {
            return false;
        }

        var sortedLayers = config.Layers.OrderBy(l => l.ZOrder).ToList();

        if (key.Key is ConsoleKey.P)
        {
            if (sortedLayers.Count == 0)
            {
                return false;
            }
            int idx = Math.Clamp(_paletteCycleLayerIndex, 0, sortedLayers.Count - 1);
            var paletteLayer = sortedLayers[idx];
            var currentId = paletteLayer.PaletteId ?? config.PaletteId ?? "";
            var all = _paletteRepo.GetAll();
            if (all.Count == 0)
            {
                return true;
            }
            int nextIndex = 0;
            for (int i = 0; i < all.Count; i++)
            {
                if (string.Equals(all[i].Id, currentId, StringComparison.OrdinalIgnoreCase))
                {
                    nextIndex = (i + 1) % all.Count;
                    break;
                }
            }
            var next = all[nextIndex];
            paletteLayer.PaletteId = next.Id;
            return true;
        }

        if (key.Key is ConsoleKey.Oem4 or ConsoleKey.Oem6)
        {
            int layerIndex = Math.Clamp(_paletteCycleLayerIndex, 0, sortedLayers.Count - 1);
            var layer = sortedLayers[layerIndex];
            if (layer.LayerType == TextLayerType.Oscilloscope)
            {
                var osc = layer.GetCustom<OscilloscopeSettings>() ?? new OscilloscopeSettings();
                double delta = key.Key is ConsoleKey.Oem6 ? 0.5 : -0.5;
                osc.Gain = Math.Clamp(osc.Gain + delta, 1.0, 10.0);
                layer.SetCustom(osc);
                return true;
            }
        }

        if (key.Key is ConsoleKey.I)
        {
            bool anyAdvanced = false;
            for (int i = 0; i < sortedLayers.Count && i < _layerStates.Count; i++)
            {
                if (sortedLayers[i].LayerType != TextLayerType.AsciiImage)
                {
                    continue;
                }
                var state = _layerStates[i];
                state.SnippetIndex++;
                _layerStates[i] = state;
                anyAdvanced = true;
            }
            return anyAdvanced;
        }

        if (key.Key is ConsoleKey.LeftArrow or ConsoleKey.RightArrow)
        {
            if (sortedLayers.Count == 0)
            {
                return false;
            }
            int layerIndex = Math.Clamp(_paletteCycleLayerIndex, 0, sortedLayers.Count - 1);
            var layer = sortedLayers[layerIndex];
            var previousType = layer.LayerType;
            layer.LayerType = key.Key is ConsoleKey.LeftArrow
                ? TextLayerSettings.CycleTypeBackward(layer)
                : TextLayerSettings.CycleTypeForward(layer);
            ClearLayerStateWhenSwitching(layerIndex, previousType);
            return true;
        }

        int digit = key.Key switch
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

        int layerIdx = digit - 1;
        if (layerIdx >= sortedLayers.Count)
        {
            return false;
        }

        _paletteCycleLayerIndex = layerIdx;

        if (key.Modifiers.HasFlag(ConsoleModifiers.Shift))
        {
            var l = sortedLayers[layerIdx];
            l.Enabled = !l.Enabled;
            return true;
        }

        return true;
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
