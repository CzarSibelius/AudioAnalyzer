namespace AudioAnalyzer.Visualizers;

/// <summary>How the Fill layer combines with pixels already in the buffer from lower Z-order layers.</summary>
public enum FillCompositeMode
{
    /// <summary>Overwrite each cell (default).</summary>
    Replace,

    /// <summary>Blend the fill RGB over the existing cell color; use <see cref="FillSettings.BlendStrength"/>.</summary>
    BlendOver
}
