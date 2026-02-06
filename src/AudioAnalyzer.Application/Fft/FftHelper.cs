namespace AudioAnalyzer.Application.Fft;

/// <summary>
/// In-place radix-2 FFT (no NAudio dependency in Application layer).
/// </summary>
internal static class FftHelper
{
    public static void Fft(bool forward, int log2N, ComplexFloat[] data)
    {
        int n = 1 << log2N;
        int j = 0;
        for (int i = 0; i < n - 1; i++)
        {
            if (i < j)
            {
                (data[i], data[j]) = (data[j], data[i]);
            }
            int k = n >> 1;
            while (j >= k)
            {
                j -= k;
                k >>= 1;
            }
            j += k;
        }

        int sign = forward ? 1 : -1;
        for (int len = 2; len <= n; len <<= 1)
        {
            float angle = sign * 2.0f * MathF.PI / len;
            float wlenX = MathF.Cos(angle);
            float wlenY = MathF.Sin(angle);
            int halfLen = len >> 1;

            for (int i = 0; i < n; i += len)
            {
                float wX = 1.0f;
                float wY = 0.0f;
                for (int k = 0; k < halfLen; k++)
                {
                    int u = i + k;
                    int v = u + halfLen;
                    float tX = data[v].X * wX - data[v].Y * wY;
                    float tY = data[v].X * wY + data[v].Y * wX;
                    data[v].X = data[u].X - tX;
                    data[v].Y = data[u].Y - tY;
                    data[u].X += tX;
                    data[u].Y += tY;
                    float nextWX = wX * wlenX - wY * wlenY;
                    float nextWY = wX * wlenY + wY * wlenX;
                    wX = nextWX;
                    wY = nextWY;
                }
            }
        }

        if (!forward)
        {
            float scale = 1.0f / n;
            for (int i = 0; i < n; i++)
            {
                data[i].X *= scale;
                data[i].Y *= scale;
            }
        }
    }
}
