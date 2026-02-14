using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Renders falling letter particles.</summary>
public sealed class FallingLettersLayer : ITextLayerRenderer
{
    public TextLayerType LayerType => TextLayerType.FallingLetters;

    public (double Offset, int SnippetIndex) Draw(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        TextLayerDrawContext ctx)
    {
        var particles = ctx.FallingLettersForLayer;
        int w = ctx.Width;
        int h = ctx.Height;

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

        double fallSpeed = layer.SpeedMultiplier * ctx.SpeedBurst * 0.4;
        if (layer.BeatReaction == TextLayerBeatReaction.SpawnMore && ctx.Snapshot.BeatFlashActive)
        {
            for (int k = 0; k < 3; k++)
            {
                int col = Random.Shared.Next(0, Math.Max(1, w));
                particles.Add(new FallingLetterState { Col = col, Y = 0, Character = charsSource[Random.Shared.Next(0, charsSource.Length)] });
            }
        }
        if (layer.BeatReaction == TextLayerBeatReaction.SpeedBurst && ctx.Snapshot.BeatFlashActive)
        {
            fallSpeed *= 2.0;
        }

        state.Offset += 0.1;
        if (state.Offset > 2.0 && particles.Count < w)
        {
            state.Offset = 0;
            int col = Random.Shared.Next(0, Math.Max(1, w));
            particles.Add(new FallingLetterState { Col = col, Y = 0, Character = charsSource[Random.Shared.Next(0, charsSource.Length)] });
        }

        int paletteCount = ctx.Palette.Count;
        for (int i = particles.Count - 1; i >= 0; i--)
        {
            var p = particles[i];
            p.Y += fallSpeed;
            int row = (int)Math.Floor(p.Y);
            if (row >= 0 && row < h && p.Col >= 0 && p.Col < w)
            {
                var color = ctx.Palette[(layer.ColorIndex + i) % paletteCount];
                ctx.Buffer.Set(p.Col, row, p.Character, color);
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
}
