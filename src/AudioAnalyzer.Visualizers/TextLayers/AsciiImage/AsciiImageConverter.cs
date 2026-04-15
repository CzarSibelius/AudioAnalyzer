using System.IO.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace AudioAnalyzer.Visualizers;

/// <summary>Converts an image to a 2D ASCII character grid with brightness values for palette mapping.</summary>
public static class AsciiImageConverter
{
    private const string DefaultAsciiGradient = " .:-=+*#%@";

    /// <summary>
    /// Converts an image file to ASCII art. Resizes to fit target dimensions while preserving aspect ratio.
    /// Returns null if the image cannot be loaded.
    /// </summary>
    /// <param name="fileSystem">File system used to open <paramref name="imagePath"/> (supports test doubles).</param>
    /// <param name="imagePath">Full path to the image file.</param>
    /// <param name="targetWidth">Target width in characters.</param>
    /// <param name="targetHeight">Target height in characters.</param>
    /// <param name="includeRgb">When true, populates per-pixel RGB for ImageColors palette source.</param>
    /// <param name="luminanceRamp">Ordered characters dark→light; when null/empty, uses the classic default ramp.</param>
    /// <returns>Character grid and brightness grid (and optionally RGB), or null on failure.</returns>
    public static AsciiFrame? Convert(
        IFileSystem fileSystem,
        string imagePath,
        int targetWidth,
        int targetHeight,
        bool includeRgb = false,
        string? luminanceRamp = null)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        if (targetWidth <= 0 || targetHeight <= 0)
        {
            return null;
        }

        string ramp = string.IsNullOrEmpty(luminanceRamp) ? DefaultAsciiGradient : luminanceRamp;
        if (ramp.Length < 1)
        {
            ramp = DefaultAsciiGradient;
        }

        try
        {
            using Stream stream = fileSystem.File.OpenRead(imagePath);
            using var image = Image.Load<Rgba32>(stream);

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
                    Span<Rgba32> pixelRow = accessor.GetRowSpan(y);
                    for (int x = 0; x < pixelRow.Length; x++)
                    {
                        var p = pixelRow[x];
                        byte bright = (byte)(0.299 * p.R + 0.587 * p.G + 0.114 * p.B);
                        brightness[x, y] = bright;
                        int idx = (bright * (ramp.Length - 1)) / 255;
                        chars[x, y] = ramp[idx];
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
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AsciiImage: failed to load {imagePath}: {ex.Message}");
            return null;
        }
    }
}
