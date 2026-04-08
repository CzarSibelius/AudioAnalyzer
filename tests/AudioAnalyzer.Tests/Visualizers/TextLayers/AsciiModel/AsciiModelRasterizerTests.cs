using System.Numerics;
using AudioAnalyzer.Application.Viewports;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;
using Xunit;

namespace AudioAnalyzer.Tests.Visualizers.TextLayers.AsciiModel;

/// <summary>Tests for <see cref="AsciiModelRasterizer"/>.</summary>
public sealed class AsciiModelRasterizerTests
{
    [Fact]
    public void Render_ShapeMode_LeavesCellsWithoutMeshCoverageUnchanged()
    {
        const string obj = """
v -1 -1 0
v 1 -1 0
v 0 1 0
f 1 2 3
""";
        var mesh = ObjFileParser.Parse(obj);
        Assert.NotNull(mesh);

        var buffer = new ViewportCellBuffer();
        const int w = 120;
        const int h = 80;
        buffer.EnsureSize(w, h);
        var marker = PaletteColor.FromConsoleColor(ConsoleColor.Green);
        buffer.Set(0, 0, 'X', marker);
        buffer.Set(w - 1, h - 1, 'Y', marker);

        var palette = new[]
        {
            PaletteColor.FromConsoleColor(ConsoleColor.White),
            PaletteColor.FromConsoleColor(ConsoleColor.Gray)
        };

        AsciiModelRasterizer.Render(
            buffer,
            palette,
            colorBase: 0,
            width: w,
            height: h,
            mesh,
            Matrix4x4.Identity,
            cameraDistanceScale: 1.0,
            AsciiModelRenderMode.Shape,
            shapeContrastExponent: 1f,
            AsciiModelLighting.GetLightDirection(AsciiModelLightingPreset.Classic, 0, 0),
            ambient: 0f);

        Assert.Equal(('X', marker), buffer.Get(0, 0));
        Assert.Equal(('Y', marker), buffer.Get(w - 1, h - 1));
    }

    [Fact]
    public void Render_LegacyGradient_RendersTriangleWhoseNormalIsOrthogonalToViewAtCentroid()
    {
        // Triangle in the XY plane (z = 0): face normal +Z; centroid has z = 0, so toEye lies in the XY plane and
        // dot(normal, toEye) == 0 (grazing). Older logic culled dot <= 1e-5 and dropped the whole face.
        // Vertices chosen so the 2D projection is a non-degenerate triangle (not a screen-space line).
        var verts = new Vector3[]
        {
            new(0.5f, 0f, 0f),
            new(1.5f, 0f, 0f),
            new(1f, 1f, 0f),
        };
        var indices = new[] { 0, 1, 2 };
        var faceNormals = new[] { Vector3.UnitZ };
        var vertexNormals = new[] { Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ };
        var mesh = new TriangleMesh(verts, indices, faceNormals, vertexNormals);

        var buffer = new ViewportCellBuffer();
        const int w = 80;
        const int h = 60;
        buffer.EnsureSize(w, h);
        var bg = PaletteColor.FromConsoleColor(ConsoleColor.Black);
        buffer.Clear(bg);

        var palette = new[]
        {
            PaletteColor.FromConsoleColor(ConsoleColor.White),
            PaletteColor.FromConsoleColor(ConsoleColor.Gray)
        };

        AsciiModelRasterizer.Render(
            buffer,
            palette,
            colorBase: 0,
            width: w,
            height: h,
            mesh,
            Matrix4x4.Identity,
            cameraDistanceScale: 1.0,
            AsciiModelRenderMode.LegacyGradient,
            shapeContrastExponent: 1f,
            AsciiModelLighting.GetLightDirection(AsciiModelLightingPreset.Classic, 0, 0),
            ambient: 1f);

        // Legacy ramp maps low intensity to space (' '); full ambient forces a non-space glyph so we can detect writes.
        bool anyDrawn = false;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (buffer.Get(x, y).C != ' ')
                {
                    anyDrawn = true;
                    break;
                }
            }

            if (anyDrawn)
            {
                break;
            }
        }

        Assert.True(anyDrawn, "Expected at least one rasterized cell for a grazing (dot==0) face.");
    }
}
