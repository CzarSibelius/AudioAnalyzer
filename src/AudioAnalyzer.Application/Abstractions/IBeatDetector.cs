namespace AudioAnalyzer.Application.Abstractions;

/// <summary>Detects beats from audio energy and estimates BPM.</summary>
public interface IBeatDetector
{
    /// <summary>Sensitivity threshold (0.5–3.0). Higher = less sensitive.</summary>
    double BeatSensitivity { get; set; }

    /// <summary>Current detected BPM. 0 when no detection yet.</summary>
    double CurrentBpm { get; }

    /// <summary>True when a beat was recently detected (used for visual flash effects).</summary>
    bool BeatFlashActive { get; }

    /// <summary>Incremented each time a beat is detected. Used for Show playback with beats duration.</summary>
    int BeatCount { get; }

    /// <summary>Processes a frame of average energy. Call once per audio buffer.</summary>
    void ProcessFrame(double energy);

    /// <summary>Decays the beat flash. Call every ~50ms when rendering.</summary>
    void DecayFlashFrame();
}
