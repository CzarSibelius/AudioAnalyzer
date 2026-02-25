using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Renders text with sinusoidal wave motion.</summary>
public sealed class WaveTextLayer : TextLayerRendererBase, ITextLayerRenderer<NoLayerState>
{
    public override TextLayerType LayerType => TextLayerType.WaveText;

    public override (double Offset, int SnippetIndex) Draw(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        TextLayerDrawContext ctx)
    {
        int w = ctx.Width;
        int h = ctx.Height;
        var snippets = layer.TextSnippets?.Where(s => !string.IsNullOrEmpty(s)).ToList() ?? new List<string>();
        string text = snippets.Count > 0 ? snippets[state.SnippetIndex % Math.Max(1, snippets.Count)] : "Wave";
        if (text.Length == 0)
        {
            text = " ";
        }

        if (layer.BeatReaction == TextLayerBeatReaction.Flash && ctx.Snapshot.BeatFlashActive)
        {
            state.SnippetIndex = (state.SnippetIndex + 1) % Math.Max(1, snippets.Count);
        }

        state.Offset += 0.05 * layer.SpeedMultiplier * ctx.SpeedBurst;
        double phase = state.Offset;
        double amplitude = 2.0 + (ctx.Snapshot.BeatFlashActive && layer.BeatReaction == TextLayerBeatReaction.Pulse ? 2.0 : 0);
        int centerY = h / 2;
        int startX = Math.Max(0, (w - text.Length) / 2);
        int paletteCount = ctx.Palette.Count;
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
                var color = ctx.Palette[(layer.ColorIndex + i) % paletteCount];
                ctx.Buffer.Set(x, y, text[i], color);
            }
        }
        return state;
    }
}
