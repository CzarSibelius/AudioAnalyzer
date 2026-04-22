using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Concentric mandala-style rings (and optional spokes) whose radii breathe with tempo (when BPM is in range) and spectrum energy.</summary>
public sealed class MandalaRingPulseLayer : TextLayerRendererBase, ITextLayerRenderer<MandalaRingPulseState>
{
    private const double AspectRatio = 2.0;
    private const string IntensityChars = "·░▒";

    private readonly ITextLayerStateStore<MandalaRingPulseState> _stateStore;

    public MandalaRingPulseLayer(ITextLayerStateStore<MandalaRingPulseState> stateStore)
    {
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
    }

    public override TextLayerType LayerType => TextLayerType.MandalaRingPulse;

    public override (double Offset, int SnippetIndex) Draw(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        TextLayerDrawContext ctx)
    {
        int w = ctx.Width;
        int h = ctx.Height;
        var snapshot = ctx.Analysis;
        var st = _stateStore.GetState(ctx.LayerIndex);
        var s = layer.GetCustom<MandalaRingPulseSettings>() ?? new MandalaRingPulseSettings();

        int ringCount = Math.Clamp(s.RingCount, 3, 16);
        int symmetry = Math.Clamp(s.Symmetry, 3, 16);
        int pulsesPerBeat = Math.Clamp(s.PulsesPerBeat, 1, 8);
        double pulseDepth = Math.Clamp(s.PulseDepth, 0, 0.45);
        double energyMix = Math.Clamp(s.EnergyMix, 0, 1);

        double frameDelta = ctx.FrameDeltaSeconds > 0 ? ctx.FrameDeltaSeconds : 1.0 / DisplayAnimationTiming.ReferenceHz;

        double instantEnergy = ComputeBroadbandEnergy(snapshot);
        st.SmoothedEnergy = st.SmoothedEnergy * 0.85 + instantEnergy * 0.15;

        double tempoBpm = snapshot.CurrentBpm is >= 30 and <= 300 ? snapshot.CurrentBpm : 0;
        double tempoMult = layer.SpeedMultiplier * ctx.SpeedBurst;
        if (s.BeatReaction == MandalaRingPulseBeatReaction.SpeedBurst && snapshot.BeatFlashActive)
        {
            tempoMult *= 1.35;
        }

        double phaseSpeed = tempoBpm > 0 ? Math.Tau * (tempoBpm / 60.0) * pulsesPerBeat * tempoMult : 0;
        st.PhaseRadians += phaseSpeed * frameDelta;
        st.AngularOffset += s.AngularMotion * frameDelta * layer.SpeedMultiplier * ctx.SpeedBurst;

        bool usePalette = ctx.Palette.Count > 0;
        int flashBump = s.BeatReaction == MandalaRingPulseBeatReaction.Flash && snapshot.BeatFlashActive ? 1 : 0;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                double nx = w > 1 ? (double)x / (w - 1) : 0.5;
                double ny = h > 1 ? (double)y / (h - 1) : 0.5;
                double dx = nx - 0.5;
                double dy = (ny - 0.5) / AspectRatio;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                double angle = Math.Atan2(dy, dx);

                double angularGate = 0.55 + 0.45 * Math.Cos(symmetry * angle + st.AngularOffset);
                double ringBreath = pulseDepth * Math.Sin(st.PhaseRadians + dist * Math.Tau * 0.9);

                bool hit = false;
                double ringStrength = 0;
                for (int k = 0; k < ringCount; k++)
                {
                    double t = (k + 0.5) / (ringCount + 1);
                    double rCenter = t * 0.52 * (1.0 + ringBreath * (0.85 + 0.15 * Math.Sin(k * 0.7)));
                    double thickness = 0.008 + st.SmoothedEnergy * energyMix * 0.022 + pulseDepth * 0.012;
                    if (Math.Abs(dist - rCenter) < thickness)
                    {
                        hit = true;
                        double th = Math.Max(thickness, 1e-6);
                        ringStrength = 1.0 - Math.Abs(dist - rCenter) / th;
                        break;
                    }
                }

                if (!hit && s.Pattern == MandalaRingPulsePattern.RingAndSpoke)
                {
                    double adjAngle = angle + st.AngularOffset / symmetry;
                    double rel = adjAngle * symmetry / Math.Tau;
                    double frac = rel - Math.Floor(rel);
                    double angularDist = Math.Min(frac, 1.0 - frac) * (Math.Tau / symmetry);
                    double spokeWidthRad = 0.11;
                    if (angularDist < spokeWidthRad && dist < 0.52)
                    {
                        hit = true;
                        ringStrength = 1.0 - angularDist / spokeWidthRad;
                    }
                }

                if (!hit)
                {
                    continue;
                }

                ringStrength *= angularGate;
                int charIdx = (int)Math.Clamp(ringStrength * (IntensityChars.Length - 1), 0, IntensityChars.Length - 1);
                char ch = IntensityChars[charIdx];
                int colorIdx = layer.ColorIndex + (int)(dist * 12) + flashBump;
                PaletteColor color;
                if (usePalette)
                {
                    int pc = ctx.Palette.Count;
                    int pi = ((colorIdx % pc) + pc) % pc;
                    color = ctx.Palette[pi];
                }
                else
                {
                    color = PaletteColor.FromConsoleColor(ConsoleColor.Cyan);
                }

                ctx.SetLocal(x, y, ch, color);
            }
        }

        return state;
    }

    private static double ComputeBroadbandEnergy(AudioAnalysisSnapshot snapshot)
    {
        var mags = snapshot.SmoothedMagnitudes;
        if (mags.Length == 0)
        {
            return 0;
        }

        double gain = snapshot.TargetMaxMagnitude > 0.0001 ? Math.Min(1000, 1.0 / snapshot.TargetMaxMagnitude) : 1000;
        double sum = 0;
        for (int i = 0; i < mags.Length; i++)
        {
            sum += mags[i] * gain;
        }

        return Math.Clamp(sum / mags.Length, 0, 1);
    }
}
