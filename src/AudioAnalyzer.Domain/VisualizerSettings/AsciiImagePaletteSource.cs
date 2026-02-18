namespace AudioAnalyzer.Domain;

/// <summary>Color source for the ASCII image layer.</summary>
public enum AsciiImagePaletteSource
{
    /// <summary>Map brightness to the layer's selected palette.</summary>
    LayerPalette,

    /// <summary>Use the actual RGB color of each source pixel from the image.</summary>
    ImageColors
}
