namespace AudioAnalyzer.Visualizers;

/// <summary>How star color is chosen from the layer palette.</summary>
public enum StarfieldDepthShading
{
    /// <summary>Single palette entry from <c>ColorIndex</c>.</summary>
    Flat,

    /// <summary>Palette index varies with depth (farther = different index along the palette).</summary>
    DepthGradient
}
