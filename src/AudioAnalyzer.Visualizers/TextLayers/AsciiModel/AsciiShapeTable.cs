using System.Numerics;

namespace AudioAnalyzer.Visualizers;

/// <summary>Nearest-character lookup and global contrast on 6D sampling vectors (Harri-style).</summary>
internal static partial class AsciiShapeTable
{
    /// <summary>Applies global contrast (normalize by max component, pow, scale back). Exponent 1 leaves unchanged.</summary>
    public static void ApplyGlobalContrast(Span<float> sampling6, float exponent)
    {
        if (exponent <= 1.0001f)
        {
            return;
        }

        float max = 0f;
        for (int i = 0; i < 6; i++)
        {
            max = Math.Max(max, sampling6[i]);
        }

        if (max < 1e-8f)
        {
            return;
        }

        for (int i = 0; i < 6; i++)
        {
            float x = sampling6[i] / max;
            sampling6[i] = MathF.Pow(x, exponent) * max;
        }
    }

    /// <summary>Picks the character whose normalized shape vector is closest in Euclidean distance.</summary>
    public static char FindBestCharacter(ReadOnlySpan<float> sampling6)
    {
        ReadOnlySpan<float> rows = NormalizedShapeRows;
        int count = ShapeCharset.Length;
        float s0 = sampling6[0];
        float s1 = sampling6[1];
        float s2 = sampling6[2];
        float s3 = sampling6[3];
        float s4 = sampling6[4];
        float s5 = sampling6[5];

        int best = 0;
        float bestDist = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            int o = i * 6;
            var dv = new Vector4(s0 - rows[o], s1 - rows[o + 1], s2 - rows[o + 2], s3 - rows[o + 3]);
            float d = Vector4.Dot(dv, dv);
            float t4 = s4 - rows[o + 4];
            float t5 = s5 - rows[o + 5];
            d += t4 * t4 + t5 * t5;
            if (d < bestDist)
            {
                bestDist = d;
                best = i;
            }
        }

        return ShapeCharset[best];
    }
}
