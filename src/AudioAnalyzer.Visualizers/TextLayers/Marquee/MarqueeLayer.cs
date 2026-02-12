using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Renders a scrolling marquee text layer.</summary>
public sealed class MarqueeLayer : ITextLayerRenderer
{
    public TextLayerType LayerType => TextLayerType.Marquee;

    public (double Offset, int SnippetIndex) Draw(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        TextLayerDrawContext ctx)
    {
        int w = ctx.Width;
        int h = ctx.Height;
        var snippets = layer.TextSnippets?.Where(s => !string.IsNullOrEmpty(s)).ToList() ?? new List<string>();
        string text = snippets.Count > 0
            ? snippets[state.SnippetIndex % Math.Max(1, snippets.Count)]
            : "  Layered text  ";
        if (text.Length == 0)
        {
            text = " ";
        }

        double speed = layer.SpeedMultiplier * ctx.SpeedBurst * 0.8;
        if (layer.BeatReaction == TextLayerBeatReaction.SpeedBurst && ctx.Snapshot.BeatFlashActive)
        {
            speed *= 2.5;
        }

        state.Offset += speed;
        if (layer.BeatReaction == TextLayerBeatReaction.Flash && ctx.Snapshot.BeatFlashActive)
        {
            state.Offset += 1.0;
        }

        int scrollOffset = (int)Math.Floor(state.Offset) % (text.Length + w);
        if (scrollOffset < 0)
        {
            scrollOffset = (scrollOffset % (text.Length + w) + (text.Length + w)) % (text.Length + w);
        }

        int centerY = h / 2;
        bool pulse = layer.BeatReaction == TextLayerBeatReaction.Pulse && ctx.Snapshot.BeatFlashActive;
        var color = ctx.Palette[Math.Max(0, layer.ColorIndex % ctx.Palette.Count)];
        if (pulse)
        {
            color = ctx.Palette[(layer.ColorIndex + 1) % ctx.Palette.Count];
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
            ctx.Buffer.Set(x, centerY, c, color);
        }

        return state;
    }
}
