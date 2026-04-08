using AudioAnalyzer.Application;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Renders a Mandelbrot or Julia escape-time fractal with continuous zoom animation.</summary>
public sealed class FractalZoomLayer : TextLayerRendererBase, ITextLayerRenderer<FractalZoomState>
{
    private static readonly char[] DensityChars = [' ', '·', ':', ';', '+', '*', '#', '@', '█'];
    private readonly ITextLayerStateStore<FractalZoomState> _stateStore;

    public FractalZoomLayer(ITextLayerStateStore<FractalZoomState> stateStore)
    {
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
    }

    public override TextLayerType LayerType => TextLayerType.FractalZoom;

    public override (double Offset, int SnippetIndex) Draw(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        TextLayerDrawContext ctx)
    {
        int w = ctx.Width;
        int h = ctx.Height;
        var snapshot = ctx.Analysis;
        var st = _stateStore.GetState(ctx.LayerIndex);
        var s = layer.GetCustom<FractalZoomSettings>() ?? new FractalZoomSettings();

        int maxIter = Math.Clamp(s.MaxIterations, 4, 32);

        double dtScale = DisplayAnimationTiming.ScaleForReference60(ctx.FrameDeltaSeconds);
        double phaseStep = s.ZoomSpeed * layer.SpeedMultiplier * ctx.SpeedBurst;
        if (s.BeatReaction == FractalZoomBeatReaction.SpeedBurst && snapshot.BeatFlashActive)
        {
            phaseStep *= 2.0;
        }

        st.ZoomPhase += phaseStep * dtScale;
        while (st.ZoomPhase >= 1.0)
        {
            st.ZoomPhase -= 1.0;
            st.OrbitAngle += s.OrbitStep;
        }

        double flashBoost = (s.BeatReaction == FractalZoomBeatReaction.Flash && snapshot.BeatFlashActive) ? 0.12 : 0;

        st.ViewRotation += 0.0018 * layer.SpeedMultiplier * ctx.SpeedBurst * dtScale;

        double logScaleMin = s.LogScaleMin;
        double logScaleMax = s.LogScaleMax;
        if (logScaleMin > logScaleMax)
        {
            (logScaleMin, logScaleMax) = (logScaleMax, logScaleMin);
        }

        double scaleT = FractalZoomAnimation.RemapPhaseToScaleT(st.ZoomPhase, s.Dwell);
        double logS = logScaleMin + scaleT * (logScaleMax - logScaleMin);
        double pixelScale = Math.Exp(logS);

        const double baseCr = -0.75;
        const double baseCi = 0.05;
        const double orbitRadius = 0.085;
        double centerRe = baseCr + orbitRadius * Math.Cos(st.OrbitAngle);
        double centerIm = baseCi + orbitRadius * Math.Sin(st.OrbitAngle);

        double cosA = Math.Cos(st.ViewRotation);
        double sinA = Math.Sin(st.ViewRotation);
        int minDim = Math.Min(w, h);
        double norm = minDim > 0 ? minDim * 0.5 : 1.0;

        bool usePalette = ctx.Palette.Count > 0;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                double nx = (x - w * 0.5) / norm;
                double ny = (y - h * 0.5) / norm;
                double rx = nx * cosA - ny * sinA;
                double ry = nx * sinA + ny * cosA;
                double cr = centerRe + rx * pixelScale;
                double ci = centerIm - ry * pixelScale;

                double smooth = s.FractalMode == FractalZoomMode.Mandelbrot
                    ? FractalZoomSampler.EscapeSmoothMandelbrot(cr, ci, maxIter)
                    : FractalZoomSampler.EscapeSmoothJulia(cr, ci, s.JuliaRe, s.JuliaIm, maxIter);

                double t = smooth / maxIter + flashBoost;
                if (t > 1.0)
                {
                    t = 1.0;
                }

                int charIdx = Math.Min((int)(t * (DensityChars.Length - 1)), DensityChars.Length - 1);
                char ch = DensityChars[charIdx];

                PaletteColor color;
                if (usePalette)
                {
                    double hue = (t * 0.85 + scaleT * 0.15) % 1.0;
                    if (hue < 0)
                    {
                        hue += 1.0;
                    }

                    int paletteIndex = (int)(hue * ctx.Palette.Count) % ctx.Palette.Count;
                    if (paletteIndex < 0)
                    {
                        paletteIndex = (paletteIndex % ctx.Palette.Count + ctx.Palette.Count) % ctx.Palette.Count;
                    }

                    color = ctx.Palette[paletteIndex];
                }
                else
                {
                    color = PaletteColor.FromConsoleColor(GetFallbackColor(t));
                }

                ctx.SetLocal(x, y, ch, color);
            }
        }

        return state;
    }

    private static ConsoleColor GetFallbackColor(double t)
    {
        if (t < 0.15)
        {
            return ConsoleColor.DarkBlue;
        }

        int idx = (int)(t * 8) % 8;
        return idx switch
        {
            0 => ConsoleColor.DarkYellow,
            1 => ConsoleColor.Yellow,
            2 => ConsoleColor.Green,
            3 => ConsoleColor.Cyan,
            4 => ConsoleColor.Blue,
            5 => ConsoleColor.Magenta,
            6 => ConsoleColor.DarkMagenta,
            _ => ConsoleColor.White
        };
    }
}
