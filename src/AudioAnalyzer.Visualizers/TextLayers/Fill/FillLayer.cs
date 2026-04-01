using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>
/// Fills the layer render region (full viewport when <see cref="TextLayerSettings.RenderBounds"/> is null)
/// with a solid or gradient color and configurable fill character
/// (full block, half blocks, shades, space, or custom ASCII). Use low ZOrder for background, high for overlay.
/// </summary>
public sealed class FillLayer : TextLayerRendererBase, ITextLayerRenderer<NoLayerState>
{
    public override TextLayerType LayerType => TextLayerType.Fill;

    public override (double Offset, int SnippetIndex) Draw(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        TextLayerDrawContext ctx)
    {
        int w = ctx.Width;
        int h = ctx.Height;
        if (w < 1 || h < 1)
        {
            return state;
        }

        var settings = layer.GetCustom<FillSettings>() ?? new FillSettings();
        char fillChar = GetFillChar(settings);
        int paletteCount = ctx.Palette.Count;
        (byte R, byte G, byte B) rgbStart;
        (byte R, byte G, byte B) rgbEnd;
        if (paletteCount <= 0)
        {
            var fallback = PaletteColorBlending.ToRgb(PaletteColor.FromConsoleColor(ConsoleColor.DarkGray));
            rgbStart = fallback;
            rgbEnd = fallback;
        }
        else
        {
            int startIdx = Math.Max(0, layer.ColorIndex % paletteCount);
            int endIdx = Math.Max(0, settings.GradientEndColorIndex % paletteCount);
            rgbStart = PaletteColorBlending.ToRgb(ctx.Palette[startIdx]);
            rgbEnd = PaletteColorBlending.ToRgb(ctx.Palette[endIdx]);
        }

        bool useGradient = settings.FillColorStyle == FillColorStyle.Gradient;
        bool blendOver = settings.FillCompositeMode == FillCompositeMode.BlendOver;
        double blendStrength = settings.BlendStrength;

        for (int ly = 0; ly < h; ly++)
        {
            for (int lx = 0; lx < w; lx++)
            {
                (byte R, byte G, byte B) fillRgb;
                if (useGradient)
                {
                    double t = ComputeGradientT(lx, ly, w, h, settings.FillGradientDirection);
                    fillRgb = PaletteColorBlending.LerpRgb(rgbStart, rgbEnd, t);
                }
                else
                {
                    fillRgb = rgbStart;
                }

                PaletteColor fillColor = PaletteColor.FromRgb(fillRgb.R, fillRgb.G, fillRgb.B);

                if (blendOver)
                {
                    if (blendStrength <= 0.0)
                    {
                        continue;
                    }

                    var (c, under) = ctx.GetLocal(lx, ly);
                    if (settings.BlendSpaceAsBlack && c == ' ')
                    {
                        under = PaletteColor.FromRgb(0, 0, 0);
                    }

                    PaletteColor outColor = blendStrength >= 1.0
                        ? fillColor
                        : PaletteColorBlending.BlendOver(under, fillColor, blendStrength);
                    ctx.SetLocal(lx, ly, fillChar, outColor);
                }
                else
                {
                    ctx.SetLocal(lx, ly, fillChar, fillColor);
                }
            }
        }

        return state;
    }

    /// <summary>Scalar in [0, 1] along the gradient for cell (x, y).</summary>
    internal static double ComputeGradientT(int x, int y, int w, int h, FillGradientDirection direction)
    {
        int maxX = Math.Max(1, w - 1);
        int maxY = Math.Max(1, h - 1);

        switch (direction)
        {
            case FillGradientDirection.LeftToRight:
                return x / (double)maxX;
            case FillGradientDirection.RightToLeft:
                return (w - 1 - x) / (double)maxX;
            case FillGradientDirection.TopToBottom:
                return y / (double)maxY;
            case FillGradientDirection.BottomToTop:
                return (h - 1 - y) / (double)maxY;
            case FillGradientDirection.TopLeftToBottomRight:
                return ProjectT(x, y, 0, 0, w - 1, h - 1);
            case FillGradientDirection.TopRightToBottomLeft:
                return ProjectT(x, y, w - 1, 0, 0, h - 1);
            case FillGradientDirection.BottomLeftToTopRight:
                return ProjectT(x, y, 0, h - 1, w - 1, 0);
            case FillGradientDirection.BottomRightToTopLeft:
                return ProjectT(x, y, w - 1, h - 1, 0, 0);
            default:
                return x / (double)maxX;
        }
    }

    /// <summary>Parameter along the segment from (ax,ay) to (bx,by), clamped to [0,1].</summary>
    private static double ProjectT(int x, int y, int ax, int ay, int bx, int by)
    {
        int dx = bx - ax;
        int dy = by - ay;
        long len2 = (long)dx * dx + (long)dy * dy;
        if (len2 <= 0)
        {
            return 0;
        }

        double num = (x - ax) * (double)dx + (y - ay) * (double)dy;
        double t = num / len2;
        return Math.Clamp(t, 0.0, 1.0);
    }

    private static char GetFillChar(FillSettings settings)
    {
        return settings.FillType switch
        {
            FillType.FullBlock => '█',
            FillType.HalfBlockUpper => '▀',
            FillType.HalfBlockLower => '▄',
            FillType.LightShade => '░',
            FillType.MediumShade => '▒',
            FillType.DarkShade => '▓',
            FillType.Space => ' ',
            FillType.Custom => GetCustomChar(settings.CustomChar),
            _ => '█'
        };
    }

    private static char GetCustomChar(string? customChar)
    {
        if (!string.IsNullOrEmpty(customChar))
        {
            return customChar[0];
        }
        return '#';
    }
}
