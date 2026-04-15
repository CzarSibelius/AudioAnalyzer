using System.Numerics;
using AudioAnalyzer.Application.Viewports;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Projects a triangle mesh to the cell buffer with z-buffering and Lambert shading to ASCII characters.</summary>
public static class AsciiModelRasterizer
{
    private const string DefaultAsciiGradient = " .:-=+*#%@";

    /// <summary>Terminal cells are taller than wide; multiply projected X by this so models are not stretched vertically in the terminal.</summary>
    private const float CellAspectX = 2f;

    /// <summary>
    /// Renders the mesh into the buffer. Legacy mode writes only cells covered by projected triangles.
    /// Shape mode writes only cells where at least one subsample hit geometry; other cells are unchanged for compositing with lower-Z layers.
    /// </summary>
    /// <param name="buffer">Target cell buffer.</param>
    /// <param name="palette">Color palette for shaded output.</param>
    /// <param name="colorBase">Base palette index for shading.</param>
    /// <param name="width">Layer width in cells.</param>
    /// <param name="height">Layer height in cells.</param>
    /// <param name="mesh">Triangle mesh in model space.</param>
    /// <param name="rotation">Model rotation applied before projection.</param>
    /// <param name="cameraDistanceScale">Scale factor for camera distance / focal length.</param>
    /// <param name="renderMode">Shape (subsampled) or legacy gradient.</param>
    /// <param name="shapeContrastExponent">Shape-mode global contrast exponent.</param>
    /// <param name="lightDirectionWorld">Direction to light in world space (normalized internally when non-zero).</param>
    /// <param name="ambient">Ambient term in [0,1].</param>
    /// <param name="bufferOriginX">Optional buffer X offset.</param>
    /// <param name="bufferOriginY">Optional buffer Y offset.</param>
    /// <param name="luminanceRampChars">Legacy gradient ramp; when null/empty, uses the classic default.</param>
    /// <param name="rasterScratch">When non-null, reuses z/luminance and world-space buffers from per-layer state to avoid per-frame allocations.</param>
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
        int bufferOriginY = 0,
        string? luminanceRampChars = null,
        AsciiModelState? rasterScratch = null)
    {
        if (width <= 0 || height <= 0 || palette.Count == 0)
        {
            return;
        }

        string ramp = string.IsNullOrEmpty(luminanceRampChars) ? DefaultAsciiGradient : luminanceRampChars;
        if (ramp.Length < 1)
        {
            ramp = DefaultAsciiGradient;
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
            RenderLegacy(buffer, palette, colorBase, width, height, mesh, rotation, cameraDistanceScale, lightDir, ambientClamped, bufferOriginX, bufferOriginY, ramp, rasterScratch);
        }
        else
        {
            RenderShape(buffer, palette, colorBase, width, height, mesh, rotation, cameraDistanceScale, shapeContrastExponent, lightDir, ambientClamped, bufferOriginX, bufferOriginY, rasterScratch);
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
        int bufferOriginY,
        string ramp,
        AsciiModelState? rasterScratch)
    {
        float zOffset = (float)(2.0 / Math.Max(0.25, cameraDistanceScale));
        float focal = Math.Min(width, height) * 0.42f * (float)cameraDistanceScale;
        int zLen = width * height;
        float[] zBuf = AcquireLegacyZBuffer(zLen, rasterScratch);
        Array.Fill(zBuf, float.PositiveInfinity, 0, zLen);

        EnsureWorldSpaceTransformed(mesh, rotation, rasterScratch, out Vector3[] worldPos, out _);

        int triCount = mesh.TriangleCount;
        var idx = mesh.Indices;
        var faceN = mesh.FaceNormals;

        for (int t = 0; t < triCount; t++)
        {
            int i0 = idx[t * 3];
            int i1 = idx[t * 3 + 1];
            int i2 = idx[t * 3 + 2];

            var c0 = worldPos[i0];
            var c1 = worldPos[i1];
            var c2 = worldPos[i2];
            var nwRaw = Vector3.TransformNormal(faceN[t], rotation);
            if (ShouldCullBackFace(c0, c1, c2, nwRaw))
            {
                continue;
            }

            var nw = nwRaw;
            if (nw.LengthSquared() > 1e-12f)
            {
                nw = Vector3.Normalize(nw);
            }

            var p0 = ProjectTransformed(c0, zOffset, focal, width, height);
            var p1 = ProjectTransformed(c1, zOffset, focal, width, height);
            var p2 = ProjectTransformed(c2, zOffset, focal, width, height);

            if (!p0.Valid || !p1.Valid || !p2.Valid)
            {
                continue;
            }

            float nd = Math.Clamp(Vector3.Dot(nw, lightDir), 0f, 1f);
            float intensity = AsciiModelLighting.CombineDiffuseAndAmbient(nd, ambient);
            byte bright = (byte)(intensity * 255);
            int g = (bright * (ramp.Length - 1)) / 255;
            char asciiChar = ramp[g];

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
        int bufferOriginY,
        AsciiModelState? rasterScratch)
    {
        float zOffset = (float)(2.0 / Math.Max(0.25, cameraDistanceScale));
        float focal = Math.Min(width, height) * 0.42f * (float)cameraDistanceScale;
        int cellCount = width * height;
        int sampleCount = AsciiCellSampling.SampleCount;
        int subsLen = cellCount * sampleCount;
        var (zBuf, lumBuf) = AcquireShapeBuffers(subsLen, rasterScratch);
        Array.Fill(zBuf, float.PositiveInfinity, 0, subsLen);
        Array.Fill(lumBuf, 0f, 0, subsLen);

        EnsureWorldSpaceTransformed(mesh, rotation, rasterScratch, out Vector3[] worldPos, out Vector3[] worldNormals);

        int triCount = mesh.TriangleCount;
        var idx = mesh.Indices;
        var faceN = mesh.FaceNormals;

        ReadOnlySpan<float> ox = AsciiCellSampling.NormalizedX;
        ReadOnlySpan<float> oy = AsciiCellSampling.NormalizedY;

        int passX0 = width;
        int passX1 = -1;
        int passY0 = height;
        int passY1 = -1;

        for (int t = 0; t < triCount; t++)
        {
            int i0 = idx[t * 3];
            int i1 = idx[t * 3 + 1];
            int i2 = idx[t * 3 + 2];

            var v0w = worldPos[i0];
            var v1w = worldPos[i1];
            var v2w = worldPos[i2];
            var nwRaw = Vector3.TransformNormal(faceN[t], rotation);
            if (ShouldCullBackFace(v0w, v1w, v2w, nwRaw))
            {
                continue;
            }

            var nw = nwRaw;
            if (nw.LengthSquared() > 1e-12f)
            {
                nw = Vector3.Normalize(nw);
            }

            var p0 = ProjectTransformed(v0w, zOffset, focal, width, height);
            var p1 = ProjectTransformed(v1w, zOffset, focal, width, height);
            var p2 = ProjectTransformed(v2w, zOffset, focal, width, height);

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

            passX0 = Math.Min(passX0, x0);
            passX1 = Math.Max(passX1, x1);
            passY0 = Math.Min(passY0, y0);
            passY1 = Math.Max(passY1, y1);

            var a = new Vector2(p0.X, p0.Y);
            var b = new Vector2(p1.X, p1.Y);
            var c2 = new Vector2(p2.X, p2.Y);

            var wn0 = worldNormals[i0];
            var wn1 = worldNormals[i1];
            var wn2 = worldNormals[i2];

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
                        var nInterp = w0 * wn0 + w1 * wn1 + w2 * wn2;
                        Vector3 nwInterp;
                        if (nInterp.LengthSquared() > 1e-12f)
                        {
                            nwInterp = Vector3.Normalize(nInterp);
                        }
                        else
                        {
                            nwInterp = nw;
                        }

                        float nd = Math.Clamp(Vector3.Dot(nwInterp, lightDir), 0f, 1f);
                        lumBuf[zi] = AsciiModelLighting.CombineDiffuseAndAmbient(nd, ambient);
                    }
                }
            }
        }

        if (passX1 < passX0 || passY1 < passY0)
        {
            return;
        }

        Span<float> sample = stackalloc float[6];
        for (int py = passY0; py <= passY1; py++)
        {
            for (int px = passX0; px <= passX1; px++)
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

    private static void EnsureWorldSpaceTransformed(
        TriangleMesh mesh,
        Matrix4x4 rotation,
        AsciiModelState? scratch,
        out Vector3[] worldPos,
        out Vector3[] worldNormals)
    {
        int n = mesh.Vertices.Length;
        Vector3[] pos;
        Vector3[] norms;
        if (scratch != null)
        {
            if (scratch.WorldVertices == null || scratch.WorldVertices.Length < n)
            {
                scratch.WorldVertices = new Vector3[n];
                scratch.WorldVertexNormals = new Vector3[n];
            }

            pos = scratch.WorldVertices;
            norms = scratch.WorldVertexNormals!;
        }
        else
        {
            pos = new Vector3[n];
            norms = new Vector3[n];
        }

        var verts = mesh.Vertices;
        var vn = mesh.VertexNormals;
        for (int i = 0; i < n; i++)
        {
            pos[i] = Vector3.Transform(verts[i], rotation);
            norms[i] = Vector3.TransformNormal(vn[i], rotation);
        }

        worldPos = pos;
        worldNormals = norms;
    }

    private static float[] AcquireLegacyZBuffer(int length, AsciiModelState? scratch)
    {
        if (scratch == null)
        {
            return new float[length];
        }

        if (scratch.LegacyZBuffer == null || scratch.LegacyZBuffer.Length < length)
        {
            scratch.LegacyZBuffer = new float[length];
        }

        return scratch.LegacyZBuffer;
    }

    private static (float[] zBuf, float[] lumBuf) AcquireShapeBuffers(int length, AsciiModelState? scratch)
    {
        if (scratch == null)
        {
            return (new float[length], new float[length]);
        }

        if (scratch.ShapeZBuffer == null || scratch.ShapeZBuffer.Length < length)
        {
            scratch.ShapeZBuffer = new float[length];
            scratch.ShapeLumBuffer = new float[length];
        }

        return (scratch.ShapeZBuffer, scratch.ShapeLumBuffer!);
    }

    /// <summary>
    /// Skips triangles whose face normal clearly points away from the camera at the triangle centroid (eye at origin).
    /// Grazing faces (dot product near zero) are not culled; only a strictly negative dot is treated as back-facing.
    /// </summary>
    private static bool ShouldCullBackFace(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 faceNormalWorld)
    {
        if (faceNormalWorld.LengthSquared() < 1e-12f)
        {
            return false;
        }

        var unitN = Vector3.Normalize(faceNormalWorld);
        var center = (v0 + v1 + v2) * (1f / 3f);
        float lenSq = center.LengthSquared();
        if (lenSq < 1e-18f)
        {
            return false;
        }

        var toEye = Vector3.Normalize(-center);
        return Vector3.Dot(unitN, toEye) < 0f;
    }

    private static ProjPoint ProjectTransformed(Vector3 p, float zOffset, float focal, int w, int h)
    {
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
