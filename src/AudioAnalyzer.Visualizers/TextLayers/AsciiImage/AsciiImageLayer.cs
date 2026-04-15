using System.IO.Abstractions;
using AudioAnalyzer.Application;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Renders an ASCII art layer from images in a configured folder, with optional scroll and zoom.</summary>
public sealed class AsciiImageLayer : TextLayerRendererBase, ITextLayerRenderer<AsciiImageState>
{
    private readonly ITextLayerStateStore<AsciiImageState> _stateStore;
    private readonly UiSettings _uiSettings;
    private readonly IFileSystem _fileSystem;
    private readonly CharsetResolver _charsetResolver;

    /// <summary>Initializes a new instance of the <see cref="AsciiImageLayer"/> class.</summary>
    public AsciiImageLayer(
        ITextLayerStateStore<AsciiImageState> stateStore,
        UiSettings uiSettings,
        IFileSystem fileSystem,
        CharsetResolver charsetResolver)
    {
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _charsetResolver = charsetResolver ?? throw new ArgumentNullException(nameof(charsetResolver));
    }

    public override TextLayerType LayerType => TextLayerType.AsciiImage;

    public override (double Offset, int SnippetIndex) Draw(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        TextLayerDrawContext ctx)
    {
        int w = ctx.Width;
        int h = ctx.Height;

        var s = layer.GetCustom<AsciiImageSettings>() ?? new AsciiImageSettings();
        var imagePaths = FileBasedLayerAssetPaths.GetSortedImagePaths(s.ImageFolderPath, _uiSettings, _fileSystem);
        if (imagePaths.Count == 0)
        {
            RenderPlaceholder(ctx, "No images");
            return state;
        }

        int imageIndex = FileBasedLayerAssetPaths.ResolveIndexByFileName(imagePaths, s.SelectedImageFileName);
        string imagePath = imagePaths[imageIndex];

        var asciiState = _stateStore.GetState(ctx.LayerIndex);

        // Convert at 2x size to allow scroll and zoom room
        int convertW = w * 2;
        int convertH = h * 2;
        bool includeRgb = s.PaletteSource == AsciiImagePaletteSource.ImageColors;
        string ramp = _charsetResolver.ResolveByIdOrDefault(s.CharsetId, CharsetIds.AsciiRampClassic, " .:-=+*#%@");
        if (asciiState.CachedFrame == null
            || asciiState.CachedPath != imagePath
            || asciiState.CachedWidth != convertW
            || asciiState.CachedHeight != convertH
            || asciiState.CachedPaletteSource != s.PaletteSource
            || !string.Equals(asciiState.CachedCharsetId, ramp, StringComparison.Ordinal))
        {
            asciiState.CachedFrame = AsciiImageConverter.Convert(_fileSystem, imagePath, convertW, convertH, includeRgb, ramp);
            asciiState.CachedPath = imagePath;
            asciiState.CachedWidth = convertW;
            asciiState.CachedHeight = convertH;
            asciiState.CachedPaletteSource = s.PaletteSource;
            asciiState.CachedCharsetId = ramp;
        }

        if (asciiState.CachedFrame == null)
        {
            RenderPlaceholder(ctx, "Load failed");
            return state;
        }

        double speed = layer.SpeedMultiplier * ctx.SpeedBurst * 0.3;
        if (s.BeatReaction == AsciiImageBeatReaction.SpeedBurst && ctx.Analysis.BeatFlashActive)
        {
            speed *= 2.0;
        }

        double dtScale = DisplayAnimationTiming.ScaleForReference60(ctx.FrameDeltaSeconds);
        var movement = s.Movement;
        bool doScroll = movement is AsciiImageMovement.Scroll or AsciiImageMovement.Both;
        bool doZoom = movement is AsciiImageMovement.Zoom or AsciiImageMovement.Both;

        if (doScroll)
        {
            asciiState.ScrollX += speed * dtScale;
            asciiState.ScrollY += speed * s.ScrollRatioY * dtScale;
        }
        if (doZoom)
        {
            asciiState.ZoomPhase += speed * s.ZoomSpeed * dtScale;
            if (asciiState.ZoomPhase > 1.0)
            {
                asciiState.ZoomPhase -= 1.0;
            }
        }

        if (s.BeatReaction == AsciiImageBeatReaction.Flash && ctx.Analysis.BeatFlashActive)
        {
            var nextName = FileBasedLayerAssetPaths.NextFileNameAfter(imagePaths, s.SelectedImageFileName);
            if (nextName != null)
            {
                s.SelectedImageFileName = nextName;
                layer.SetCustom(s);
            }
        }

        double zoomPhase = asciiState.ZoomPhase;
        double scale = ComputeZoomScale(zoomPhase, s.ZoomMin, s.ZoomMax, s.ZoomStyle);
        double scrollX = asciiState.ScrollX;
        double scrollY = asciiState.ScrollY;

        var frame = asciiState.CachedFrame;
        int fw = frame.Width;
        int fh = frame.Height;

        bool useImageColors = s.PaletteSource == AsciiImagePaletteSource.ImageColors && frame.HasRgb;
        int paletteCount = ctx.Palette.Count;
        int colorBase = Math.Max(0, layer.ColorIndex % paletteCount);
        bool pulse = s.BeatReaction == AsciiImageBeatReaction.Pulse && ctx.Analysis.BeatFlashActive;
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
                    var color = useImageColors && frame.R != null && frame.G != null && frame.B != null
                        ? PaletteColor.FromRgb(frame.R[ix, iy], frame.G[ix, iy], frame.B[ix, iy])
                        : ctx.Palette[(colorBase + (frame.Brightness[ix, iy] * paletteCount) / 256) % paletteCount];
                    ctx.SetLocal(vx, vy, c, color);
                }
            }
        }

        return state;
    }

    private static double ComputeZoomScale(double phase, double zoomMin, double zoomMax, AsciiImageZoomStyle style)
    {
        double min = Math.Min(zoomMin, zoomMax);
        double max = Math.Max(zoomMin, zoomMax);
        double range = max - min;
        double t = style switch
        {
            AsciiImageZoomStyle.Sine => (1 + Math.Sin(phase * Math.PI * 2)) / 2,
            AsciiImageZoomStyle.Breathe => (1 - Math.Cos(phase * Math.PI)) / 2,
            AsciiImageZoomStyle.PingPong => phase <= 0.5 ? 2 * phase : 2 * (1 - phase),
            _ => (1 + Math.Sin(phase * Math.PI * 2)) / 2
        };
        return min + range * t;
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
                ctx.SetLocal(x, centerY, text[i], color);
            }
        }
    }
}
