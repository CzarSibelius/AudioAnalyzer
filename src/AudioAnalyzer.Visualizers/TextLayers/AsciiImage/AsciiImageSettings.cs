using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Layer-specific settings for AsciiImage. Only AsciiImageLayer reads these.</summary>
public sealed class AsciiImageSettings
{
    /// <summary>Path to folder containing images. Can be null/empty.</summary>
    public string? ImageFolderPath { get; set; }

    /// <summary>Movement mode. Default Scroll.</summary>
    public AsciiImageMovement Movement { get; set; } = AsciiImageMovement.Scroll;
}
