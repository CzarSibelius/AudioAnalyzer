using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>
/// Mirrors the current buffer content: one half of the screen is the source,
/// the other half is overwritten with its horizontal mirror. Place this layer
/// above the layers you want mirrored.
/// </summary>
public sealed class MirrorLayer : ITextLayerRenderer
{
    public TextLayerType LayerType => TextLayerType.Mirror;

    public (double Offset, int SnippetIndex) Draw(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        TextLayerDrawContext ctx)
    {
        int w = ctx.Width;
        int h = ctx.Height;

        if (w < 2 || h < 1)
        {
            return state;
        }

        var buffer = ctx.Buffer;
        var settings = layer.GetCustom<MirrorSettings>() ?? new MirrorSettings();

        if (settings.Direction == MirrorDirection.LeftToRight)
        {
            // Source: left half [0 .. mid-1]. Destination: right half [mid .. w-1], mirror so x -> (w-1-x).
            int mid = w / 2;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < mid; x++)
                {
                    var (c, color) = buffer.Get(x, y);
                    buffer.Set(w - 1 - x, y, c, color);
                }
            }
        }
        else
        {
            // RightToLeft: source is right half, destination is left half.
            int mid = w / 2;
            for (int y = 0; y < h; y++)
            {
                for (int x = mid; x < w; x++)
                {
                    var (c, color) = buffer.Get(x, y);
                    buffer.Set(w - 1 - x, y, c, color);
                }
            }
        }

        return state;
    }
}
