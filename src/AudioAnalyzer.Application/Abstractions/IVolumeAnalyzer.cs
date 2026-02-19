namespace AudioAnalyzer.Application.Abstractions;

/// <summary>Analyzes audio volume per frame: smoothed channels, peaks, and VU-style peak hold.</summary>
public interface IVolumeAnalyzer
{
    /// <summary>Latest volume from audio processing (0–1). Used for header display.</summary>
    float Volume { get; }

    /// <summary>Smoothed left channel level (0–1).</summary>
    float LeftChannel { get; }

    /// <summary>Smoothed right channel level (0–1).</summary>
    float RightChannel { get; }

    /// <summary>VU-style peak hold for left channel.</summary>
    float LeftPeakHold { get; }

    /// <summary>VU-style peak hold for right channel.</summary>
    float RightPeakHold { get; }

    /// <summary>Processes a frame of max left, right, and mono volume. Call once per audio buffer.</summary>
    void ProcessFrame(float maxLeft, float maxRight, float maxVolume);
}
