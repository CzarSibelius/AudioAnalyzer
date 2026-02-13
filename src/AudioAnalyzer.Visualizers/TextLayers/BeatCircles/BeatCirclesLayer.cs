using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Renders expanding beat circles over the layer below.</summary>
public sealed class BeatCirclesLayer : ITextLayerRenderer
{
    private static readonly ConsoleColor[] BeatCircleColors =
    [
        ConsoleColor.Cyan, ConsoleColor.Magenta, ConsoleColor.Yellow,
        ConsoleColor.Green, ConsoleColor.Red, ConsoleColor.Blue
    ];

    public TextLayerType LayerType => TextLayerType.BeatCircles;

    public (double Offset, int SnippetIndex) Draw(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        TextLayerDrawContext ctx)
    {
        int w = ctx.Width;
        int h = ctx.Height;
        var snapshot = ctx.Snapshot;
        var beatState = ctx.BeatCirclesStateForLayer;

        double bassIntensity = beatState.BassIntensity;
        if (snapshot.SmoothedMagnitudes.Length > 0)
        {
            double gain = snapshot.TargetMaxMagnitude > 0.0001 ? Math.Min(1000, 1.0 / snapshot.TargetMaxMagnitude) : 1000;
            int bassEnd = Math.Max(1, snapshot.SmoothedMagnitudes.Length / 4);
            double bassSum = 0;
            for (int i = 0; i < bassEnd; i++)
            {
                bassSum += snapshot.SmoothedMagnitudes[i] * gain;
            }
            bassIntensity = bassIntensity * 0.7 + (bassSum / bassEnd) * 0.3;
            beatState.BassIntensity = bassIntensity;
        }

        if (snapshot.BeatCount != beatState.LastBeatCount)
        {
            beatState.LastBeatCount = snapshot.BeatCount;
            SpawnBeatCircle(beatState.Circles, bassIntensity);
        }
        UpdateBeatCircles(beatState.Circles);

        bool usePalette = ctx.Palette.Count > 0;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                double nx = (double)x / w;
                double ny = (double)y / h;

                double aspectRatio = 2.0;
                double distFromCenter = Math.Sqrt((nx - 0.5) * (nx - 0.5) + ((ny - 0.5) / aspectRatio) * ((ny - 0.5) / aspectRatio));

                foreach (var circle in beatState.Circles)
                {
                    double thickness = 0.02 + (1.0 - (double)circle.Age / 30) * 0.01;
                    if (Math.Abs(distFromCenter - circle.Radius) < thickness)
                    {
                        PaletteColor color;
                        if (usePalette)
                        {
                            color = ctx.Palette[circle.ColorIndex % ctx.Palette.Count];
                        }
                        else
                        {
                            color = PaletteColor.FromConsoleColor(BeatCircleColors[circle.ColorIndex % BeatCircleColors.Length]);
                        }
                        ctx.Buffer.Set(x, y, '○', color);
                        break;
                    }
                }
            }
        }

        return state;
    }

    private static void SpawnBeatCircle(List<BeatCircle> circles, double bassIntensity)
    {
        int colorIndex = Random.Shared.Next(6);
        double maxRadius = Math.Clamp(0.3 + bassIntensity * 0.4, 0.3, 0.7);
        circles.Add(new BeatCircle(0.02, maxRadius, 0, colorIndex));
        while (circles.Count > 5)
        {
            circles.RemoveAt(0);
        }
    }

    private static void UpdateBeatCircles(List<BeatCircle> circles)
    {
        for (int i = circles.Count - 1; i >= 0; i--)
        {
            var c = circles[i];
            double newRadius = c.Radius + 0.03;
            int newAge = c.Age + 1;
            if (newRadius > c.MaxRadius || newAge > 30)
            {
                circles.RemoveAt(i);
            }
            else
            {
                circles[i] = new BeatCircle(newRadius, c.MaxRadius, newAge, c.ColorIndex);
            }
        }
    }
}
