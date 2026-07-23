namespace AudioAnalyzer.Application.Abstractions;

/// <summary>Broad grouping for a reported feature capability.</summary>
public enum FeatureCapabilityCategory
{
    /// <summary>Audio capture / input features.</summary>
    Audio,

    /// <summary>Visual capture features (e.g. webcam ASCII video).</summary>
    Visual,

    /// <summary>External integrations (e.g. Ableton Link, now playing, screen dump).</summary>
    Integration,

    /// <summary>Operating-system permission grants (e.g. macOS TCC).</summary>
    Permission
}
