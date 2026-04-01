using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Viewports;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>
/// Mirrors the current buffer content inside the layer's <see cref="TextLayerSettings.RenderBounds"/> (or the full viewport when null):
/// one half of that region is the source, the other half is overwritten with its mirror (horizontal or vertical).
/// <see cref="MirrorSettings.SplitPercent"/> applies within that region. Place this layer above the layers you want mirrored.
/// </summary>
public sealed class MirrorLayer : TextLayerRendererBase, ITextLayerRenderer<NoLayerState>
{
    public override TextLayerType LayerType => TextLayerType.Mirror;

    private static void FlipRegion180(ViewportCellBuffer buffer, int xMin, int yMin, int rectW, int rectH)
    {
        if (rectW < 1 || rectH < 1)
        {
            return;
        }

        int count = rectW * rectH;
        var tempC = new char[count];
        var tempColor = new PaletteColor[count];
        int i = 0;
        for (int y = 0; y < rectH; y++)
        {
            for (int x = 0; x < rectW; x++)
            {
                var (c, color) = buffer.Get(xMin + x, yMin + y);
                tempC[i] = c;
                tempColor[i] = color;
                i++;
            }
        }

        int xMax = xMin + rectW - 1;
        int yMax = yMin + rectH - 1;
        i = 0;
        for (int y = 0; y < rectH; y++)
        {
            for (int x = 0; x < rectW; x++)
            {
                buffer.Set(xMax - x, yMax - y, tempC[i], tempColor[i]);
                i++;
            }
        }
    }

    public override (double Offset, int SnippetIndex) Draw(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        TextLayerDrawContext ctx)
    {
        var buffer = ctx.Buffer;
        var settings = layer.GetCustom<MirrorSettings>() ?? new MirrorSettings();

        var (rx, ry, rw, rh) = TextLayerRenderBounds.ToPixelRect(layer.RenderBounds, ctx.ViewportWidth, ctx.ViewportHeight);

        if (settings.Direction == MirrorDirection.LeftToRight || settings.Direction == MirrorDirection.RightToLeft)
        {
            if (rw < 2)
            {
                return state;
            }

            int split = (rw * Math.Clamp(settings.SplitPercent, 1, 99)) / 100;
            int sourceSize = Math.Min(split, rw - split);
            if (sourceSize < 1)
            {
                return state;
            }

            if (settings.Direction == MirrorDirection.LeftToRight)
            {
                for (int y = ry; y < ry + rh; y++)
                {
                    for (int lx = 0; lx < sourceSize; lx++)
                    {
                        var (c, color) = buffer.Get(rx + lx, y);
                        buffer.Set(rx + rw - 1 - lx, y, c, color);
                    }
                }

                if (settings.Rotation == MirrorRotation.Flip180)
                {
                    FlipRegion180(buffer, rx + rw - sourceSize, ry, sourceSize, rh);
                }
            }
            else
            {
                for (int y = ry; y < ry + rh; y++)
                {
                    for (int lx = rw - sourceSize; lx < rw; lx++)
                    {
                        var (c, color) = buffer.Get(rx + lx, y);
                        buffer.Set(rx + rw - 1 - lx, y, c, color);
                    }
                }

                if (settings.Rotation == MirrorRotation.Flip180)
                {
                    FlipRegion180(buffer, rx, ry, sourceSize, rh);
                }
            }
        }
        else
        {
            if (rh < 2)
            {
                return state;
            }

            int split = (rh * Math.Clamp(settings.SplitPercent, 1, 99)) / 100;
            int sourceSize = Math.Min(split, rh - split);
            if (sourceSize < 1)
            {
                return state;
            }

            if (settings.Direction == MirrorDirection.TopToBottom)
            {
                for (int ly = 0; ly < sourceSize; ly++)
                {
                    for (int x = rx; x < rx + rw; x++)
                    {
                        var (c, color) = buffer.Get(x, ry + ly);
                        buffer.Set(x, ry + rh - 1 - ly, c, color);
                    }
                }

                if (settings.Rotation == MirrorRotation.Flip180)
                {
                    FlipRegion180(buffer, rx, ry + rh - sourceSize, rw, sourceSize);
                }
            }
            else
            {
                for (int y = ry + rh - sourceSize; y < ry + rh; y++)
                {
                    for (int x = rx; x < rx + rw; x++)
                    {
                        var (c, color) = buffer.Get(x, y);
                        buffer.Set(x, ry + rh - 1 - (y - ry), c, color);
                    }
                }

                if (settings.Rotation == MirrorRotation.Flip180)
                {
                    FlipRegion180(buffer, rx, ry, rw, sourceSize);
                }
            }
        }

        return state;
    }
}
