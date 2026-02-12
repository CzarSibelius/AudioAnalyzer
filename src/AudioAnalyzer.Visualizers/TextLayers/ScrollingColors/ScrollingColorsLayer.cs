namespace AudioAnalyzer.Visualizers;

/// <summary>Renders a scrolling color grid layer.</summary>
public sealed class ScrollingColorsLayer : ITextLayerRenderer
{
    public TextLayerType LayerType => TextLayerType.ScrollingColors;

    public (double Offset, int SnippetIndex) Draw(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        TextLayerDrawContext ctx)
    {
        int w = ctx.Buffer.Width;
        int h = ctx.Buffer.Height;
        double speed = layer.SpeedMultiplier * ctx.SpeedBurst * 0.5;
        if (layer.BeatReaction == TextLayerBeatReaction.SpeedBurst && ctx.Snapshot.BeatFlashActive)
        {
            speed *= 2.0;
        }

        state.Offset += speed;
        double offset = state.Offset;
        int colorOffset = layer.BeatReaction == TextLayerBeatReaction.ColorPop && ctx.Snapshot.BeatFlashActive
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
                ctx.Buffer.Set(x, y, '░', color);
            }
        }

        return state;
    }
}
