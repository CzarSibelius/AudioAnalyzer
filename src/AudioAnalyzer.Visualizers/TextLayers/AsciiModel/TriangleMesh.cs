using System.Numerics;

namespace AudioAnalyzer.Visualizers;

/// <summary>Triangle mesh for ASCII rasterization: indexed vertices, vertex normals, and one normal per triangle.</summary>
public sealed class TriangleMesh
{
    /// <summary>Creates a mesh. <paramref name="indices"/> length must be a multiple of 3.</summary>
    public TriangleMesh(Vector3[] vertices, int[] indices, Vector3[] faceNormals, Vector3[] vertexNormals)
    {
        Vertices = vertices;
        Indices = indices;
        FaceNormals = faceNormals;
        VertexNormals = vertexNormals;
        TriangleCount = indices.Length / 3;
    }

    /// <summary>Vertex positions (model space, centered and scaled).</summary>
    public Vector3[] Vertices { get; }

    /// <summary>Triangle corner indices (3 per triangle).</summary>
    public int[] Indices { get; }

    /// <summary>Unit face normals in model space (one per triangle).</summary>
    public Vector3[] FaceNormals { get; }

    /// <summary>Unit vertex normals in model space (angle-weighted from adjacent faces).</summary>
    public Vector3[] VertexNormals { get; }

    /// <summary>Number of triangles.</summary>
    public int TriangleCount { get; }
}
