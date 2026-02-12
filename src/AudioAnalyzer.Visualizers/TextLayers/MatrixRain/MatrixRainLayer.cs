namespace AudioAnalyzer.Visualizers;

/// <summary>Renders matrix-style falling digits.</summary>
public sealed class MatrixRainLayer : ITextLayerRenderer
{
    public TextLayerType LayerType => TextLayerType.MatrixRain;

    public (double Offset, int SnippetIndex) Draw(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        TextLayerDrawContext ctx)
    {
        int w = ctx.Width;
        int h = ctx.Height;

        string chars = "01";
        var snippets = layer.TextSnippets?.Where(s => !string.IsNullOrEmpty(s)).ToList();
        if (snippets is { Count: > 0 })
        {
            chars = string.Join("", snippets).Length > 0 ? string.Join("", snippets) : "01";
        }

        double colPhase = state.Offset;
        state.Offset += 0.15 * layer.SpeedMultiplier * ctx.SpeedBurst;
        if (layer.BeatReaction == TextLayerBeatReaction.Flash && ctx.Snapshot.BeatFlashActive)
        {
            colPhase += Random.Shared.Next(0, 20);
        }

        int paletteCount = ctx.Palette.Count;
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
                var color = ctx.Palette[colorIdx];
                if (y >= 0)
                {
                    ctx.Buffer.Set(x, y, c, color);
                }
            }
        }
        return state;
    }
}
