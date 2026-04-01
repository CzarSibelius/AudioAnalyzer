using System.Numerics;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Renders Wavefront OBJ meshes as shaded ASCII art with rotation and optional zoom.</summary>
public sealed class AsciiModelLayer : TextLayerRendererBase, ITextLayerRenderer<AsciiModelState>
{
    private readonly ITextLayerStateStore<AsciiModelState> _stateStore;
    private readonly UiSettings _uiSettings;

    /// <summary>Initializes a new instance of the <see cref="AsciiModelLayer"/> class.</summary>
    public AsciiModelLayer(ITextLayerStateStore<AsciiModelState> stateStore, UiSettings uiSettings)
    {
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
    }

    /// <inheritdoc />
    public override TextLayerType LayerType => TextLayerType.AsciiModel;

    /// <inheritdoc />
    public override (double Offset, int SnippetIndex) Draw(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        TextLayerDrawContext ctx)
    {
        int w = ctx.Width;
        int h = ctx.Height;

        var s = layer.GetCustom<AsciiModelSettings>() ?? new AsciiModelSettings();
        var paths = FileBasedLayerAssetPaths.GetSortedObjPaths(s.ModelFolderPath, _uiSettings);
        if (paths.Count == 0)
        {
            RenderPlaceholder(ctx, "No models");
            return state;
        }

        int fileIndex = FileBasedLayerAssetPaths.ResolveIndexByFileName(paths, s.SelectedModelFileName);
        string path = paths[fileIndex];

        var m = _stateStore.GetState(ctx.LayerIndex);
        if (!TryRefreshMesh(path, s, m))
        {
            RenderPlaceholder(ctx, "Load failed");
            return state;
        }

        if (m.CachedMesh == null)
        {
            RenderPlaceholder(ctx, "Load failed");
            return state;
        }

        if (m.CachedMesh.TriangleCount > s.MaxTriangles)
        {
            RenderPlaceholder(ctx, "Too many tris");
            return state;
        }

        double speed = layer.SpeedMultiplier * ctx.SpeedBurst * 0.3;
        if (s.BeatReaction == AsciiModelBeatReaction.SpeedBurst && ctx.Snapshot.BeatFlashActive)
        {
            speed *= 2.0;
        }

        double dir = s.RotationDirection == AsciiModelRotationDirection.CounterClockwise ? 1.0 : -1.0;
        m.RotationAngle += speed * s.RotationSpeed * dir;

        if (s.EnableZoom)
        {
            m.ZoomPhase += speed * s.ZoomSpeed;
            if (m.ZoomPhase > 1.0)
            {
                m.ZoomPhase -= 1.0;
            }
        }

        if (s.BeatReaction == AsciiModelBeatReaction.Flash && ctx.Snapshot.BeatFlashActive)
        {
            var nextName = FileBasedLayerAssetPaths.NextFileNameAfter(paths, s.SelectedModelFileName);
            if (nextName != null)
            {
                s.SelectedModelFileName = nextName;
                layer.SetCustom(s);
            }
        }

        float angle = (float)m.RotationAngle;
        Matrix4x4 rot = s.RotationAxis == AsciiModelRotationAxis.Y
            ? Matrix4x4.CreateRotationY(angle)
            : Matrix4x4.CreateFromYawPitchRoll(angle * 0.95f, angle * 0.55f, angle * 0.35f);

        double zoomScale = s.EnableZoom
            ? ComputeZoomScale(m.ZoomPhase, s.ZoomMin, s.ZoomMax, s.ZoomStyle)
            : 1.0;

        int paletteCount = ctx.Palette.Count;
        int colorBase = Math.Max(0, layer.ColorIndex % paletteCount);

        var lightDir = AsciiModelLighting.GetLightDirection(
            s.LightingPreset,
            s.LightAzimuthDegrees,
            s.LightElevationDegrees);

        AsciiModelRasterizer.Render(
            ctx.Buffer,
            ctx.Palette,
            colorBase,
            w,
            h,
            m.CachedMesh,
            rot,
            zoomScale,
            s.RenderMode,
            (float)s.ShapeContrastExponent,
            lightDir,
            (float)s.Ambient,
            ctx.BufferOriginX,
            ctx.BufferOriginY);

        return state;
    }

    private static bool TryRefreshMesh(string path, AsciiModelSettings settings, AsciiModelState m)
    {
        try
        {
            var info = new FileInfo(path);
            if (!info.Exists)
            {
                return false;
            }

            long len = info.Length;
            DateTime lw = info.LastWriteTimeUtc;
            if (m.CachedMesh != null
                && string.Equals(m.CachedPath, path, StringComparison.OrdinalIgnoreCase)
                && m.CachedFileLength == len
                && m.CachedLastWriteUtc == lw)
            {
                return true;
            }

            var mesh = ObjFileParser.ParseFile(path);
            if (mesh == null)
            {
                return false;
            }

            m.CachedMesh = mesh;
            m.CachedPath = path;
            m.CachedFileLength = len;
            m.CachedLastWriteUtc = lw;
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AsciiModel: refresh mesh failed: {ex.Message}");
            return false;
        }
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
