using AudioAnalyzer.Application;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Renders a scrolling marquee text layer.</summary>
public sealed class MarqueeLayer : TextLayerRendererBase, ITextLayerRenderer<NoLayerState>
{
    public override TextLayerType LayerType => TextLayerType.Marquee;

    public override (double Offset, int SnippetIndex) Draw(
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

        var s = layer.GetCustom<MarqueeSettings>() ?? new MarqueeSettings();
        double speed = layer.SpeedMultiplier * ctx.SpeedBurst * 0.8;
        if (s.BeatReaction == MarqueeBeatReaction.SpeedBurst && ctx.Analysis.BeatFlashActive)
        {
            speed *= 2.5;
        }

        double dtScale = DisplayAnimationTiming.ScaleForReference60(ctx.FrameDeltaSeconds);
        state.Offset += speed * dtScale;
        if (s.BeatReaction == MarqueeBeatReaction.Flash && ctx.Analysis.BeatFlashActive)
        {
            state.Offset += 1.0 * dtScale;
        }

        int scrollOffset = (int)Math.Floor(state.Offset) % (text.Length + w);
        if (scrollOffset < 0)
        {
            scrollOffset = (scrollOffset % (text.Length + w) + (text.Length + w)) % (text.Length + w);
        }

        int centerY = h / 2;
        bool pulse = s.BeatReaction == MarqueeBeatReaction.Pulse && ctx.Analysis.BeatFlashActive;
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
            ctx.SetLocal(x, centerY, c, color);
        }

        return state;
    }
}
