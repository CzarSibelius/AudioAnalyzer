using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>
/// Mirrors the current buffer content: one half of the screen is the source,
/// the other half is overwritten with its mirror (horizontal or vertical).
/// Place this layer above the layers you want mirrored.
/// </summary>
public sealed class MirrorLayer : ITextLayerRenderer
{
    public TextLayerType LayerType => TextLayerType.Mirror;

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

    public (double Offset, int SnippetIndex) Draw(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        TextLayerDrawContext ctx)
    {
        int w = ctx.Width;
        int h = ctx.Height;

        var buffer = ctx.Buffer;
        var settings = layer.GetCustom<MirrorSettings>() ?? new MirrorSettings();

        if (settings.Direction == MirrorDirection.LeftToRight || settings.Direction == MirrorDirection.RightToLeft)
        {
            if (w < 2)
            {
                return state;
            }

            int split = (w * Math.Clamp(settings.SplitPercent, 1, 99)) / 100;
            int sourceSize = Math.Min(split, w - split);
            if (sourceSize < 1)
            {
                return state;
            }

            if (settings.Direction == MirrorDirection.LeftToRight)
            {
                // Source: left [0 .. sourceSize-1]. Destination: right [w-sourceSize .. w-1], mirror x -> (w-1-x).
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < sourceSize; x++)
                    {
                        var (c, color) = buffer.Get(x, y);
                        buffer.Set(w - 1 - x, y, c, color);
                    }
                }
                if (settings.Rotation == MirrorRotation.Flip180)
                {
                    FlipRegion180(buffer, w - sourceSize, 0, sourceSize, h);
                }
            }
            else
            {
                // RightToLeft: source is right [w-sourceSize .. w-1], destination is left [0 .. sourceSize-1].
                for (int y = 0; y < h; y++)
                {
                    for (int x = w - sourceSize; x < w; x++)
                    {
                        var (c, color) = buffer.Get(x, y);
                        buffer.Set(w - 1 - x, y, c, color);
                    }
                }
                if (settings.Rotation == MirrorRotation.Flip180)
                {
                    FlipRegion180(buffer, 0, 0, sourceSize, h);
                }
            }
        }
        else
        {
            // TopToBottom or BottomToTop: vertical mirror
            if (h < 2)
            {
                return state;
            }

            int split = (h * Math.Clamp(settings.SplitPercent, 1, 99)) / 100;
            int sourceSize = Math.Min(split, h - split);
            if (sourceSize < 1)
            {
                return state;
            }

            if (settings.Direction == MirrorDirection.TopToBottom)
            {
                // Source: top [0 .. sourceSize-1]. Destination: bottom [h-sourceSize .. h-1], mirror y -> (h-1-y).
                for (int y = 0; y < sourceSize; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        var (c, color) = buffer.Get(x, y);
                        buffer.Set(x, h - 1 - y, c, color);
                    }
                }
                if (settings.Rotation == MirrorRotation.Flip180)
                {
                    FlipRegion180(buffer, 0, h - sourceSize, w, sourceSize);
                }
            }
            else
            {
                // BottomToTop: source is bottom [h-sourceSize .. h-1], destination is top [0 .. sourceSize-1].
                for (int y = h - sourceSize; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        var (c, color) = buffer.Get(x, y);
                        buffer.Set(x, h - 1 - y, c, color);
                    }
                }
                if (settings.Rotation == MirrorRotation.Flip180)
                {
                    FlipRegion180(buffer, 0, 0, w, sourceSize);
                }
            }
        }

        return state;
    }
}
