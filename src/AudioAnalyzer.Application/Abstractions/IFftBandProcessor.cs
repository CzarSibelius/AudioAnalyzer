using AudioAnalyzer.Application.Fft;

namespace AudioAnalyzer.Application.Abstractions;

/// <summary>Processes FFT buffer into frequency bands with smoothing and peak hold.</summary>
public interface IFftBandProcessor
{
    /// <summary>Number of bands from last Process call.</summary>
    int NumBands { get; }

    /// <summary>Smoothed magnitude per band. Length = NumBands. Contents may change on next Process call.</summary>
    double[] SmoothedMagnitudes { get; }

    /// <summary>Peak hold per band. Length = NumBands. Contents may change on next Process call.</summary>
    double[] PeakHold { get; }

    /// <summary>Target max magnitude for normalization.</summary>
    double TargetMaxMagnitude { get; }

    /// <summary>Processes the FFT buffer (already FFT-transformed), creates bands, updates smoothing and peak hold.</summary>
    /// <param name="fftBuffer">FFT buffer (positive half used).</param>
    /// <param name="sampleRate">Audio sample rate in Hz.</param>
    /// <param name="numBands">Number of frequency bands (from display width).</param>
    void Process(ComplexFloat[] fftBuffer, int sampleRate, int numBands);
}
