using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Renders an ASCII art layer from images in a configured folder, with optional scroll and zoom.</summary>
public sealed class AsciiImageLayer : ITextLayerRenderer
{
    private static readonly string[] s_imageExtensions = [".bmp", ".gif", ".jpg", ".jpeg", ".png", ".webp"];

    public TextLayerType LayerType => TextLayerType.AsciiImage;

    public (double Offset, int SnippetIndex) Draw(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        TextLayerDrawContext ctx)
    {
        int w = ctx.Width;
        int h = ctx.Height;

        var s = layer.GetCustom<AsciiImageSettings>() ?? new AsciiImageSettings();
        var imagePaths = GetImagePaths(s.ImageFolderPath);
        if (imagePaths.Count == 0)
        {
            RenderPlaceholder(ctx, "No images");
            return state;
        }

        int imageIndex = state.SnippetIndex % Math.Max(1, imagePaths.Count);
        string imagePath = imagePaths[imageIndex];

        var asciiState = ctx.AsciiImageStateForLayer;

        // Convert at 2x size to allow scroll and zoom room
        int convertW = w * 2;
        int convertH = h * 2;
        if (asciiState.CachedFrame == null || asciiState.CachedPath != imagePath || asciiState.CachedWidth != convertW || asciiState.CachedHeight != convertH)
        {
            asciiState.CachedFrame = AsciiImageConverter.Convert(imagePath, convertW, convertH);
            asciiState.CachedPath = imagePath;
            asciiState.CachedWidth = convertW;
            asciiState.CachedHeight = convertH;
        }

        if (asciiState.CachedFrame == null)
        {
            RenderPlaceholder(ctx, "Load failed");
            return state;
        }

        double speed = layer.SpeedMultiplier * ctx.SpeedBurst * 0.3;
        if (layer.BeatReaction == TextLayerBeatReaction.SpeedBurst && ctx.Snapshot.BeatFlashActive)
        {
            speed *= 2.0;
        }

        var movement = s.Movement;
        bool doScroll = movement is AsciiImageMovement.Scroll or AsciiImageMovement.Both;
        bool doZoom = movement is AsciiImageMovement.Zoom or AsciiImageMovement.Both;

        if (doScroll)
        {
            asciiState.ScrollX += speed;
            asciiState.ScrollY += speed * 0.5;
        }
        if (doZoom)
        {
            asciiState.ZoomPhase += speed * 0.02;
            if (asciiState.ZoomPhase > 1.0)
            {
                asciiState.ZoomPhase -= 1.0;
            }
        }

        if (layer.BeatReaction == TextLayerBeatReaction.Flash && ctx.Snapshot.BeatFlashActive)
        {
            state.SnippetIndex = (state.SnippetIndex + 1) % Math.Max(1, imagePaths.Count);
        }

        double zoomPhase = asciiState.ZoomPhase;
        double scale = 0.85 + 0.45 * Math.Sin(zoomPhase * Math.PI * 2); // 0.85 to 1.3
        double scrollX = asciiState.ScrollX;
        double scrollY = asciiState.ScrollY;

        var frame = asciiState.CachedFrame;
        int fw = frame.Width;
        int fh = frame.Height;

        int paletteCount = ctx.Palette.Count;
        int colorBase = Math.Max(0, layer.ColorIndex % paletteCount);
        bool pulse = layer.BeatReaction == TextLayerBeatReaction.Pulse && ctx.Snapshot.BeatFlashActive;
        if (pulse)
        {
            colorBase = (colorBase + 1) % paletteCount;
        }

        for (int vy = 0; vy < h; vy++)
        {
            for (int vx = 0; vx < w; vx++)
            {
                double sx = scrollX + (vx - w / 2.0) * (1.0 / scale) + fw / 2.0;
                double sy = scrollY + (vy - h / 2.0) * (1.0 / scale) + fh / 2.0;

                int ix = (int)Math.Floor(sx) % fw;
                int iy = (int)Math.Floor(sy) % fh;
                if (ix < 0)
                {
                    ix = (ix % fw + fw) % fw;
                }
                if (iy < 0)
                {
                    iy = (iy % fh + fh) % fh;
                }

                if (ix >= 0 && ix < fw && iy >= 0 && iy < fh)
                {
                    char c = frame.Chars[ix, iy];
                    byte b = frame.Brightness[ix, iy];
                    int colorIdx = (colorBase + (b * paletteCount) / 256) % paletteCount;
                    var color = ctx.Palette[colorIdx];
                    ctx.Buffer.Set(vx, vy, c, color);
                }
            }
        }

        return state;
    }

    private static List<string> GetImagePaths(string? folderPath)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            return result;
        }

        try
        {
            foreach (var path in Directory.EnumerateFiles(folderPath))
            {
                var ext = Path.GetExtension(path).ToLowerInvariant();
                if (s_imageExtensions.Contains(ext))
                {
                    result.Add(path);
                }
            }
            result.Sort(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AsciiImage: failed to enumerate {folderPath}: {ex.Message}");
        }

        return result;
    }

    private static void RenderPlaceholder(TextLayerDrawContext ctx, string text)
    {
        int w = ctx.Width;
        int h = ctx.Height;
        int centerY = h / 2;
        int startX = Math.Max(0, (w - text.Length) / 2);
        var color = ctx.Palette[Math.Min(1, ctx.Palette.Count - 1)];
        for (int i = 0; i < text.Length; i++)
        {
            int x = startX + i;
            if (x >= 0 && x < w)
            {
                ctx.Buffer.Set(x, centerY, text[i], color);
            }
        }
    }
}
