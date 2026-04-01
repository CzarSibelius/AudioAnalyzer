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
        int best = 0;
        float bestDist = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            float d = 0f;
            int o = i * 6;
            for (int k = 0; k < 6; k++)
            {
                float t = sampling6[k] - rows[o + k];
                d += t * t;
            }

            if (d < bestDist)
            {
                bestDist = d;
                best = i;
            }
        }

        return ShapeCharset[best];
    }
}
