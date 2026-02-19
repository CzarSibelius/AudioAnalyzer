using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Application.Fft;

/// <summary>Processes FFT buffer into logarithmic frequency bands with smoothing and peak hold.</summary>
public sealed class FftBandProcessor : IFftBandProcessor
{
    private const int FftLength = 8192;
    private const double SmoothingFactor = 0.7;
    private const int PeakHoldFrames = 20;
    private const double PeakFallRate = 0.08;

    private int _numBands;
    private double[] _bandMagnitudes = Array.Empty<double>();
    private double[] _smoothedMagnitudes = Array.Empty<double>();
    private double[] _peakHold = Array.Empty<double>();
    private int[] _peakHoldTime = Array.Empty<int>();
    private double _maxMagnitudeEver = 0.001;
    private double _targetMaxMagnitude = 0.001;

    /// <inheritdoc />
    public int NumBands => _numBands;

    /// <inheritdoc />
    public double[] SmoothedMagnitudes => _smoothedMagnitudes;

    /// <inheritdoc />
    public double[] PeakHold => _peakHold;

    /// <inheritdoc />
    public double TargetMaxMagnitude => _targetMaxMagnitude;

    /// <inheritdoc />
    public void Process(ComplexFloat[] fftBuffer, int sampleRate, int numBands)
    {
        EnsureCapacity(numBands);
        _numBands = numBands;

        var bandRanges = CreateFrequencyBands(sampleRate);
        for (int b = 0; b < _numBands; b++)
        {
            double totalMagnitude = 0;
            int count = 0;
            for (int i = bandRanges[b].start; i < bandRanges[b].end && i < FftLength / 2; i++)
            {
                double magnitude = Math.Sqrt(fftBuffer[i].X * fftBuffer[i].X + fftBuffer[i].Y * fftBuffer[i].Y);
                totalMagnitude += magnitude;
                count++;
            }
            _bandMagnitudes[b] = count > 0 ? totalMagnitude / count : 0;
            _smoothedMagnitudes[b] = _smoothedMagnitudes[b] * SmoothingFactor + _bandMagnitudes[b] * (1 - SmoothingFactor);
            UpdatePeakHold(b);
            if (_smoothedMagnitudes[b] > _maxMagnitudeEver)
            {
                _maxMagnitudeEver = _smoothedMagnitudes[b];
            }
        }
        _targetMaxMagnitude = _targetMaxMagnitude * 0.95 + _maxMagnitudeEver * 0.05;
    }

    private void EnsureCapacity(int numBands)
    {
        if (_bandMagnitudes.Length != numBands)
        {
            _bandMagnitudes = new double[numBands];
            _smoothedMagnitudes = new double[numBands];
            _peakHold = new double[numBands];
            _peakHoldTime = new int[numBands];
        }
    }

    private (int start, int end)[] CreateFrequencyBands(int sampleRate)
    {
        var bandRanges = new (int start, int end)[_numBands];
        const double minFreq = 20, maxFreq = 20000;
        double logMin = Math.Log10(minFreq), logMax = Math.Log10(maxFreq);
        double step = (logMax - logMin) / _numBands;
        for (int band = 0; band < _numBands; band++)
        {
            double logStart = logMin + band * step;
            double logEnd = logMin + (band + 1) * step;
            int startFreq = (int)Math.Pow(10, logStart);
            int endFreq = (int)Math.Pow(10, logEnd);
            int startBin = (int)(startFreq * FftLength / (double)sampleRate);
            int endBin = (int)(endFreq * FftLength / (double)sampleRate);
            bandRanges[band] = (startBin, endBin);
        }
        return bandRanges;
    }

    private void UpdatePeakHold(int bandIndex)
    {
        if (_smoothedMagnitudes[bandIndex] > _peakHold[bandIndex])
        {
            _peakHold[bandIndex] = _smoothedMagnitudes[bandIndex];
            _peakHoldTime[bandIndex] = 0;
        }
        else
        {
            _peakHoldTime[bandIndex]++;
            if (_peakHoldTime[bandIndex] > PeakHoldFrames)
            {
                _peakHold[bandIndex] = Math.Max(0, _peakHold[bandIndex] - _peakHold[bandIndex] * PeakFallRate);
            }
        }
    }
}
