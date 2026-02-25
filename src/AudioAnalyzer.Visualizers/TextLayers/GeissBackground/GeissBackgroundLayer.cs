using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Renders the Geiss plasma-style background layer.</summary>
public sealed class GeissBackgroundLayer : TextLayerRendererBase, ITextLayerRenderer<GeissBackgroundState>
{
    private static readonly char[] PlasmaChars = [' ', '·', ':', ';', '+', '*', '#', '@', '█'];
    private readonly ITextLayerStateStore<GeissBackgroundState> _stateStore;

    public GeissBackgroundLayer(ITextLayerStateStore<GeissBackgroundState> stateStore)
    {
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
    }

    public override TextLayerType LayerType => TextLayerType.GeissBackground;

    public override (double Offset, int SnippetIndex) Draw(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        TextLayerDrawContext ctx)
    {
        int w = ctx.Width;
        int h = ctx.Height;
        var snapshot = ctx.Snapshot;
        var geissState = _stateStore.GetState(ctx.LayerIndex);

        double phaseSpeed = layer.SpeedMultiplier * ctx.SpeedBurst * 0.15;
        geissState.Phase += phaseSpeed;
        geissState.ColorPhase += 0.08;

        if (snapshot.SmoothedMagnitudes.Length > 0)
        {
            double gain = snapshot.TargetMaxMagnitude > 0.0001 ? Math.Min(1000, 1.0 / snapshot.TargetMaxMagnitude) : 1000;
            int bassEnd = Math.Max(1, snapshot.SmoothedMagnitudes.Length / 4);
            double bassSum = 0;
            for (int i = 0; i < bassEnd; i++)
            {
                bassSum += snapshot.SmoothedMagnitudes[i] * gain;
            }

            geissState.BassIntensity = geissState.BassIntensity * 0.7 + (bassSum / bassEnd) * 0.3;
            int trebleStart = snapshot.SmoothedMagnitudes.Length * 3 / 4;
            double trebleSum = 0;
            for (int i = trebleStart; i < snapshot.SmoothedMagnitudes.Length; i++)
            {
                trebleSum += snapshot.SmoothedMagnitudes[i] * gain;
            }

            geissState.TrebleIntensity = geissState.TrebleIntensity * 0.7 + (trebleSum / (snapshot.SmoothedMagnitudes.Length - trebleStart)) * 0.3;
        }

        bool usePalette = ctx.Palette.Count > 0;
        double plasmaBoost = (layer.BeatReaction == TextLayerBeatReaction.Flash && snapshot.BeatFlashActive) ? 0.3 : 0;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                double nx = (double)x / w;
                double ny = (double)y / h;

                double v1 = Math.Sin(nx * 10 + geissState.Phase);
                double v2 = Math.Sin(ny * 8 + geissState.Phase * 0.7);
                double v3 = Math.Sin((nx + ny) * 6 + geissState.Phase * 1.3);
                double v4 = Math.Sin(Math.Sqrt((nx - 0.5) * (nx - 0.5) + (ny - 0.5) * (ny - 0.5)) * 12 + geissState.Phase);
                double plasma = (v1 + v2 + v3 + v4) / 4.0;
                double distFromCenterPlasma = Math.Sqrt((nx - 0.5) * (nx - 0.5) + (ny - 0.5) * (ny - 0.5));
                plasma += Math.Sin(distFromCenterPlasma * 20 - geissState.Phase * 2) * geissState.BassIntensity * 0.5;
                plasma += Math.Sin(nx * 30 + ny * 30 + geissState.Phase * 3) * geissState.TrebleIntensity * 0.3;
                plasma += plasmaBoost;

                plasma = Math.Clamp((plasma + 1.5) / 3.0, 0, 1);
                double hue = ((nx + ny + geissState.ColorPhase) + plasma * 0.3) % 1.0;
                if (hue < 0)
                {
                    hue += 1.0;
                }
                char ch = PlasmaChars[Math.Min((int)(plasma * (PlasmaChars.Length - 1)), PlasmaChars.Length - 1)];

                PaletteColor color;
                if (usePalette)
                {
                    int paletteIndex = (int)(hue * ctx.Palette.Count) % ctx.Palette.Count;
                    if (paletteIndex < 0)
                    {
                        paletteIndex = (paletteIndex % ctx.Palette.Count + ctx.Palette.Count) % ctx.Palette.Count;
                    }
                    color = ctx.Palette[paletteIndex];
                }
                else
                {
                    color = PaletteColor.FromConsoleColor(GetGeissColor(hue, plasma));
                }

                ctx.Buffer.Set(x, y, ch, color);
            }
        }

        return state;
    }

    private static ConsoleColor GetGeissColor(double hue, double intensity)
    {
        if (intensity < 0.2)
        {
            return ConsoleColor.DarkBlue;
        }

        int colorIndex = (int)(hue * 12) % 12;
        return colorIndex switch
        {
            0 => ConsoleColor.Red,
            1 => ConsoleColor.DarkYellow,
            2 => ConsoleColor.Yellow,
            3 => ConsoleColor.Green,
            4 => ConsoleColor.Cyan,
            5 => ConsoleColor.Blue,
            6 => ConsoleColor.DarkBlue,
            7 => ConsoleColor.Magenta,
            8 => ConsoleColor.DarkMagenta,
            9 => ConsoleColor.Red,
            10 => ConsoleColor.DarkRed,
            _ => ConsoleColor.White
        };
    }
}
