using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Renders spectrum bars (LlamaStyle). Supports Winamp and Spectrum Analyzer features via configurable options.</summary>
public sealed class LlamaStyleLayer : ITextLayerRenderer
{
    public TextLayerType LayerType => TextLayerType.LlamaStyle;

    public (double Offset, int SnippetIndex) Draw(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        TextLayerDrawContext ctx)
    {
        int w = ctx.Width;
        int h = ctx.Height;
        var snapshot = ctx.Snapshot;

        if (w < 30 || h < 5)
        {
            return state;
        }

        int barWidth = Math.Clamp(layer.LlamaStyleBarWidth, 2, 3);
        bool showVolumeBar = layer.LlamaStyleShowVolumeBar;
        bool showRowLabels = layer.LlamaStyleShowRowLabels;
        bool showFrequencyLabels = layer.LlamaStyleShowFrequencyLabels;
        bool spectrumColors = string.Equals(layer.LlamaStyleColorScheme, "Spectrum", StringComparison.OrdinalIgnoreCase);
        bool doubleLinePeak = string.Equals(layer.LlamaStylePeakMarkerStyle, "DoubleLine", StringComparison.OrdinalIgnoreCase);

        int fixedLines = 1 + (showVolumeBar ? 2 : 0) + (showFrequencyLabels ? 2 : 0); // separator + optional volume + optional labels
        int maxBarLines = Math.Max(1, h - fixedLines);
        int barHeight = spectrumColors || showRowLabels
            ? Math.Max(10, Math.Min(30, maxBarLines))
            : Math.Max(10, Math.Min(20, maxBarLines));

        int leftMargin = showRowLabels ? 5 : 2;
        int numBands = Math.Min(snapshot.NumBands, Math.Max(1, (w - leftMargin) / barWidth));
        double gain = snapshot.TargetMaxMagnitude > 0.0001 ? Math.Min(1000, 1.0 / snapshot.TargetMaxMagnitude) : 1000;

        int row = 0;

        void WriteString(int x, int y, string s, PaletteColor color)
        {
            for (int i = 0; i < s.Length && x + i < w; i++)
            {
                ctx.Buffer.Set(x + i, y, s[i], color);
            }
        }

        var darkGray = PaletteColor.FromConsoleColor(ConsoleColor.DarkGray);
        var white = PaletteColor.FromConsoleColor(ConsoleColor.White);

        if (showVolumeBar && row < h)
        {
            int availableWidth = Math.Max(20, w - 10);
            int volBarLength = (int)(snapshot.Volume * availableWidth);
            int x = 0;
            if (x < w) { ctx.Buffer.Set(x, row, '[', white); x++; }
            for (int i = 0; i < availableWidth && x < w; i++, x++)
            {
                if (i < volBarLength)
                {
                    var color = spectrumColors ? GetSpectrumVolumeColor((double)i / availableWidth) : GetWinampVolumeColor((double)i / availableWidth);
                    ctx.Buffer.Set(x, row, '█', color);
                }
                else
                {
                    ctx.Buffer.Set(x, row, ' ', darkGray);
                }
            }
            if (x < w) { ctx.Buffer.Set(x, row, ']', white); }
            row++;
        }
        if (showVolumeBar && row < h)
        {
            row++;
        }

        for (int barRow = barHeight; barRow >= 1 && row < h; barRow--, row++)
        {
            int x = 0;

            if (showRowLabels)
            {
                string label = barRow switch
                {
                    _ when barRow == barHeight => "100%",
                    _ when barRow == (int)(barHeight * 0.75) && barHeight >= 16 => " 75%",
                    _ when barRow == (int)(barHeight * 0.5) && barHeight >= 12 => " 50%",
                    _ when barRow == (int)(barHeight * 0.25) && barHeight >= 16 => " 25%",
                    _ when barRow == 1 => "  0%",
                    _ => "    "
                };
                WriteString(0, row, label, darkGray);
                x = 5;
            }
            else
            {
                x = 2;
            }

            for (int band = 0; band < numBands && x < w; band++)
            {
                double normalizedMag = Math.Min(snapshot.SmoothedMagnitudes[band] * gain * 0.8, 1.0);
                int height = (int)(normalizedMag * barHeight);
                double normalizedPeak = Math.Min(snapshot.PeakHold[band] * gain * 0.8, 1.0);
                int peakH = (int)(normalizedPeak * barHeight);

                string barChars = barWidth == 2 ? "██" : "██";
                string peakChars = doubleLinePeak ? "══" : "▀▀";
                string emptyChars = barWidth == 2 ? "  " : "  ";

                if (barRow == peakH && peakH > 0)
                {
                    for (int c = 0; c < (barWidth == 2 ? 2 : 2) && x < w; c++, x++)
                    {
                        ctx.Buffer.Set(x, row, peakChars[c], white);
                    }
                }
                else if (height >= barRow)
                {
                    var color = spectrumColors ? GetSpectrumBarColor(barRow, barHeight) : GetWinampBarColor(barRow, barHeight);
                    for (int c = 0; c < (barWidth == 2 ? 2 : 2) && x < w; c++, x++)
                    {
                        ctx.Buffer.Set(x, row, barChars[c], color);
                    }
                }
                else
                {
                    for (int c = 0; c < (barWidth == 2 ? 2 : 2) && x < w; c++, x++)
                    {
                        ctx.Buffer.Set(x, row, ' ', darkGray);
                    }
                }

                if (barWidth == 3 && x < w)
                {
                    ctx.Buffer.Set(x, row, ' ', darkGray);
                    x++;
                }
            }
        }

        if (row < h)
        {
            int x = showRowLabels ? 5 : 2;
            for (int band = 0; band < numBands && x < w; band++)
            {
                if (barWidth == 3)
                {
                    if (x < w) { ctx.Buffer.Set(x, row, '═', darkGray); x++; }
                    if (x < w) { ctx.Buffer.Set(x, row, '═', darkGray); x++; }
                    if (x < w) { ctx.Buffer.Set(x, row, ' ', darkGray); x++; }
                }
                else
                {
                    if (x < w) { ctx.Buffer.Set(x, row, '─', darkGray); x++; }
                    if (x < w) { ctx.Buffer.Set(x, row, '─', darkGray); x++; }
                }
            }
            row++;
        }

        if (showFrequencyLabels && row < h)
        {
            int x = showRowLabels ? 5 : 2;
            string[] allLabels = ["20", "30", "50", "80", "100", "150", "200", "300", "500", "800",
                "1k", "1.5k", "2k", "3k", "5k", "8k", "10k", "15k", "20k"];
            int maxLabels = Math.Max(4, Math.Min(allLabels.Length, numBands / 3));
            int labelInterval = Math.Max(1, numBands / maxLabels);
            int charsPerBand = barWidth == 3 ? 3 : 2;

            for (int band = 0; band < numBands && x < w; band++)
            {
                if (band % labelInterval == 0 && band / labelInterval < allLabels.Length)
                {
                    string label = allLabels[Math.Min(band / labelInterval, allLabels.Length - 1)];
                    string toShow = (label.Length >= 2 ? label[..2] : label).PadRight(Math.Min(charsPerBand, 2));
                    for (int c = 0; c < charsPerBand && x < w; c++, x++)
                    {
                        ctx.Buffer.Set(x, row, c < toShow.Length ? toShow[c] : ' ', darkGray);
                    }
                }
                else
                {
                    for (int c = 0; c < charsPerBand && x < w; c++, x++)
                    {
                        ctx.Buffer.Set(x, row, ' ', darkGray);
                    }
                }
            }
            row++;
        }

        if (showFrequencyLabels && row < h)
        {
            var footer = " Frequency (Hz)";
            WriteString(0, row, footer.Length <= w ? footer : footer[..w], darkGray);
        }
        else if (row < h)
        {
            var footer = " LlamaStyle - Classic music player visualization";
            WriteString(0, row, footer.Length <= w ? footer : footer[..w], darkGray);
        }

        return state;
    }

    private static PaletteColor GetWinampBarColor(int row, int barHeight)
    {
        double position = (double)row / barHeight;
        var cc = position switch
        {
            >= 0.85 => ConsoleColor.Red,
            >= 0.7 => ConsoleColor.DarkYellow,
            >= 0.5 => ConsoleColor.Yellow,
            >= 0.3 => ConsoleColor.Green,
            _ => ConsoleColor.DarkGreen
        };
        return PaletteColor.FromConsoleColor(cc);
    }

    private static PaletteColor GetSpectrumBarColor(int row, int barHeight)
    {
        double position = (double)row / barHeight;
        var cc = position switch
        {
            <= 0.25 => ConsoleColor.Red,
            <= 0.4 => ConsoleColor.Magenta,
            <= 0.55 => ConsoleColor.Yellow,
            <= 0.7 => ConsoleColor.Green,
            <= 0.85 => ConsoleColor.Cyan,
            _ => ConsoleColor.Blue
        };
        return PaletteColor.FromConsoleColor(cc);
    }

    private static PaletteColor GetWinampVolumeColor(double position)
    {
        var cc = position switch
        {
            >= 0.85 => ConsoleColor.Red,
            >= 0.7 => ConsoleColor.DarkYellow,
            >= 0.5 => ConsoleColor.Yellow,
            >= 0.3 => ConsoleColor.Green,
            _ => ConsoleColor.DarkGreen
        };
        return PaletteColor.FromConsoleColor(cc);
    }

    private static PaletteColor GetSpectrumVolumeColor(double position)
    {
        var cc = position switch
        {
            >= 0.85 => ConsoleColor.Red,
            >= 0.7 => ConsoleColor.Magenta,
            >= 0.55 => ConsoleColor.Yellow,
            >= 0.4 => ConsoleColor.Green,
            >= 0.25 => ConsoleColor.Cyan,
            _ => ConsoleColor.Blue
        };
        return PaletteColor.FromConsoleColor(cc);
    }
}
