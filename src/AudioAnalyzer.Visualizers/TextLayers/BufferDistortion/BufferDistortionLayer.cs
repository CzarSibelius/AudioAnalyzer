using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Viewports;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>
/// Post-process layer: snapshots the current <see cref="ViewportCellBuffer"/> in its effect rectangle (full viewport or <see cref="TextLayerSettings.RenderBounds"/>),
/// then redraws it with sinusoidal plane waves or radial beat-spawned ripples. Place above layers whose composite should be warped (same pattern as <see cref="MirrorLayer"/>).
/// </summary>
public sealed class BufferDistortionLayer : TextLayerRendererBase, ITextLayerRenderer<BufferDistortionState>
{
    private readonly ITextLayerStateStore<BufferDistortionState> _stateStore;
    private readonly Random _rng = new();

    private char[] _scratchChars = [];
    private PaletteColor[] _scratchColors = [];

    public BufferDistortionLayer(ITextLayerStateStore<BufferDistortionState> stateStore)
    {
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
    }

    public override TextLayerType LayerType => TextLayerType.BufferDistortion;

    public override (double Offset, int SnippetIndex) Draw(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        TextLayerDrawContext ctx)
    {
        var settings = layer.GetCustom<BufferDistortionSettings>() ?? new BufferDistortionSettings();
        var st = _stateStore.GetState(ctx.LayerIndex);
        double dt = ctx.FrameDeltaSeconds > 0 ? ctx.FrameDeltaSeconds : 1.0 / 60.0;
        double speed = layer.SpeedMultiplier <= 0 ? 1.0 : layer.SpeedMultiplier;
        double scaleT = DisplayAnimationTiming.ScaleForReference60(dt) * speed;

        var (rx, ry, rw, rh) = TextLayerRenderBounds.ToPixelRect(layer.RenderBounds, ctx.ViewportWidth, ctx.ViewportHeight);
        if (rw < 1 || rh < 1)
        {
            return state;
        }

        var buffer = ctx.Buffer;
        EnsureScratch(rw * rh);
        CopyRect(buffer, rx, ry, rw, rh);

        if (settings.Mode == BufferDistortionMode.PlaneWaves)
        {
            st.PlanePhase += settings.PlanePhaseSpeed * scaleT;
        }
        else
        {
            UpdateRipples(settings, st, rw, rh, ctx.Analysis.BeatCount, dt);
        }

        int maxDisp = Math.Clamp(settings.MaxDisplacementCells, 1, 12);
        double waveLen = Math.Max(2.0, settings.PlaneWavelengthCells);
        double k = 2.0 * Math.PI / waveLen;

        for (int ly = 0; ly < rh; ly++)
        {
            for (int lx = 0; lx < rw; lx++)
            {
                double dx = 0;
                double dy = 0;

                if (settings.Mode == BufferDistortionMode.PlaneWaves)
                {
                    AddPlaneDisplacement(settings, st.PlanePhase, k, lx, ly, ref dx, ref dy);
                }
                else
                {
                    AddRippleDisplacement(settings, st, lx, ly, ref dx, ref dy);
                }

                ClampDisplacement(ref dx, ref dy, maxDisp);

                int sx = lx + (int)Math.Round(dx);
                int sy = ly + (int)Math.Round(dy);
                sx = Math.Clamp(sx, 0, rw - 1);
                sy = Math.Clamp(sy, 0, rh - 1);
                int si = sy * rw + sx;
                buffer.Set(rx + lx, ry + ly, _scratchChars[si], _scratchColors[si]);
            }
        }

        return state;
    }

    private void EnsureScratch(int count)
    {
        if (_scratchChars.Length >= count)
        {
            return;
        }

        _scratchChars = new char[count];
        _scratchColors = new PaletteColor[count];
    }

    private void CopyRect(ViewportCellBuffer buffer, int rx, int ry, int rw, int rh)
    {
        int i = 0;
        for (int y = 0; y < rh; y++)
        {
            for (int x = 0; x < rw; x++)
            {
                var (c, col) = buffer.Get(rx + x, ry + y);
                _scratchChars[i] = c;
                _scratchColors[i] = col;
                i++;
            }
        }
    }

    private static void AddPlaneDisplacement(
        BufferDistortionSettings settings,
        double phase,
        double k,
        int lx,
        int ly,
        ref double dx,
        ref double dy)
    {
        int a = Math.Clamp(settings.PlaneAmplitudeCells, 0, 12);
        if (a == 0)
        {
            return;
        }

        switch (settings.PlaneOrientation)
        {
            case BufferDistortionPlaneOrientation.WaveAlongX:
                dy += a * Math.Sin(phase + k * lx);
                break;
            case BufferDistortionPlaneOrientation.WaveAlongY:
                dx += a * Math.Sin(phase + k * ly);
                break;
            case BufferDistortionPlaneOrientation.Both:
                dx += (a * 0.5) * Math.Sin(phase + k * ly);
                dy += (a * 0.5) * Math.Sin(phase * 1.07 + k * lx);
                break;
        }
    }

    private void UpdateRipples(
        BufferDistortionSettings settings,
        BufferDistortionState st,
        int rw,
        int rh,
        int beatCount,
        double dt)
    {
        for (int i = st.Ripples.Count - 1; i >= 0; i--)
        {
            BufferDistortionRipple r = st.Ripples[i];
            r.AgeSeconds += dt;
            if (r.AgeSeconds >= settings.RippleMaxAgeSeconds)
            {
                st.Ripples.RemoveAt(i);
            }
        }

        if (st.LastBeatCountForSpawn == int.MinValue)
        {
            st.LastBeatCountForSpawn = beatCount;
        }
        else if (settings.SpawnOnBeat && beatCount > st.LastBeatCountForSpawn && rw >= 2 && rh >= 2)
        {
            st.LastBeatCountForSpawn = beatCount;
            TryAddRipple(settings, st, rw, rh);
        }
    }

    private void TryAddRipple(BufferDistortionSettings settings, BufferDistortionState st, int rw, int rh)
    {
        int max = Math.Clamp(settings.MaxRipples, 1, 64);
        while (st.Ripples.Count >= max)
        {
            st.Ripples.RemoveAt(0);
        }

        st.Ripples.Add(new BufferDistortionRipple
        {
            CenterX = (float)(_rng.NextDouble() * (rw - 1)),
            CenterY = (float)(_rng.NextDouble() * (rh - 1))
        });
    }

    private static void AddRippleDisplacement(
        BufferDistortionSettings settings,
        BufferDistortionState st,
        int lx,
        int ly,
        ref double dx,
        ref double dy)
    {
        int amp = Math.Clamp(settings.RippleAmplitudeCells, 0, 12);
        if (amp == 0 || st.Ripples.Count == 0)
        {
            return;
        }

        double wn = settings.RippleWaveNumber;
        double ts = settings.RippleTimeSpeed;
        double decay = settings.RippleDecayPerSecond;

        foreach (BufferDistortionRipple r in st.Ripples)
        {
            double dlx = lx - r.CenterX;
            double dly = ly - r.CenterY;
            double dist = Math.Sqrt(dlx * dlx + dly * dly);
            if (dist < 1e-3)
            {
                continue;
            }

            double env = Math.Exp(-decay * r.AgeSeconds);
            if (env < 1e-4)
            {
                continue;
            }

            double phase = wn * dist - ts * r.AgeSeconds;
            double bump = amp * Math.Sin(phase) * env;
            double ux = dlx / dist;
            double uy = dly / dist;
            dx += ux * bump;
            dy += uy * bump;
        }
    }

    private static void ClampDisplacement(ref double dx, ref double dy, int maxCells)
    {
        double len = Math.Sqrt(dx * dx + dy * dy);
        if (len > maxCells && len > 1e-6)
        {
            double s = maxCells / len;
            dx *= s;
            dy *= s;
        }
    }
}
