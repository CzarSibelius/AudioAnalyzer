using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Dual-phase logarithmic spiral interference (moiré) mapped through a density charset, twisted in sync with BPM.</summary>
public sealed class HypnoSpiralLayer : TextLayerRendererBase, ITextLayerRenderer<HypnoSpiralState>
{
    private const double AspectRatio = 2.0;
    private const string DefaultDensityRamp = " \u00B7:;+*#@\u2588";

    private readonly ITextLayerStateStore<HypnoSpiralState> _stateStore;
    private readonly CharsetResolver _charsetResolver;

    public HypnoSpiralLayer(ITextLayerStateStore<HypnoSpiralState> stateStore, CharsetResolver charsetResolver)
    {
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _charsetResolver = charsetResolver ?? throw new ArgumentNullException(nameof(charsetResolver));
    }

    public override TextLayerType LayerType => TextLayerType.HypnoSpiral;

    public override (double Offset, int SnippetIndex) Draw(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        TextLayerDrawContext ctx)
    {
        int w = ctx.Width;
        int h = ctx.Height;
        var snapshot = ctx.Analysis;
        var st = _stateStore.GetState(ctx.LayerIndex);
        var s = layer.GetCustom<HypnoSpiralSettings>() ?? new HypnoSpiralSettings();

        int arms = Math.Clamp(s.ArmCount, 2, 24);
        double logPitch = Math.Clamp(s.LogPitch, 2, 22);
        double revPerBeat = Math.Clamp(s.RevolutionsPerBeat, 0.125, 6);
        double moireMix = Math.Clamp(s.MoireMix, 0, 1);
        double moireDetune = Math.Clamp(s.MoireDetune, 0.92, 1.08);
        double frameDelta = ctx.FrameDeltaSeconds > 0 ? ctx.FrameDeltaSeconds : 1.0 / DisplayAnimationTiming.ReferenceHz;

        double bpm = snapshot.CurrentBpm is >= 30 and <= 300 ? snapshot.CurrentBpm : 120;
        double tempoMult = layer.SpeedMultiplier * ctx.SpeedBurst;
        if (s.BeatReaction == HypnoSpiralBeatReaction.SpeedBurst && snapshot.BeatFlashActive)
        {
            tempoMult *= 1.35;
        }

        double twistSpeed = Math.Tau * (bpm / 60.0) * revPerBeat * tempoMult;
        st.TwistRadians += twistSpeed * frameDelta;
        st.MoireDrift += s.MoireDriftSpeed * frameDelta * layer.SpeedMultiplier;

        bool usePalette = ctx.Palette.Count > 0;
        string ramp = _charsetResolver.ResolveByIdOrDefault(s.CharsetId, CharsetIds.DensitySoft, DefaultDensityRamp);
        if (ramp.Length == 0)
        {
            ramp = DefaultDensityRamp;
        }

        double flashBoost = s.BeatReaction == HypnoSpiralBeatReaction.Flash && snapshot.BeatFlashActive ? 0.08 : 0;
        double minDim = Math.Max(1, Math.Min(w, h));

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                double nx = w > 1 ? (double)x / (w - 1) : 0.5;
                double ny = h > 1 ? (double)y / (h - 1) : 0.5;
                double dx = nx - 0.5;
                double dy = (ny - 0.5) / AspectRatio;
                double rho = Math.Sqrt(dx * dx + dy * dy);
                double theta = Math.Atan2(dy, dx);

                double cellScale = 2.0 / minDim;
                double rhoN = Math.Max(rho, cellScale * 0.35);
                double logR = Math.Log(rhoN * minDim * 0.5 + 1e-4);

                double w1 = Math.Sin(arms * (theta + st.TwistRadians) + logPitch * logR);
                double w2 = Math.Sin(arms * (theta + st.TwistRadians) * moireDetune + logPitch * logR * moireDetune + s.MoirePhase + st.MoireDrift);
                double combined = (1.0 - moireMix) * w1 + moireMix * w2;
                double t = (combined + 1.0) * 0.5 + flashBoost;
                t = Math.Clamp(t, 0.0, 1.0);

                int lastRamp = ramp.Length - 1;
                int charIdx = lastRamp <= 0 ? 0 : Math.Clamp((int)(t * lastRamp), 0, lastRamp);
                char ch = ramp[charIdx];
                int colorIdx = layer.ColorIndex + charIdx + (int)(theta * 2);
                PaletteColor color;
                if (usePalette)
                {
                    int pc = ctx.Palette.Count;
                    int pi = ((colorIdx % pc) + pc) % pc;
                    color = ctx.Palette[pi];
                }
                else
                {
                    color = PaletteColor.FromConsoleColor(ConsoleColor.DarkCyan);
                }

                ctx.SetLocal(x, y, ch, color);
            }
        }

        return state;
    }
}
