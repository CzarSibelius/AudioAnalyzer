namespace AudioAnalyzer.Visualizers;

/// <summary>Spatial orientation for <see cref="BufferDistortionMode.PlaneWaves"/>.</summary>
public enum BufferDistortionPlaneOrientation
{
    /// <summary>Displacement primarily in Y from a sine that varies along X.</summary>
    WaveAlongX,

    /// <summary>Displacement primarily in X from a sine that varies along Y.</summary>
    WaveAlongY,

    /// <summary>Combined X and Y displacement for a busier pattern.</summary>
    Both
}
