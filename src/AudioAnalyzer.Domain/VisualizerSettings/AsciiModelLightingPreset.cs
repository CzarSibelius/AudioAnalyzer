namespace AudioAnalyzer.Domain;

/// <summary>How AsciiModel chooses the directional light vector (Lambert diffuse).</summary>
public enum AsciiModelLightingPreset
{
    /// <summary>Fixed direction matching the original shader (~ toward +X,+Y,+Z).</summary>
    Classic = 0,

    /// <summary>Light from the camera / viewer direction (+Z in projection space); front faces stay brighter while rotating.</summary>
    Headlight = 1,

    /// <summary>Use custom azimuth and elevation degrees from layer settings (AsciiModel).</summary>
    Custom = 2
}
