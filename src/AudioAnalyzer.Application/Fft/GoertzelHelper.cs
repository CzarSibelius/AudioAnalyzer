namespace AudioAnalyzer.Application.Fft;

/// <summary>Single-frequency Goertzel magnitude for a short real sample block (overview bucket spectral coloring).</summary>
internal static class GoertzelHelper
{
    private const int MaxSamplesPerBucket = 256;

    /// <summary>Returns a band power estimate in roughly the same scale as sample amplitude (not normalized).</summary>
    public static float BandPower(ReadOnlySpan<float> samples, double targetHz, double sampleRate)
    {
        if (samples.IsEmpty || sampleRate <= 1.0 || targetHz <= 0)
        {
            return 0f;
        }

        double nyquist = sampleRate * 0.5;
        if (targetHz >= nyquist * 0.98)
        {
            targetHz = nyquist * 0.98;
        }

        int n = Math.Min(samples.Length, MaxSamplesPerBucket);
        if (n < 2)
        {
            return Math.Abs(samples[0]);
        }

        double omega = 2.0 * Math.PI * targetHz / sampleRate;
        double coeff = 2.0 * Math.Cos(omega);
        double s = 0;
        double sprev = 0;
        double sprev2 = 0;
        for (int i = 0; i < n; i++)
        {
            s = samples[i] + coeff * sprev - sprev2;
            sprev2 = sprev;
            sprev = s;
        }

        double cosw = Math.Cos(omega);
        double sinw = Math.Sin(omega);
        double real = sprev - sprev2 * cosw;
        double imag = sprev2 * sinw;
        return (float)Math.Sqrt(real * real + imag * imag) / n;
    }
}
