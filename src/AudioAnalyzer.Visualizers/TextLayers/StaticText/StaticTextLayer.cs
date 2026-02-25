using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Renders static centered text.</summary>
public sealed class StaticTextLayer : TextLayerRendererBase, ITextLayerRenderer<NoLayerState>
{
    public override TextLayerType LayerType => TextLayerType.StaticText;

    public override (double Offset, int SnippetIndex) Draw(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        TextLayerDrawContext ctx)
    {
        int w = ctx.Width;
        int h = ctx.Height;
        var snippets = layer.TextSnippets?.Where(s => !string.IsNullOrEmpty(s)).ToList() ?? new List<string>();
        string text = snippets.Count > 0 ? snippets[state.SnippetIndex % Math.Max(1, snippets.Count)] : "Static";
        if (text.Length == 0)
        {
            text = " ";
        }

        if (layer.BeatReaction == TextLayerBeatReaction.Flash && ctx.Snapshot.BeatFlashActive)
        {
            state.SnippetIndex = (state.SnippetIndex + 1) % Math.Max(1, snippets.Count);
            text = snippets.Count > 0 ? snippets[state.SnippetIndex % snippets.Count] : text;
        }

        int centerY = h / 2;
        int startX = Math.Max(0, (w - text.Length) / 2);
        var color = ctx.Palette[Math.Max(0, layer.ColorIndex % ctx.Palette.Count)];
        if (layer.BeatReaction == TextLayerBeatReaction.Pulse && ctx.Snapshot.BeatFlashActive)
        {
            color = ctx.Palette[(layer.ColorIndex + 1) % ctx.Palette.Count];
        }
        for (int i = 0; i < text.Length; i++)
        {
            int x = startX + i;
            if (x >= 0 && x < w)
            {
                ctx.Buffer.Set(x, centerY, text[i], color);
            }
        }
        return state;
    }
}
