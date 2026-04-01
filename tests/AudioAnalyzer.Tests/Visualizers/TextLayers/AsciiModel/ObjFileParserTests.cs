using AudioAnalyzer.Visualizers;
using Xunit;

namespace AudioAnalyzer.Tests.Visualizers.TextLayers.AsciiModel;

/// <summary>Tests for <see cref="ObjFileParser"/>.</summary>
public sealed class ObjFileParserTests
{
    [Fact]
    public void Parse_MinimalTriangle_ReturnsMeshWithOneTriangle()
    {
        var obj = string.Join("\n", "v 0 0 0", "v 1 0 0", "v 0 1 0", "f 1 2 3") + "\n";

        var mesh = ObjFileParser.Parse(obj);
        Assert.NotNull(mesh);
        Assert.Equal(3, mesh!.Vertices.Length);
        Assert.Equal(3, mesh.Indices.Length);
        Assert.Equal(1, mesh.TriangleCount);
        Assert.Single(mesh.FaceNormals);
        Assert.Equal(mesh.Vertices.Length, mesh.VertexNormals.Length);
    }

    [Fact]
    public void Parse_Quad_SplitsIntoTwoTriangles()
    {
        const string obj = "v 0 0 0\nv 1 0 0\nv 1 1 0\nv 0 1 0\nf 1 2 3 4\n";

        var mesh = ObjFileParser.Parse(obj);
        Assert.NotNull(mesh);
        Assert.Equal(2, mesh!.TriangleCount);
    }

    [Fact]
    public void Parse_FaceWithSlash_IgnoresTextureAndNormalIndices()
    {
        const string obj = "v 0 0 0\nv 1 0 0\nv 0 1 0\nf 1/1/1 2/2/2 3/3/3\n";

        var mesh = ObjFileParser.Parse(obj);
        Assert.NotNull(mesh);
        Assert.Equal(1, mesh!.TriangleCount);
    }

    [Fact]
    public void Parse_Empty_ReturnsNull()
    {
        Assert.Null(ObjFileParser.Parse(""));
        Assert.Null(ObjFileParser.Parse("   "));
    }
}
