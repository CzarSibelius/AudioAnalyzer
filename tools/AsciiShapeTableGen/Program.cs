using System.Globalization;
using System.Text;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace AsciiShapeTableGen;

/// <summary>Regenerates embedded 6D shape vectors for <see cref="AudioAnalyzer.Visualizers.AsciiShapeTable"/> (run manually; requires a monospace TTF).</summary>
internal static class Program
{
    private const int BitmapW = 24;
    private const int BitmapH = 48;

    /// <summary>Staggered sampling circles in normalized cell space (must match AsciiCellSampling).</summary>
    private static readonly float[] s_cx = [0.22f, 0.78f, 0.18f, 0.82f, 0.22f, 0.78f];

    private static readonly float[] s_cy = [0.15f, 0.15f, 0.42f, 0.42f, 0.72f, 0.72f];

    private const float CircleRadiusNorm = 0.11f;

    private const string Charset =
        " .'`^\",:;!?-+=*#%@/\\|()[]{}<>~_0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public static int Main()
    {
        string? fontPath = FindMonospaceFontPath();
        if (fontPath == null)
        {
            Console.Error.WriteLine("No monospace TTF found (Consolas, Cascadia Mono, DejaVu Sans Mono).");
            return 1;
        }

        var fonts = new FontCollection();
        FontFamily family = fonts.Add(fontPath, CultureInfo.InvariantCulture);
        Font font = family.CreateFont(BitmapH * 0.55f);

        var raw = new float[Charset.Length, 6];
        for (int i = 0; i < Charset.Length; i++)
        {
            char c = Charset[i];
            using var img = RenderGlyph(font, c);
            for (int k = 0; k < 6; k++)
            {
                raw[i, k] = SampleCircleInk(img, s_cx[k], s_cy[k], CircleRadiusNorm);
            }
        }

        // Per-dimension max across characters (Harri normalization).
        var maxDim = new float[6];
        for (int k = 0; k < 6; k++)
        {
            float m = 0f;
            for (int i = 0; i < Charset.Length; i++)
            {
                m = Math.Max(m, raw[i, k]);
            }

            maxDim[k] = m < 1e-6f ? 1f : m;
        }

        var norm = new float[Charset.Length * 6];
        for (int i = 0; i < Charset.Length; i++)
        {
            for (int k = 0; k < 6; k++)
            {
                norm[i * 6 + k] = raw[i, k] / maxDim[k];
            }
        }

        Console.Write(GenerateCSharp(norm));
        return 0;
    }

    private static string? FindMonospaceFontPath()
    {
        string[] candidates =
        [
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "consola.ttf"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "CascadiaMono.ttf"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "lucon.ttf"),
            "/usr/share/fonts/truetype/dejavu/DejaVuSansMono.ttf",
            "/usr/share/fonts/truetype/liberation/LiberationMono-Regular.ttf"
        ];

        foreach (var p in candidates)
        {
            if (File.Exists(p))
            {
                return p;
            }
        }

        return null;
    }

    private static Image<Rgba32> RenderGlyph(Font font, char c)
    {
        var img = new Image<Rgba32>(BitmapW, BitmapH, Color.Black);
        string s = c == '"' ? "\u201c" : new string(c, 1);
        var textOptions = new RichTextOptions(font)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Origin = new SixLabors.ImageSharp.PointF(BitmapW / 2f, BitmapH / 2f)
        };

        img.Mutate(ctx => ctx.DrawText(textOptions, s, Color.White));
        return img;
    }

    private static float SampleCircleInk(Image<Rgba32> img, float nx, float ny, float radiusNorm)
    {
        float cx = nx * BitmapW;
        float cy = ny * BitmapH;
        float r = radiusNorm * Math.Min(BitmapW, BitmapH);
        float rSq = r * r;
        int count = 0;
        int ink = 0;
        int x0 = Math.Max(0, (int)Math.Floor(cx - r));
        int x1 = Math.Min(BitmapW - 1, (int)Math.Ceiling(cx + r));
        int y0 = Math.Max(0, (int)Math.Floor(cy - r));
        int y1 = Math.Min(BitmapH - 1, (int)Math.Ceiling(cy + r));
        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                float dx = x + 0.5f - cx;
                float dy = y + 0.5f - cy;
                if (dx * dx + dy * dy > rSq)
                {
                    continue;
                }

                count++;
                Rgba32 p = img[x, y];
                float lum = (p.R + p.G + p.B) / (3f * 255f);
                if (lum > 0.15f)
                {
                    ink++;
                }
            }
        }

        return count == 0 ? 0f : ink / (float)count;
    }

    private static string GenerateCSharp(float[] normalizedFlat)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("// Regenerate: dotnet run --project tools/AsciiShapeTableGen");
        sb.AppendLine("namespace AudioAnalyzer.Visualizers;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>Precomputed 6D shape vectors (Harri-style) for nearest-character matching.</summary>");
        sb.AppendLine("internal static partial class AsciiShapeTable");
        sb.AppendLine("{");
        sb.Append("    internal const string ShapeCharset = \"");
        foreach (char c in Charset)
        {
            switch (c)
            {
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '"':
                    sb.Append("\\\"");
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }

        sb.AppendLine("\";");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>Normalized shape rows: <c>ShapeCharset.Length</c> by 6, row-major.</summary>");
        sb.AppendLine("    internal static ReadOnlySpan<float> NormalizedShapeRows => s_normalizedShapeRows;");
        sb.AppendLine();
        sb.AppendLine("    private static readonly float[] s_normalizedShapeRows =");
        sb.AppendLine("    [");
        for (int i = 0; i < Charset.Length; i++)
        {
            sb.Append("        ");
            for (int k = 0; k < 6; k++)
            {
                float v = normalizedFlat[i * 6 + k];
                sb.Append(v.ToString("G9", CultureInfo.InvariantCulture));
                sb.Append('f');
                if (k < 5)
                {
                    sb.Append(", ");
                }
            }

            sb.AppendLine(i < Charset.Length - 1 ? "," : string.Empty);
        }

        sb.AppendLine("    ];");
        sb.AppendLine("}");
        return sb.ToString();
    }
}
