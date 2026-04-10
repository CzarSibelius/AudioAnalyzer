using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Renders live video as ASCII using <see cref="IAsciiVideoFrameSource"/>.</summary>
public sealed class AsciiVideoLayer : TextLayerRendererBase, ITextLayerRenderer<AsciiVideoState>
{
    private const int NoFrameHintAfterMs = 8000;

    private readonly IAsciiVideoFrameSource _frameSource;
    private readonly ITextLayerStateStore<AsciiVideoState> _stateStore;

    /// <summary>Initializes a new instance of the <see cref="AsciiVideoLayer"/> class.</summary>
    public AsciiVideoLayer(IAsciiVideoFrameSource frameSource, ITextLayerStateStore<AsciiVideoState> stateStore)
    {
        _frameSource = frameSource ?? throw new ArgumentNullException(nameof(frameSource));
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
    }

    /// <inheritdoc />
    public override TextLayerType LayerType => TextLayerType.AsciiVideo;

    /// <inheritdoc />
    public override (double Offset, int SnippetIndex) Draw(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        TextLayerDrawContext ctx)
    {
        int w = ctx.Width;
        int h = ctx.Height;

        var s = layer.GetCustom<AsciiVideoSettings>() ?? new AsciiVideoSettings();

        if (s.SourceKind == AsciiVideoSourceKind.File)
        {
            RenderPlaceholder(ctx, "File source N/A");
            return state;
        }

        if (s.SourceKind != AsciiVideoSourceKind.Webcam)
        {
            RenderPlaceholder(ctx, "No source");
            return state;
        }

        var videoState = _stateStore.GetState(ctx.LayerIndex);

        if (!_frameSource.TryGetLatestFrame(out AsciiVideoFrameSnapshot? snap) || snap == null)
        {
            RenderAsciiVideoPlaceholder(ctx, videoState);
            return state;
        }

        videoState.WaitingForFirstFrameSinceTicks = 0;
        bool includeRgb = s.PaletteSource == AsciiImagePaletteSource.ImageColors;
        int convertW = w * 2;
        int convertH = h * 2;

        if (videoState.CachedFrame == null
            || videoState.CachedSequence != snap.Sequence
            || videoState.CachedConvertWidth != convertW
            || videoState.CachedConvertHeight != convertH
            || videoState.CachedPaletteSource != s.PaletteSource)
        {
            videoState.CachedFrame = AsciiRasterConverter.FromBgra(
                snap.BgraPixels,
                snap.Width,
                snap.Height,
                convertW,
                convertH,
                includeRgb);
            videoState.CachedSequence = snap.Sequence;
            videoState.CachedConvertWidth = convertW;
            videoState.CachedConvertHeight = convertH;
            videoState.CachedPaletteSource = s.PaletteSource;
        }

        if (videoState.CachedFrame == null)
        {
            RenderPlaceholder(ctx, "Convert failed");
            return state;
        }

        var frame = videoState.CachedFrame;
        int fw = frame.Width;
        int fh = frame.Height;
        bool useImageColors = s.PaletteSource == AsciiImagePaletteSource.ImageColors && frame.HasRgb;
        int paletteCount = ctx.Palette.Count;
        int colorBase = Math.Max(0, layer.ColorIndex % paletteCount);

        for (int vy = 0; vy < h; vy++)
        {
            for (int vx = 0; vx < w; vx++)
            {
                double u = (vx + 0.5) / w;
                if (s.FlipHorizontal)
                {
                    u = 1.0 - u;
                }

                double v = (vy + 0.5) / h;
                int ix = (int)(u * fw);
                int iy = (int)(v * fh);
                ix = Math.Clamp(ix, 0, fw - 1);
                iy = Math.Clamp(iy, 0, fh - 1);

                char c = frame.Chars[ix, iy];
                var color = useImageColors && frame.R != null && frame.G != null && frame.B != null
                    ? PaletteColor.FromRgb(frame.R[ix, iy], frame.G[ix, iy], frame.B[ix, iy])
                    : ctx.Palette[(colorBase + (frame.Brightness[ix, iy] * paletteCount) / 256) % paletteCount];
                ctx.SetLocal(vx, vy, c, color);
            }
        }

        return state;
    }

    private void RenderAsciiVideoPlaceholder(TextLayerDrawContext ctx, AsciiVideoState videoState)
    {
        if (!_frameSource.IsWebcamStarting && !_frameSource.IsWebcamSessionActive)
        {
            videoState.WaitingForFirstFrameSinceTicks = 0;
            RenderTwoLinePlaceholder(ctx, "No camera", null);
            return;
        }

        if (_frameSource.IsWebcamStarting)
        {
            videoState.WaitingForFirstFrameSinceTicks = 0;
            RenderTwoLinePlaceholder(ctx, "Opening camera", null);
            return;
        }

        long now = Environment.TickCount64;
        if (videoState.WaitingForFirstFrameSinceTicks == 0)
        {
            videoState.WaitingForFirstFrameSinceTicks = now;
        }

        long elapsedMs = now - videoState.WaitingForFirstFrameSinceTicks;
        if (elapsedMs < 0)
        {
            videoState.WaitingForFirstFrameSinceTicks = now;
            elapsedMs = 0;
        }

        if (elapsedMs >= NoFrameHintAfterMs)
        {
            RenderTwoLinePlaceholder(ctx, "No video signal", "Check Camera privacy (Settings)");
        }
        else
        {
            RenderTwoLinePlaceholder(ctx, "Waiting for video", null);
        }
    }

    private static void RenderTwoLinePlaceholder(TextLayerDrawContext ctx, string line1, string? line2)
    {
        int cw = ctx.Width;
        int ch = ctx.Height;
        var color = ctx.Palette[Math.Min(1, ctx.Palette.Count - 1)];

        static string FitWidth(string s, int width)
        {
            if (s.Length <= width)
            {
                return s;
            }

            if (width <= 3)
            {
                return s[..width];
            }

            return string.Concat(s.AsSpan(0, width - 3), "...");
        }

        int lineCount = line2 != null ? 2 : 1;
        int startY = Math.Max(0, (ch - lineCount) / 2);

        void DrawRow(string text, int rowY)
        {
            string t = FitWidth(text, cw);
            int startX = Math.Max(0, (cw - t.Length) / 2);
            for (int i = 0; i < t.Length; i++)
            {
                int x = startX + i;
                if (x >= 0 && x < cw)
                {
                    ctx.SetLocal(x, rowY, t[i], color);
                }
            }
        }

        DrawRow(line1, startY);
        if (line2 != null)
        {
            DrawRow(line2, startY + 1);
        }
    }

    private static void RenderPlaceholder(TextLayerDrawContext ctx, string text)
    {
        int cw = ctx.Width;
        int ch = ctx.Height;
        int centerY = ch / 2;
        int startX = Math.Max(0, (cw - text.Length) / 2);
        var color = ctx.Palette[Math.Min(1, ctx.Palette.Count - 1)];
        for (int i = 0; i < text.Length; i++)
        {
            int x = startX + i;
            if (x >= 0 && x < cw)
            {
                ctx.SetLocal(x, centerY, text[i], color);
            }
        }
    }
}
