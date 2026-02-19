using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Application.VolumeAnalysis;

/// <summary>Analyzes volume per frame: smoothed channel levels and VU-style peak hold.</summary>
public sealed class VolumeAnalyzer : IVolumeAnalyzer
{
    private float _leftChannel;
    private float _rightChannel;
    private float _leftPeak;
    private float _rightPeak;
    private float _leftPeakHold;
    private float _rightPeakHold;
    private int _leftPeakHoldTime;
    private int _rightPeakHoldTime;
    private float _lastVolume;

    /// <inheritdoc />
    public float Volume => _lastVolume;

    /// <inheritdoc />
    public float LeftChannel => _leftChannel;

    /// <inheritdoc />
    public float RightChannel => _rightChannel;

    /// <inheritdoc />
    public float LeftPeakHold => _leftPeakHold;

    /// <inheritdoc />
    public float RightPeakHold => _rightPeakHold;

    /// <inheritdoc />
    public void ProcessFrame(float maxLeft, float maxRight, float maxVolume)
    {
        _lastVolume = maxVolume;
        _leftChannel = _leftChannel * 0.7f + maxLeft * 0.3f;
        _rightChannel = _rightChannel * 0.7f + maxRight * 0.3f;

        if (maxLeft > _leftPeak)
        {
            _leftPeak = maxLeft;
        }
        else
        {
            _leftPeak *= 0.95f;
        }

        if (maxRight > _rightPeak)
        {
            _rightPeak = maxRight;
        }
        else
        {
            _rightPeak *= 0.95f;
        }

        UpdateVuPeakHold(ref _leftPeakHold, ref _leftPeakHoldTime, maxLeft);
        UpdateVuPeakHold(ref _rightPeakHold, ref _rightPeakHoldTime, maxRight);
    }

    private static void UpdateVuPeakHold(ref float peakHold, ref int holdTime, float current)
    {
        if (current > peakHold) { peakHold = current; holdTime = 0; }
        else
        {
            holdTime++;
            if (holdTime > 30)
            {
                peakHold = Math.Max(0, peakHold - 0.02f);
            }
        }
    }
}
