using AudioAnalyzer.Application;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Renders a scrolling color grid layer.</summary>
public sealed class ScrollingColorsLayer : TextLayerRendererBase, ITextLayerRenderer<NoLayerState>
{
    public override TextLayerType LayerType => TextLayerType.ScrollingColors;

    public override (double Offset, int SnippetIndex) Draw(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        TextLayerDrawContext ctx)
    {
        int w = ctx.Width;
        int h = ctx.Height;
        var s = layer.GetCustom<ScrollingColorsSettings>() ?? new ScrollingColorsSettings();
        double speed = layer.SpeedMultiplier * ctx.SpeedBurst * 0.5;
        if (s.BeatReaction == ScrollingColorsBeatReaction.SpeedBurst && ctx.Analysis.BeatFlashActive)
        {
            speed *= 2.0;
        }

        state.Offset += speed * DisplayAnimationTiming.ScaleForReference60(ctx.FrameDeltaSeconds);
        double offset = state.Offset;
        int colorOffset = s.BeatReaction == ScrollingColorsBeatReaction.ColorPop && ctx.Analysis.BeatFlashActive
            ? 1
            : 0;
        int paletteCount = ctx.Palette.Count;

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
                var color = ctx.Palette[idx % paletteCount];
                ctx.SetLocal(x, y, '░', color);
            }
        }

        return state;
    }
}
