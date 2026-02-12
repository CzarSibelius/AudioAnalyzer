using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>State for one falling letter: column, vertical position, character.</summary>
internal struct FallingLetterState
{
    public int Col;
    public double Y;
    public char Char;
}

/// <summary>
/// Layered text visualizer: composites multiple independent layers (e.g. ScrollingColors, Marquee)
/// with configurable text snippets and beat-reactive behavior. Uses a cell buffer for z-order compositing.
/// </summary>
public sealed class TextLayersVisualizer : IVisualizer
{
    public string TechnicalName => "textlayers";
    public string DisplayName => "Layered text";
    public bool SupportsPaletteCycling => true;

    private readonly ViewportCellBuffer _buffer = new();
    /// <summary>Per-layer state: (offset for scroll/marquee/wave, snippet index). Index matches sorted layer list.</summary>
    private readonly List<(double Offset, int SnippetIndex)> _layerStates = new();
    /// <summary>Falling letter particles per layer index (only for FallingLetters layers).</summary>
    private readonly List<List<FallingLetterState>> _fallingLettersByLayer = new();
    private int _lastBeatCount = -1;
    private int _beatFlashFrames;

    public void Render(AnalysisSnapshot snapshot, VisualizerViewport viewport)
    {
        var config = snapshot.TextLayersConfig;
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
            var state = _layerStates[i];
            switch (layer.LayerType)
            {
                case TextLayerType.ScrollingColors:
                    state = DrawScrollingColors(layer, ref state, snapshot, colors, speedBurst);
                    break;
                case TextLayerType.Marquee:
                    state = DrawMarquee(layer, ref state, snapshot, colors, speedBurst, w, h);
                    break;
                case TextLayerType.FallingLetters:
                    state = DrawFallingLetters(layer, ref state, snapshot, colors, speedBurst, w, h, i);
                    break;
                case TextLayerType.WaveText:
                    state = DrawWaveText(layer, ref state, snapshot, colors, speedBurst, w, h);
                    break;
                case TextLayerType.StaticText:
                    state = DrawStaticText(layer, ref state, snapshot, colors, w, h);
                    break;
                case TextLayerType.MatrixRain:
                    state = DrawMatrixRain(layer, ref state, snapshot, colors, speedBurst, w, h);
                    break;
                default:
                    break;
            }
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

    private (double Offset, int SnippetIndex) DrawScrollingColors(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        AnalysisSnapshot snapshot,
        IReadOnlyList<PaletteColor> palette,
        double speedBurst)
    {
        int w = _buffer.Width;
        int h = _buffer.Height;
        double speed = layer.SpeedMultiplier * speedBurst * 0.5;
        if (layer.BeatReaction == TextLayerBeatReaction.SpeedBurst && snapshot.BeatFlashActive)
        {
            speed *= 2.0;
        }

        state.Offset += speed;
        double offset = state.Offset;
        int colorOffset = layer.BeatReaction == TextLayerBeatReaction.ColorPop && snapshot.BeatFlashActive
            ? 1
            : 0;
        int paletteCount = palette.Count;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                double t = (x + y * 0.5 + offset) * 0.1;
                int idx = (layer.ColorIndex + colorOffset + (int)Math.Floor(t)) % paletteCount;
                if (idx < 0)
                {
                    idx = (idx % paletteCount + paletteCount) % paletteCount;
                }
                var color = palette[idx % paletteCount];
                _buffer.Set(x, y, '░', color);
            }
        }

        return state;
    }

    private (double Offset, int SnippetIndex) DrawMarquee(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        AnalysisSnapshot snapshot,
        IReadOnlyList<PaletteColor> palette,
        double speedBurst,
        int w,
        int h)
    {
        var snippets = layer.TextSnippets?.Where(s => !string.IsNullOrEmpty(s)).ToList() ?? new List<string>();
        string text = snippets.Count > 0
            ? snippets[state.SnippetIndex % Math.Max(1, snippets.Count)]
            : "  Layered text  ";
        if (text.Length == 0)
        {
            text = " ";
        }

        double speed = layer.SpeedMultiplier * speedBurst * 0.8;
        if (layer.BeatReaction == TextLayerBeatReaction.SpeedBurst && snapshot.BeatFlashActive)
        {
            speed *= 2.5;
        }

        state.Offset += speed;
        if (layer.BeatReaction == TextLayerBeatReaction.Flash && snapshot.BeatFlashActive)
        {
            state.Offset += 1.0;
        }

        int scrollOffset = (int)Math.Floor(state.Offset) % (text.Length + w);
        if (scrollOffset < 0)
        {
            scrollOffset = (scrollOffset % (text.Length + w) + (text.Length + w)) % (text.Length + w);
        }

        int centerY = h / 2;
        bool pulse = layer.BeatReaction == TextLayerBeatReaction.Pulse && snapshot.BeatFlashActive;
        var color = palette[Math.Max(0, layer.ColorIndex % palette.Count)];
        if (pulse)
        {
            color = palette[(layer.ColorIndex + 1) % palette.Count];
        }

        for (int x = 0; x < w; x++)
        {
            int srcIndex = (scrollOffset + x) % (text.Length + w);
            char c = ' ';
            if (srcIndex < text.Length)
            {
                c = text[srcIndex];
                if (pulse && c != ' ')
                {
                    c = char.IsLower(c) ? char.ToUpperInvariant(c) : (c == ' ' ? ' ' : '█');
                }
            }
            _buffer.Set(x, centerY, c, color);
        }

        return state;
    }

    private (double Offset, int SnippetIndex) DrawFallingLetters(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        AnalysisSnapshot snapshot,
        IReadOnlyList<PaletteColor> palette,
        double speedBurst,
        int w,
        int h,
        int layerIndex)
    {
        var particles = _fallingLettersByLayer[layerIndex];
        string charsSource = " ";
        var snippets = layer.TextSnippets?.Where(s => !string.IsNullOrEmpty(s)).ToList();
        if (snippets is { Count: > 0 })
        {
            charsSource = string.Join("", snippets);
        }
        if (charsSource.Length == 0)
        {
            charsSource = ".*#%";
        }

        double fallSpeed = layer.SpeedMultiplier * speedBurst * 0.4;
        if (layer.BeatReaction == TextLayerBeatReaction.SpawnMore && snapshot.BeatFlashActive)
        {
            for (int k = 0; k < 3; k++)
            {
                int col = Random.Shared.Next(0, Math.Max(1, w));
                particles.Add(new FallingLetterState { Col = col, Y = 0, Char = charsSource[Random.Shared.Next(0, charsSource.Length)] });
            }
        }
        if (layer.BeatReaction == TextLayerBeatReaction.SpeedBurst && snapshot.BeatFlashActive)
        {
            fallSpeed *= 2.0;
        }

        state.Offset += 0.1;
        if (state.Offset > 2.0 && particles.Count < w)
        {
            state.Offset = 0;
            int col = Random.Shared.Next(0, Math.Max(1, w));
            particles.Add(new FallingLetterState { Col = col, Y = 0, Char = charsSource[Random.Shared.Next(0, charsSource.Length)] });
        }

        int paletteCount = palette.Count;
        for (int i = particles.Count - 1; i >= 0; i--)
        {
            var p = particles[i];
            p.Y += fallSpeed;
            int row = (int)Math.Floor(p.Y);
            if (row >= 0 && row < h && p.Col >= 0 && p.Col < w)
            {
                var color = palette[(layer.ColorIndex + i) % paletteCount];
                _buffer.Set(p.Col, row, p.Char, color);
            }
            if (row >= h)
            {
                particles.RemoveAt(i);
            }
            else
            {
                particles[i] = p;
            }
        }
        return state;
    }

    private (double Offset, int SnippetIndex) DrawWaveText(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        AnalysisSnapshot snapshot,
        IReadOnlyList<PaletteColor> palette,
        double speedBurst,
        int w,
        int h)
    {
        var snippets = layer.TextSnippets?.Where(s => !string.IsNullOrEmpty(s)).ToList() ?? new List<string>();
        string text = snippets.Count > 0 ? snippets[state.SnippetIndex % Math.Max(1, snippets.Count)] : "Wave";
        if (text.Length == 0)
        {
            text = " ";
        }

        if (layer.BeatReaction == TextLayerBeatReaction.Flash && snapshot.BeatFlashActive)
        {
            state.SnippetIndex = (state.SnippetIndex + 1) % Math.Max(1, snippets.Count);
        }

        state.Offset += 0.05 * layer.SpeedMultiplier * speedBurst;
        double phase = state.Offset;
        double amplitude = 2.0 + (snapshot.BeatFlashActive && layer.BeatReaction == TextLayerBeatReaction.Pulse ? 2.0 : 0);
        int centerY = h / 2;
        int startX = Math.Max(0, (w - text.Length) / 2);
        int paletteCount = palette.Count;
        for (int i = 0; i < text.Length; i++)
        {
            int x = startX + i;
            if (x < 0 || x >= w)
            {
                continue;
            }
            double wave = Math.Sin(phase + i * 0.3) * amplitude;
            int y = centerY + (int)Math.Round(wave);
            if (y >= 0 && y < h)
            {
                var color = palette[(layer.ColorIndex + i) % paletteCount];
                _buffer.Set(x, y, text[i], color);
            }
        }
        return state;
    }

    private (double Offset, int SnippetIndex) DrawStaticText(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        AnalysisSnapshot snapshot,
        IReadOnlyList<PaletteColor> palette,
        int w,
        int h)
    {
        var snippets = layer.TextSnippets?.Where(s => !string.IsNullOrEmpty(s)).ToList() ?? new List<string>();
        string text = snippets.Count > 0 ? snippets[state.SnippetIndex % Math.Max(1, snippets.Count)] : "Static";
        if (text.Length == 0)
        {
            text = " ";
        }

        if (layer.BeatReaction == TextLayerBeatReaction.Flash && snapshot.BeatFlashActive)
        {
            state.SnippetIndex = (state.SnippetIndex + 1) % Math.Max(1, snippets.Count);
            text = snippets.Count > 0 ? snippets[state.SnippetIndex % snippets.Count] : text;
        }

        int centerY = h / 2;
        int startX = Math.Max(0, (w - text.Length) / 2);
        var color = palette[Math.Max(0, layer.ColorIndex % palette.Count)];
        if (layer.BeatReaction == TextLayerBeatReaction.Pulse && snapshot.BeatFlashActive)
        {
            color = palette[(layer.ColorIndex + 1) % palette.Count];
        }
        for (int i = 0; i < text.Length; i++)
        {
            int x = startX + i;
            if (x >= 0 && x < w)
            {
                _buffer.Set(x, centerY, text[i], color);
            }
        }
        return state;
    }

    private (double Offset, int SnippetIndex) DrawMatrixRain(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        AnalysisSnapshot snapshot,
        IReadOnlyList<PaletteColor> palette,
        double speedBurst,
        int w,
        int h)
    {
        string chars = "01";
        var snippets = layer.TextSnippets?.Where(s => !string.IsNullOrEmpty(s)).ToList();
        if (snippets is { Count: > 0 })
        {
            chars = string.Join("", snippets).Length > 0 ? string.Join("", snippets) : "01";
        }

        double colPhase = state.Offset;
        state.Offset += 0.15 * layer.SpeedMultiplier * speedBurst;
        if (layer.BeatReaction == TextLayerBeatReaction.Flash && snapshot.BeatFlashActive)
        {
            colPhase += Random.Shared.Next(0, 20);
        }

        int paletteCount = palette.Count;
        for (int x = 0; x < w; x += 2)
        {
            double seed = (x * 1.3 + colPhase) % 100;
            int headRow = (int)Math.Abs(seed * 0.4) % (h + 5) - 2;
            for (int d = 0; d < 8 && headRow - d >= 0; d++)
            {
                int y = headRow - d;
                if (y >= h)
                {
                    continue;
                }
                char c = d == 0 ? chars[Random.Shared.Next(0, chars.Length)] : (char)('0' + (d % 2));
                int colorIdx = (layer.ColorIndex + x + d) % paletteCount;
                var color = palette[colorIdx];
                if (y >= 0)
                {
                    _buffer.Set(x, y, c, color);
                }
            }
        }
        return state;
    }

    public string? GetToolbarSuffix(AnalysisSnapshot snapshot)
    {
        var config = snapshot.TextLayersConfig;
        if (config?.Layers is not { Count: > 0 })
        {
            return "Layers: (config in settings)";
        }
        return $"Layers: {config.Layers.Count} (1–9: switch layer text)";
    }

    /// <summary>
    /// Handles keys 1–9 to switch (cycle) the text snippet for the Nth frontmost layer.
    /// 1 = frontmost, 2 = second frontmost, etc. Returns true if the key was handled.
    /// </summary>
    public bool HandleKey(ConsoleKey key, TextLayersVisualizerSettings? config)
    {
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
        int frontIndex = sortedLayers.Count - digit; // 1 = last, 2 = second-to-last, etc.
        if (frontIndex < 0)
        {
            return false;
        }

        // Ensure _layerStates has enough entries
        while (_layerStates.Count < sortedLayers.Count)
        {
            _layerStates.Add((0, 0));
        }

        var layer = sortedLayers[frontIndex];
        var snippets = layer.TextSnippets?.Where(s => !string.IsNullOrEmpty(s)).ToList() ?? new List<string>();
        int count = Math.Max(1, snippets.Count);
        var state = _layerStates[frontIndex];
        state.SnippetIndex = (state.SnippetIndex + 1) % count;
        _layerStates[frontIndex] = state;
        return true;
    }
}
