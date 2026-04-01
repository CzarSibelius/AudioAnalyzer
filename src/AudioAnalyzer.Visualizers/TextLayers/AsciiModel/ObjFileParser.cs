using System.Globalization;
using System.Numerics;

namespace AudioAnalyzer.Visualizers;

/// <summary>Parses a subset of Wavefront OBJ (vertices and polygon faces) into a <see cref="TriangleMesh"/>.</summary>
public static class ObjFileParser
{
    /// <summary>Parses OBJ text into a centered, scaled triangle mesh, or null if invalid/empty.</summary>
    public static TriangleMesh? Parse(string objText)
    {
        if (string.IsNullOrWhiteSpace(objText))
        {
            return null;
        }

        var positions = new List<Vector3>();
        var rawFaces = new List<int[]>();

        using var reader = new StringReader(objText);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            if (IsVertexPositionLine(line))
            {
                ReadOnlySpan<char> rest = line.AsSpan(1).TrimStart();
                if (TryParseVertex(rest.ToString(), out var v))
                {
                    positions.Add(v);
                }
            }
            else if (IsFaceLine(line))
            {
                ReadOnlySpan<char> faceRest = line.AsSpan(1).TrimStart();
                var faceCorners = ParseFaceVertexIndices(faceRest.ToString());
                if (faceCorners.Count >= 3)
                {
                    TriangulateFace(faceCorners, rawFaces);
                }
            }
        }

        if (positions.Count < 3 || rawFaces.Count == 0)
        {
            return null;
        }

        var indices = new List<int>(rawFaces.Count * 3);
        foreach (var face in rawFaces)
        {
            foreach (var idx in face)
            {
                int zero = idx < 0 ? positions.Count + idx : idx - 1;
                if ((uint)zero >= (uint)positions.Count)
                {
                    return null;
                }

                indices.Add(zero);
            }
        }

        var verts = positions.ToArray();
        var idxArray = indices.ToArray();
        NormalizeToUnitBox(verts);
        var faceNormals = ComputeFaceNormals(verts, idxArray);
        var vertexNormals = ComputeVertexNormals(verts, idxArray);
        return new TriangleMesh(verts, idxArray, faceNormals, vertexNormals);
    }

    /// <summary>True for a face line (<c>f</c> + whitespace + corner indices).</summary>
    private static bool IsFaceLine(string line)
    {
        return line.Length >= 3 && line[0] == 'f' && char.IsWhiteSpace(line[1]);
    }

    /// <summary>True for a vertex position line (<c>v</c> + whitespace + coords), excluding <c>vn</c>, <c>vt</c>, <c>vp</c>.</summary>
    private static bool IsVertexPositionLine(string line)
    {
        if (line.Length < 3 || line[0] != 'v')
        {
            return false;
        }

        return char.IsWhiteSpace(line[1]);
    }

    /// <summary>Parses OBJ from a file path.</summary>
    public static TriangleMesh? ParseFile(string filePath)
    {
        try
        {
            return Parse(File.ReadAllText(filePath));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AsciiModel: failed to load OBJ {filePath}: {ex.Message}");
            return null;
        }
    }

    private static bool TryParseVertex(string s, out Vector3 v)
    {
        v = default;
        var parts = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
        {
            return false;
        }

        if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x))
        {
            return false;
        }

        if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
        {
            return false;
        }

        if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
        {
            return false;
        }

        v = new Vector3(x, y, z);
        return true;
    }

    private static List<int> ParseFaceVertexIndices(string s)
    {
        var list = new List<int>();
        var parts = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            int slash = part.IndexOf('/');
            ReadOnlySpan<char> num = slash >= 0 ? part.AsSpan(0, slash) : part.AsSpan();
            if (int.TryParse(num, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idx))
            {
                list.Add(idx);
            }
        }

        return list;
    }

    private static void TriangulateFace(List<int> verts, List<int[]> rawFaces)
    {
        if (verts.Count == 3)
        {
            rawFaces.Add([verts[0], verts[1], verts[2]]);
        }
        else if (verts.Count == 4)
        {
            rawFaces.Add([verts[0], verts[1], verts[2]]);
            rawFaces.Add([verts[0], verts[2], verts[3]]);
        }
        else
        {
            for (int i = 1; i < verts.Count - 1; i++)
            {
                rawFaces.Add([verts[0], verts[i], verts[i + 1]]);
            }
        }
    }

    private static Vector3[] ComputeFaceNormals(Vector3[] vertices, int[] indices)
    {
        int triCount = indices.Length / 3;
        var normals = new Vector3[triCount];
        for (int t = 0; t < triCount; t++)
        {
            int i0 = indices[t * 3];
            int i1 = indices[t * 3 + 1];
            int i2 = indices[t * 3 + 2];
            var e1 = vertices[i1] - vertices[i0];
            var e2 = vertices[i2] - vertices[i0];
            var n = Vector3.Cross(e1, e2);
            if (n.LengthSquared() > 1e-20f)
            {
                n = Vector3.Normalize(n);
            }
            else
            {
                n = new Vector3(0, 0, 1);
            }

            normals[t] = n;
        }

        return normals;
    }

    /// <summary>Angle-weighted vertex normals (Mikkelsen-style accumulation per corner angle).</summary>
    private static Vector3[] ComputeVertexNormals(Vector3[] vertices, int[] indices)
    {
        var normals = new Vector3[vertices.Length];
        int triCount = indices.Length / 3;
        for (int t = 0; t < triCount; t++)
        {
            int i0 = indices[t * 3];
            int i1 = indices[t * 3 + 1];
            int i2 = indices[t * 3 + 2];
            var p0 = vertices[i0];
            var p1 = vertices[i1];
            var p2 = vertices[i2];
            var e1 = p1 - p0;
            var e2 = p2 - p0;
            var faceN = Vector3.Cross(e1, e2);
            float lenSq = faceN.LengthSquared();
            if (lenSq < 1e-20f)
            {
                continue;
            }

            faceN /= MathF.Sqrt(lenSq);

            float AngleAt(int ia, int ib, int ic)
            {
                var u = vertices[ib] - vertices[ia];
                var v = vertices[ic] - vertices[ia];
                float lu = u.Length();
                float lv = v.Length();
                if (lu < 1e-12f || lv < 1e-12f)
                {
                    return 0f;
                }

                u /= lu;
                v /= lv;
                float cos = Math.Clamp(Vector3.Dot(u, v), -1f, 1f);
                return MathF.Acos(cos);
            }

            float a0 = AngleAt(i0, i1, i2);
            float a1 = AngleAt(i1, i2, i0);
            float a2 = AngleAt(i2, i0, i1);
            normals[i0] += faceN * a0;
            normals[i1] += faceN * a1;
            normals[i2] += faceN * a2;
        }

        for (int i = 0; i < normals.Length; i++)
        {
            if (normals[i].LengthSquared() > 1e-20f)
            {
                normals[i] = Vector3.Normalize(normals[i]);
            }
            else
            {
                normals[i] = new Vector3(0f, 0f, 1f);
            }
        }

        return normals;
    }

    private static void NormalizeToUnitBox(Vector3[] vertices)
    {
        if (vertices.Length == 0)
        {
            return;
        }

        var min = vertices[0];
        var max = vertices[0];
        foreach (var v in vertices)
        {
            min = Vector3.Min(min, v);
            max = Vector3.Max(max, v);
        }

        var center = (min + max) * 0.5f;
        var ext = max - min;
        float m = Math.Max(Math.Max(ext.X, ext.Y), ext.Z);
        if (m < 1e-6f)
        {
            m = 1f;
        }

        float s = 0.9f / m;
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = (vertices[i] - center) * s;
        }
    }
}
