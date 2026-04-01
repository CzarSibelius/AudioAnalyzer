using System.Numerics;
using AudioAnalyzer.Application.Viewports;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Projects a triangle mesh to the cell buffer with z-buffering and Lambert shading to ASCII characters.</summary>
public static class AsciiModelRasterizer
{
    /// <summary>ASCII gradient from dark to light (legacy mode; matches <see cref="AsciiImageConverter"/>).</summary>
    private const string AsciiGradient = " .:-=+*#%@";

    /// <summary>
    /// Terminal cells are taller than wide; multiply projected X by this so models do not look vertically stretched.
    /// </summary>
    private const float CellAspectX = 2f;

    /// <summary>
    /// Renders the mesh into the buffer. Legacy mode writes only cells covered by projected triangles.
    /// Shape mode writes only cells where at least one subsample hit geometry; other cells are unchanged for compositing with lower-Z layers.
    /// </summary>
    public static void Render(
        ViewportCellBuffer buffer,
        IReadOnlyList<PaletteColor> palette,
        int colorBase,
        int width,
        int height,
        TriangleMesh mesh,
        Matrix4x4 rotation,
        double cameraDistanceScale,
        AsciiModelRenderMode renderMode,
        float shapeContrastExponent,
        Vector3 lightDirectionWorld,
        float ambient,
        int bufferOriginX = 0,
        int bufferOriginY = 0)
    {
        if (width <= 0 || height <= 0 || palette.Count == 0)
        {
            return;
        }

        Vector3 lightDir = lightDirectionWorld;
        if (lightDir.LengthSquared() > 1e-12f)
        {
            lightDir = Vector3.Normalize(lightDir);
        }
        else
        {
            lightDir = new Vector3(0f, 0f, 1f);
        }

        float ambientClamped = Math.Clamp(ambient, 0f, 1f);

        if (renderMode == AsciiModelRenderMode.LegacyGradient)
        {
            RenderLegacy(buffer, palette, colorBase, width, height, mesh, rotation, cameraDistanceScale, lightDir, ambientClamped, bufferOriginX, bufferOriginY);
        }
        else
        {
            RenderShape(buffer, palette, colorBase, width, height, mesh, rotation, cameraDistanceScale, shapeContrastExponent, lightDir, ambientClamped, bufferOriginX, bufferOriginY);
        }
    }

    private static void RenderLegacy(
        ViewportCellBuffer buffer,
        IReadOnlyList<PaletteColor> palette,
        int colorBase,
        int width,
        int height,
        TriangleMesh mesh,
        Matrix4x4 rotation,
        double cameraDistanceScale,
        Vector3 lightDir,
        float ambient,
        int bufferOriginX,
        int bufferOriginY)
    {
        float zOffset = (float)(2.0 / Math.Max(0.25, cameraDistanceScale));
        float focal = Math.Min(width, height) * 0.42f * (float)cameraDistanceScale;
        var zBuf = new float[width * height];
        Array.Fill(zBuf, float.PositiveInfinity);

        int triCount = mesh.TriangleCount;
        var verts = mesh.Vertices;
        var idx = mesh.Indices;
        var faceN = mesh.FaceNormals;

        for (int t = 0; t < triCount; t++)
        {
            int i0 = idx[t * 3];
            int i1 = idx[t * 3 + 1];
            int i2 = idx[t * 3 + 2];

            var p0 = Project(verts[i0], rotation, zOffset, focal, width, height);
            var p1 = Project(verts[i1], rotation, zOffset, focal, width, height);
            var p2 = Project(verts[i2], rotation, zOffset, focal, width, height);

            if (!p0.Valid || !p1.Valid || !p2.Valid)
            {
                continue;
            }

            var nw = Vector3.TransformNormal(faceN[t], rotation);
            if (nw.LengthSquared() > 1e-12f)
            {
                nw = Vector3.Normalize(nw);
            }

            float nd = Math.Clamp(Vector3.Dot(nw, lightDir), 0f, 1f);
            float intensity = AsciiModelLighting.CombineDiffuseAndAmbient(nd, ambient);
            byte bright = (byte)(intensity * 255);
            int g = (bright * (AsciiGradient.Length - 1)) / 255;
            char asciiChar = AsciiGradient[g];

            FillTriangleCells(
                buffer,
                palette,
                colorBase,
                width,
                height,
                zBuf,
                p0,
                p1,
                p2,
                asciiChar,
                bright,
                bufferOriginX,
                bufferOriginY);
        }
    }

    private static void RenderShape(
        ViewportCellBuffer buffer,
        IReadOnlyList<PaletteColor> palette,
        int colorBase,
        int width,
        int height,
        TriangleMesh mesh,
        Matrix4x4 rotation,
        double cameraDistanceScale,
        float shapeContrastExponent,
        Vector3 lightDir,
        float ambient,
        int bufferOriginX,
        int bufferOriginY)
    {
        float zOffset = (float)(2.0 / Math.Max(0.25, cameraDistanceScale));
        float focal = Math.Min(width, height) * 0.42f * (float)cameraDistanceScale;
        int cellCount = width * height;
        int sampleCount = AsciiCellSampling.SampleCount;
        var zBuf = new float[cellCount * sampleCount];
        var lumBuf = new float[cellCount * sampleCount];
        Array.Fill(zBuf, float.PositiveInfinity);

        int triCount = mesh.TriangleCount;
        var verts = mesh.Vertices;
        var idx = mesh.Indices;
        var faceN = mesh.FaceNormals;
        var vn = mesh.VertexNormals;

        ReadOnlySpan<float> ox = AsciiCellSampling.NormalizedX;
        ReadOnlySpan<float> oy = AsciiCellSampling.NormalizedY;

        for (int t = 0; t < triCount; t++)
        {
            int i0 = idx[t * 3];
            int i1 = idx[t * 3 + 1];
            int i2 = idx[t * 3 + 2];

            var p0 = Project(verts[i0], rotation, zOffset, focal, width, height);
            var p1 = Project(verts[i1], rotation, zOffset, focal, width, height);
            var p2 = Project(verts[i2], rotation, zOffset, focal, width, height);

            if (!p0.Valid || !p1.Valid || !p2.Valid)
            {
                continue;
            }

            float minX = Math.Min(Math.Min(p0.X, p1.X), p2.X);
            float maxX = Math.Max(Math.Max(p0.X, p1.X), p2.X);
            float minY = Math.Min(Math.Min(p0.Y, p1.Y), p2.Y);
            float maxY = Math.Max(Math.Max(p0.Y, p1.Y), p2.Y);

            int x0 = Math.Max(0, (int)Math.Floor(minX));
            int x1 = Math.Min(width - 1, (int)Math.Ceiling(maxX));
            int y0 = Math.Max(0, (int)Math.Floor(minY));
            int y1 = Math.Min(height - 1, (int)Math.Ceiling(maxY));

            var a = new Vector2(p0.X, p0.Y);
            var b = new Vector2(p1.X, p1.Y);
            var c2 = new Vector2(p2.X, p2.Y);

            var n0 = vn[i0];
            var n1 = vn[i1];
            var n2 = vn[i2];

            for (int py = y0; py <= y1; py++)
            {
                for (int px = x0; px <= x1; px++)
                {
                    int cellBase = (py * width + px) * sampleCount;
                    for (int k = 0; k < sampleCount; k++)
                    {
                        var p = new Vector2(px + ox[k], py + oy[k]);
                        if (!PointInTriangle2D(p, a, b, c2))
                        {
                            continue;
                        }

                        float w0, w1, w2;
                        Barycentric(p, a, b, c2, out w0, out w1, out w2);
                        float z = w0 * p0.Z + w1 * p1.Z + w2 * p2.Z;
                        int zi = cellBase + k;
                        if (z >= zBuf[zi])
                        {
                            continue;
                        }

                        zBuf[zi] = z;
                        var nInterp = w0 * n0 + w1 * n1 + w2 * n2;
                        if (nInterp.LengthSquared() > 1e-12f)
                        {
                            nInterp = Vector3.Normalize(nInterp);
                        }
                        else
                        {
                            nInterp = faceN[t];
                        }

                        var nwInterp = Vector3.TransformNormal(nInterp, rotation);
                        if (nwInterp.LengthSquared() > 1e-12f)
                        {
                            nwInterp = Vector3.Normalize(nwInterp);
                        }

                        float nd = Math.Clamp(Vector3.Dot(nwInterp, lightDir), 0f, 1f);
                        lumBuf[zi] = AsciiModelLighting.CombineDiffuseAndAmbient(nd, ambient);
                    }
                }
            }
        }

        Span<float> sample = stackalloc float[6];
        for (int py = 0; py < height; py++)
        {
            for (int px = 0; px < width; px++)
            {
                int cellBase = (py * width + px) * sampleCount;
                bool anyHit = false;
                for (int k = 0; k < sampleCount; k++)
                {
                    if (zBuf[cellBase + k] < float.PositiveInfinity)
                    {
                        anyHit = true;
                        break;
                    }
                }

                if (!anyHit)
                {
                    continue;
                }

                float maxLum = 0f;
                for (int k = 0; k < sampleCount; k++)
                {
                    float l = lumBuf[cellBase + k];
                    sample[k] = l;
                    if (l > maxLum)
                    {
                        maxLum = l;
                    }
                }

                AsciiShapeTable.ApplyGlobalContrast(sample, shapeContrastExponent);
                char ch = AsciiShapeTable.FindBestCharacter(sample);
                byte bright = (byte)(maxLum * 255);
                int pal = (colorBase + (bright * palette.Count) / 256) % palette.Count;
                if (pal < 0)
                {
                    pal += palette.Count;
                }

                buffer.Set(bufferOriginX + px, bufferOriginY + py, ch, palette[pal]);
            }
        }
    }

    private static void FillTriangleCells(
        ViewportCellBuffer buffer,
        IReadOnlyList<PaletteColor> palette,
        int colorBase,
        int width,
        int height,
        float[] zBuf,
        ProjPoint p0,
        ProjPoint p1,
        ProjPoint p2,
        char asciiChar,
        byte bright,
        int bufferOriginX,
        int bufferOriginY)
    {
        float minX = Math.Min(Math.Min(p0.X, p1.X), p2.X);
        float maxX = Math.Max(Math.Max(p0.X, p1.X), p2.X);
        float minY = Math.Min(Math.Min(p0.Y, p1.Y), p2.Y);
        float maxY = Math.Max(Math.Max(p0.Y, p1.Y), p2.Y);

        int x0 = Math.Max(0, (int)Math.Floor(minX));
        int x1 = Math.Min(width - 1, (int)Math.Ceiling(maxX));
        int y0 = Math.Max(0, (int)Math.Floor(minY));
        int y1 = Math.Min(height - 1, (int)Math.Ceiling(maxY));

        var a = new Vector2(p0.X, p0.Y);
        var b = new Vector2(p1.X, p1.Y);
        var c2 = new Vector2(p2.X, p2.Y);

        for (int py = y0; py <= y1; py++)
        {
            for (int px = x0; px <= x1; px++)
            {
                var p = new Vector2(px + 0.5f, py + 0.5f);
                if (!PointInTriangle2D(p, a, b, c2))
                {
                    continue;
                }

                float w0, w1, w2;
                Barycentric(p, a, b, c2, out w0, out w1, out w2);
                float z = w0 * p0.Z + w1 * p1.Z + w2 * p2.Z;
                int zi = py * width + px;
                if (z < zBuf[zi])
                {
                    zBuf[zi] = z;
                    int pal = (colorBase + (bright * palette.Count) / 256) % palette.Count;
                    if (pal < 0)
                    {
                        pal += palette.Count;
                    }

                    buffer.Set(bufferOriginX + px, bufferOriginY + py, asciiChar, palette[pal]);
                }
            }
        }
    }

    private readonly struct ProjPoint
    {
        public ProjPoint(float x, float y, float z, bool valid)
        {
            X = x;
            Y = y;
            Z = z;
            Valid = valid;
        }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public bool Valid { get; }
    }

    private static ProjPoint Project(Vector3 v, Matrix4x4 rotation, float zOffset, float focal, int w, int h)
    {
        var p = Vector3.Transform(v, rotation);
        p.Z += zOffset;
        if (p.Z < 0.08f)
        {
            return new ProjPoint(0, 0, 0, false);
        }

        float invZ = 1f / p.Z;
        float sx = w * 0.5f + focal * p.X * invZ * CellAspectX;
        float sy = h * 0.5f - focal * p.Y * invZ;
        return new ProjPoint(sx, sy, p.Z, true);
    }

    private static bool PointInTriangle2D(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float s1 = Cross2(b - a, p - a);
        float s2 = Cross2(c - b, p - b);
        float s3 = Cross2(a - c, p - c);
        bool hasNeg = s1 < 0 || s2 < 0 || s3 < 0;
        bool hasPos = s1 > 0 || s2 > 0 || s3 > 0;
        return !(hasNeg && hasPos);
    }

    private static float Cross2(Vector2 u, Vector2 v) => u.X * v.Y - u.Y * v.X;

    private static void Barycentric(Vector2 p, Vector2 a, Vector2 b, Vector2 c, out float u, out float v, out float w)
    {
        Vector2 v0 = b - a;
        Vector2 v1 = c - a;
        Vector2 v2 = p - a;
        float d00 = Vector2.Dot(v0, v0);
        float d01 = Vector2.Dot(v0, v1);
        float d11 = Vector2.Dot(v1, v1);
        float d20 = Vector2.Dot(v2, v0);
        float d21 = Vector2.Dot(v2, v1);
        float denom = d00 * d11 - d01 * d01;
        if (Math.Abs(denom) < 1e-12f)
        {
            u = v = w = 1f / 3f;
            return;
        }

        v = (d11 * d20 - d01 * d21) / denom;
        w = (d00 * d21 - d01 * d20) / denom;
        u = 1f - v - w;
    }
}
