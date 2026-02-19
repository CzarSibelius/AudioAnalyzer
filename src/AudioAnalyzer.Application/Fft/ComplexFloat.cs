namespace AudioAnalyzer.Application.Fft;

/// <summary>Complex number for FFT operations.</summary>
public struct ComplexFloat
{
    public float X;
    public float Y;

    public ComplexFloat(float x, float y)
    {
        X = x;
        Y = y;
    }
}
