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
}
