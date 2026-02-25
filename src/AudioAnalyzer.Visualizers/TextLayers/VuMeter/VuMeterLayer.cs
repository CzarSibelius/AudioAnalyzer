using System.Globalization;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Viewports;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Renders classic stereo VU-style meters with channel levels, peak hold, dB scale, and balance.</summary>
public sealed class VuMeterLayer : TextLayerRendererBase, ITextLayerRenderer<NoLayerState>
{
    public override TextLayerType LayerType => TextLayerType.VuMeter;

    public override (double Offset, int SnippetIndex) Draw(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        TextLayerDrawContext ctx)
    {
        int w = ctx.Width;
        int h = ctx.Height;
        var snapshot = ctx.Snapshot;

        if (w < 30 || h < 7)
        {
            return state;
        }

        int meterWidth = Math.Min(60, w - 20);
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

        if (row < h) { row++; } // blank
        if (row < h) { row++; } // blank

        if (row < h)
        {
            DrawVuMeterChannel(ctx.Buffer, 0, row, w, "  L ", snapshot.LeftChannel, snapshot.LeftPeakHold, meterWidth);
            row++;
        }
        if (row < h) { row++; } // blank
        if (row < h)
        {
            DrawVuMeterChannel(ctx.Buffer, 0, row, w, "  R ", snapshot.RightChannel, snapshot.RightPeakHold, meterWidth);
            row++;
        }
        if (row < h) { row++; } // blank
        if (row < h) { row++; } // blank

        if (row < h)
        {
            var scale = "    ";
            for (int i = 0; i <= 10; i++)
            {
                scale += (i * 10).ToString(CultureInfo.InvariantCulture).PadRight(Math.Max(1, meterWidth / 10));
            }
            WriteString(0, row, scale.Length <= w ? scale : scale[..w], darkGray);
            row++;
        }

        if (row < h)
        {
            string[] dbLabels = ["-∞", "-40", "-30", "-20", "-10", "-6", "-3", "0"];
            int labelSpacing = Math.Max(1, meterWidth / (dbLabels.Length - 1));
            var dbLine = "    ";
            for (int i = 0; i < dbLabels.Length; i++)
            {
                dbLine += dbLabels[i].PadRight(labelSpacing);
            }
            WriteString(0, row, dbLine.Length <= w ? dbLine : dbLine[..w], darkGray);
            row++;
        }
        if (row < h) { row++; } // blank

        if (row < h)
        {
            float balance = (snapshot.RightChannel - snapshot.LeftChannel) / Math.Max(0.001f, snapshot.LeftChannel + snapshot.RightChannel);
            int balancePos = (int)((balance + 1) / 2 * meterWidth);
            balancePos = Math.Clamp(balancePos, 0, meterWidth - 1);
            WriteString(0, row, "  BAL ", white);
            int x = 6;
            for (int i = 0; i < balancePos && x < w; i++, x++)
            {
                ctx.Buffer.Set(x, row, '\u2500', darkGray);
            }
            if (x < w)
            {
                ctx.Buffer.Set(x, row, '\u25CF', white);
                x++;
            }
            for (int i = balancePos + 1; i < meterWidth && x < w; i++, x++)
            {
                ctx.Buffer.Set(x, row, '\u2500', darkGray);
            }
            row++;
        }

        if (row < h)
        {
            var lcr = "      L" + new string(' ', Math.Max(0, meterWidth / 2 - 2)) + "C" + new string(' ', Math.Max(0, meterWidth / 2 - 2)) + "R";
            WriteString(0, row, lcr.Length <= w ? lcr : lcr[..w], darkGray);
            row++;
        }

        if (row < h)
        {
            var footer = "  Classic VU Meter - Shows channel levels".PadRight(w);
            WriteString(0, row, footer.Length <= w ? footer : footer[..w], darkGray);
        }

        return state;
    }

    private static void DrawVuMeterChannel(ViewportCellBuffer buffer, int startX, int y, int maxWidth, string label, float level, float peakHold, int width)
    {
        int x = startX;
        var darkGray = PaletteColor.FromConsoleColor(ConsoleColor.DarkGray);
        var white = PaletteColor.FromConsoleColor(ConsoleColor.White);

        foreach (char c in label)
        {
            if (x < maxWidth) { buffer.Set(x, y, c, white); x++; }
        }
        if (x < maxWidth) { buffer.Set(x, y, '[', white); x++; }

        int barLength = (int)(level * width);
        int peakPos = (int)(peakHold * width);

        for (int i = 0; i < width && x < maxWidth; i++, x++)
        {
            if (i == peakPos && peakPos > 0)
            {
                buffer.Set(x, y, '│', white);
            }
            else if (i < barLength)
            {
                var color = GetVuColor((double)i / width);
                buffer.Set(x, y, '█', color);
            }
            else
            {
                buffer.Set(x, y, '░', darkGray);
            }
        }

        if (x < maxWidth) { buffer.Set(x, y, ']', white); x++; }
        if (x < maxWidth) { x++; } // space

        double db = 20 * Math.Log10(Math.Max(level, 0.00001));
        var dbStr = $" {db:F1} dB";
        foreach (char c in dbStr)
        {
            if (x < maxWidth) { buffer.Set(x, y, c, white); x++; }
        }
    }

    private static PaletteColor GetVuColor(double position) =>
        PaletteColor.FromConsoleColor(position switch { >= 0.9 => ConsoleColor.Red, >= 0.75 => ConsoleColor.Yellow, _ => ConsoleColor.Green });
}
