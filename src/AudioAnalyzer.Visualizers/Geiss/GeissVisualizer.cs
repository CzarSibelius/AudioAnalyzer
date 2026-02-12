using System.Text;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

public sealed class GeissVisualizer : IVisualizer
{
    public string TechnicalName => "geiss";
    public string DisplayName => "Geiss";
    public bool SupportsPaletteCycling => true;

    private readonly GeissVisualizerSettings? _settings;

    public GeissVisualizer(GeissVisualizerSettings? settings)
    {
        _settings = settings;
    }

    private bool ShowBeatCircles => _settings?.BeatCircles ?? true;

    private static readonly ConsoleColor[] BeatCircleColors = [
        ConsoleColor.Cyan, ConsoleColor.Magenta, ConsoleColor.Yellow,
        ConsoleColor.Green, ConsoleColor.Red, ConsoleColor.Blue
    ];

    private double _phase;
    private double _colorPhase;
    private double _bassIntensity;
    private double _trebleIntensity;
    private readonly List<BeatCircle> _beatCircles = new();
    private int _lastBeatCount = -1;
    private readonly StringBuilder _lineBuffer = new(256);

    public void Render(AnalysisSnapshot snapshot, VisualizerViewport viewport)
    {
        if (viewport.Width < 30 || viewport.MaxLines < 3)
        {
            return;
        }

        var palette = snapshot.Palette;
        bool usePalette = palette is { Count: > 0 };

        _phase += 0.15;
        _colorPhase += 0.08;

        if (snapshot.SmoothedMagnitudes.Length > 0)
        {
            double gain = snapshot.TargetMaxMagnitude > 0.0001 ? Math.Min(1000, 1.0 / snapshot.TargetMaxMagnitude) : 1000;
            int bassEnd = Math.Max(1, snapshot.SmoothedMagnitudes.Length / 4);
            double bassSum = 0;
            for (int i = 0; i < bassEnd; i++)
            {
                bassSum += snapshot.SmoothedMagnitudes[i] * gain;
            }

            _bassIntensity = _bassIntensity * 0.7 + (bassSum / bassEnd) * 0.3;
            int trebleStart = snapshot.SmoothedMagnitudes.Length * 3 / 4;
            double trebleSum = 0;
            for (int i = trebleStart; i < snapshot.SmoothedMagnitudes.Length; i++)
            {
                trebleSum += snapshot.SmoothedMagnitudes[i] * gain;
            }

            _trebleIntensity = _trebleIntensity * 0.7 + (trebleSum / (snapshot.SmoothedMagnitudes.Length - trebleStart)) * 0.3;
        }

        if (snapshot.BeatCount != _lastBeatCount && ShowBeatCircles)
        {
            _lastBeatCount = snapshot.BeatCount;
            SpawnBeatCircle();
        }
        UpdateBeatCircles();

        int maxHeight = Math.Max(1, viewport.MaxLines - 2);
        int height = Math.Max(12, Math.Min(25, maxHeight));
        int width = Math.Min(viewport.Width - 4, 100);
        char[] plasmaChars = [' ', '·', ':', ';', '+', '*', '#', '@', '█'];

        Console.SetCursorPosition(0, viewport.StartRow);
        _lineBuffer.Clear();
        _lineBuffer.Append(' ', Math.Max(0, viewport.Width - 1));
        Console.WriteLine(_lineBuffer.ToString());

        for (int y = 0; y < height; y++)
        {
            _lineBuffer.Clear();
            _lineBuffer.Append("  ");
            for (int x = 0; x < width; x++)
            {
                double nx = (double)x / width, ny = (double)y / height;
                bool onCircle = false;
                ConsoleColor circleColor = ConsoleColor.White;
                PaletteColor? circlePaletteColor = null;
                if (ShowBeatCircles)
                {
                    double aspectRatio = 2.0;
                    double distFromCenter = Math.Sqrt((nx - 0.5) * (nx - 0.5) + ((ny - 0.5) / aspectRatio) * ((ny - 0.5) / aspectRatio));
                    foreach (var circle in _beatCircles)
                    {
                        double thickness = 0.02 + (1.0 - (double)circle.Age / 30) * 0.01;
                        if (Math.Abs(distFromCenter - circle.Radius) < thickness)
                        {
                            onCircle = true;
                            if (usePalette)
                            {
                                circlePaletteColor = palette![circle.ColorIndex % palette.Count];
                            }
                            else
                            {
                                circleColor = BeatCircleColors[circle.ColorIndex % BeatCircleColors.Length];
                            }

                            break;
                        }
                    }
                }
                if (onCircle)
                {
                    if (usePalette && circlePaletteColor.HasValue)
                    {
                        AnsiConsole.AppendColored(_lineBuffer, '○', circlePaletteColor.Value);
                    }
                    else
                    {
                        AnsiConsole.AppendColored(_lineBuffer, '○', circleColor);
                    }
                }
                else
                {
                    double v1 = Math.Sin(nx * 10 + _phase);
                    double v2 = Math.Sin(ny * 8 + _phase * 0.7);
                    double v3 = Math.Sin((nx + ny) * 6 + _phase * 1.3);
                    double v4 = Math.Sin(Math.Sqrt((nx - 0.5) * (nx - 0.5) + (ny - 0.5) * (ny - 0.5)) * 12 + _phase);
                    double plasma = (v1 + v2 + v3 + v4) / 4.0;
                    double distFromCenterPlasma = Math.Sqrt((nx - 0.5) * (nx - 0.5) + (ny - 0.5) * (ny - 0.5));
                    plasma += Math.Sin(distFromCenterPlasma * 20 - _phase * 2) * _bassIntensity * 0.5;
                    plasma += Math.Sin(nx * 30 + ny * 30 + _phase * 3) * _trebleIntensity * 0.3;
                    if (snapshot.BeatFlashActive)
                    {
                        plasma += 0.3;
                    }

                    plasma = Math.Clamp((plasma + 1.5) / 3.0, 0, 1);
                    double hue = ((nx + ny + _colorPhase) + plasma * 0.3) % 1.0;
                    char ch = plasmaChars[(int)(plasma * (plasmaChars.Length - 1))];
                    if (usePalette)
                    {
                        int paletteIndex = (int)(hue * palette!.Count) % palette.Count;
                        if (paletteIndex < 0)
                        {
                            paletteIndex = (paletteIndex % palette.Count + palette.Count) % palette.Count;
                        }

                        AnsiConsole.AppendColored(_lineBuffer, ch, palette[paletteIndex]);
                    }
                    else
                    {
                        ConsoleColor color = GetGeissColor(hue, plasma);
                        AnsiConsole.AppendColored(_lineBuffer, ch, color);
                    }
                }
            }
            int remaining = viewport.Width - width - 3;
            if (remaining > 0)
            {
                _lineBuffer.Append(' ', remaining);
            }

            Console.WriteLine(_lineBuffer.ToString());
        }

        string footer = $"  Geiss - Psychedelic | Bass: {_bassIntensity:F2} | Treble: {_trebleIntensity:F2}".PadRight(viewport.Width - 1);
        Console.WriteLine(AnsiConsole.ToAnsiString(footer, ConsoleColor.DarkGray));
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

    private void SpawnBeatCircle()
    {
        int colorIndex = Random.Shared.Next(6);
        double maxRadius = Math.Clamp(0.3 + _bassIntensity * 0.4, 0.3, 0.7);
        _beatCircles.Add(new BeatCircle(0.02, maxRadius, 0, colorIndex));
        while (_beatCircles.Count > 5)
        {
            _beatCircles.RemoveAt(0);
        }
    }

    private void UpdateBeatCircles()
    {
        for (int i = _beatCircles.Count - 1; i >= 0; i--)
        {
            var c = _beatCircles[i];
            double newRadius = c.Radius + 0.03;
            int newAge = c.Age + 1;
            if (newRadius > c.MaxRadius || newAge > 30)
            {
                _beatCircles.RemoveAt(i);
            }
            else
            {
                _beatCircles[i] = new BeatCircle(newRadius, c.MaxRadius, newAge, c.ColorIndex);
            }
        }
    }
}
