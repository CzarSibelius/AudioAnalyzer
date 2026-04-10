using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace AudioAnalyzer.Visualizers;

/// <summary>Builds <see cref="AsciiFrame"/> from in-memory BGRA pixels (same mapping as <see cref="AsciiImageConverter"/>).</summary>
public static class AsciiRasterConverter
{
    private const string AsciiGradient = " .:-=+*#%@";

    /// <summary>
    /// Resizes BGRA8888 raster to fit target character dimensions (max mode) and produces ASCII cells.
    /// </summary>
    /// <param name="bgra">BGRA8888 row-major, length at least <paramref name="srcWidth"/> * <paramref name="srcHeight"/> * 4.</param>
    /// <param name="includeRgb">When true, populates per-pixel RGB for <see cref="AsciiImagePaletteSource.ImageColors"/>.</param>
    public static AsciiFrame? FromBgra(
        ReadOnlySpan<byte> bgra,
        int srcWidth,
        int srcHeight,
        int targetWidth,
        int targetHeight,
        bool includeRgb)
    {
        if (targetWidth <= 0 || targetHeight <= 0 || srcWidth <= 0 || srcHeight <= 0)
        {
            return null;
        }

        if (bgra.Length < srcWidth * srcHeight * 4)
        {
            return null;
        }

        using var image = Image.LoadPixelData<Bgra32>(bgra, srcWidth, srcHeight);
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(targetWidth, targetHeight),
            Mode = ResizeMode.Max
        }));

        int w = image.Width;
        int h = image.Height;
        var chars = new char[w, h];
        var brightness = new byte[w, h];
        byte[,]? r = includeRgb ? new byte[w, h] : null;
        byte[,]? g = includeRgb ? new byte[w, h] : null;
        byte[,]? b = includeRgb ? new byte[w, h] : null;

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                Span<Bgra32> pixelRow = accessor.GetRowSpan(y);
                for (int x = 0; x < pixelRow.Length; x++)
                {
                    var p = pixelRow[x];
                    byte bright = (byte)((0.299 * p.R) + (0.587 * p.G) + (0.114 * p.B));
                    brightness[x, y] = bright;
                    int idx = (bright * (AsciiGradient.Length - 1)) / 255;
                    chars[x, y] = AsciiGradient[idx];
                    if (r != null && g != null && b != null)
                    {
                        r[x, y] = p.R;
                        g[x, y] = p.G;
                        b[x, y] = p.B;
                    }
                }
            }
        });

        return new AsciiFrame(chars, brightness, w, h, r, g, b);
    }
}
