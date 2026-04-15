using AudioAnalyzer.Application;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Renders falling letter particles or column-style glyph rain (merged MatrixRain).</summary>
public sealed class FallingLettersLayer : TextLayerRendererBase, ITextLayerRenderer<FallingLettersLayerState>
{
    private readonly ITextLayerStateStore<FallingLettersLayerState> _stateStore;
    private readonly CharsetResolver _charsetResolver;

    public FallingLettersLayer(
        ITextLayerStateStore<FallingLettersLayerState> stateStore,
        CharsetResolver charsetResolver)
    {
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _charsetResolver = charsetResolver ?? throw new ArgumentNullException(nameof(charsetResolver));
    }

    public override TextLayerType LayerType => TextLayerType.FallingLetters;

    public override (double Offset, int SnippetIndex) Draw(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        TextLayerDrawContext ctx)
    {
        var layerState = _stateStore.GetState(ctx.LayerIndex);
        var s = layer.GetCustom<FallingLettersSettings>() ?? new FallingLettersSettings();

        if (layerState.LastSyncedAnimationMode is { } prevMode && prevMode != s.AnimationMode)
        {
            layerState.Particles.Clear();
        }

        layerState.LastSyncedAnimationMode = s.AnimationMode;

        string literalFallback = s.AnimationMode == FallingLettersAnimationMode.ColumnRain ? "01" : ".*#%";
        string charsSource = _charsetResolver.ResolveByIdOrDefault(s.CharsetId, CharsetIds.Digits, literalFallback);
        if (charsSource.Length == 0)
        {
            charsSource = literalFallback;
        }

        int w = ctx.Width;
        int h = ctx.Height;

        return s.AnimationMode == FallingLettersAnimationMode.ColumnRain
            ? DrawColumnRain(layer, ref state, ctx, s, charsSource, w, h)
            : DrawParticles(layer, ref state, ctx, s, charsSource, w, h, layerState.Particles);
    }

    private static (double Offset, int SnippetIndex) DrawColumnRain(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        TextLayerDrawContext ctx,
        FallingLettersSettings s,
        string chars,
        int w,
        int h)
    {
        double colPhase = state.Offset;
        state.Offset += 0.15 * layer.SpeedMultiplier * ctx.SpeedBurst * DisplayAnimationTiming.ScaleForReference60(ctx.FrameDeltaSeconds);
        if (s.BeatReaction == FallingLettersBeatReaction.Flash && ctx.Analysis.BeatFlashActive)
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
                    ctx.SetLocal(x, y, c, color);
                }
            }
        }

        return state;
    }

    private static (double Offset, int SnippetIndex) DrawParticles(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        TextLayerDrawContext ctx,
        FallingLettersSettings s,
        string charsSource,
        int w,
        int h,
        List<FallingLetterState> particles)
    {
        double fallSpeed = layer.SpeedMultiplier * ctx.SpeedBurst * 0.4;
        if (s.BeatReaction == FallingLettersBeatReaction.SpawnMore && ctx.Analysis.BeatFlashActive)
        {
            for (int k = 0; k < 3; k++)
            {
                int col = Random.Shared.Next(0, Math.Max(1, w));
                particles.Add(new FallingLetterState { Col = col, Y = 0, Character = charsSource[Random.Shared.Next(0, charsSource.Length)] });
            }
        }

        if (s.BeatReaction == FallingLettersBeatReaction.SpeedBurst && ctx.Analysis.BeatFlashActive)
        {
            fallSpeed *= 2.0;
        }

        double dtScale = DisplayAnimationTiming.ScaleForReference60(ctx.FrameDeltaSeconds);
        state.Offset += 0.1 * dtScale;
        double effectiveSpawnPhase = state.Offset;
        if (s.BeatReaction == FallingLettersBeatReaction.Flash && ctx.Analysis.BeatFlashActive)
        {
            effectiveSpawnPhase += Random.Shared.Next(0, 20);
        }

        if (effectiveSpawnPhase > 2.0 && particles.Count < w)
        {
            state.Offset = 0;
            int col = Random.Shared.Next(0, Math.Max(1, w));
            particles.Add(new FallingLetterState { Col = col, Y = 0, Character = charsSource[Random.Shared.Next(0, charsSource.Length)] });
        }

        int paletteCount = ctx.Palette.Count;
        for (int i = particles.Count - 1; i >= 0; i--)
        {
            var p = particles[i];
            p.Y += fallSpeed * dtScale;
            int row = (int)Math.Floor(p.Y);
            if (row >= 0 && row < h && p.Col >= 0 && p.Col < w)
            {
                var color = ctx.Palette[(layer.ColorIndex + i) % paletteCount];
                ctx.SetLocal(p.Col, row, p.Character, color);
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
