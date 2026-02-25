using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>
/// Fills the entire viewport with a single color and configurable fill character
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
        var color = paletteCount > 0
            ? ctx.Palette[Math.Max(0, layer.ColorIndex % paletteCount)]
            : PaletteColor.FromConsoleColor(ConsoleColor.DarkGray);

        var buffer = ctx.Buffer;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                buffer.Set(x, y, fillChar, color);
            }
        }

        return state;
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
