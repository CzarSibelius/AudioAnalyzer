using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Layer-specific settings for AsciiImage. Only AsciiImageLayer reads these.</summary>
public sealed class AsciiImageSettings
{
    /// <summary>Path to folder containing images. Can be null/empty.</summary>
    [Setting("ImagePath", "Image path")]
    public string? ImageFolderPath { get; set; }

    /// <summary>Movement mode. Default Scroll.</summary>
    [Setting("AsciiMovement", "Movement")]
    public AsciiImageMovement Movement { get; set; } = AsciiImageMovement.Scroll;

    /// <summary>Color source: layer palette or per-pixel image colors. Default LayerPalette.</summary>
    [Setting("PaletteSource", "Palette source")]
    public AsciiImagePaletteSource PaletteSource { get; set; } = AsciiImagePaletteSource.LayerPalette;

    /// <summary>Minimum zoom scale (0.5–1.0). Default 0.85.</summary>
    [Setting("ZoomMin", "Zoom min")]
    [SettingRange(0.5, 1.0, 0.05)]
    public double ZoomMin { get; set; } = 0.85;

    /// <summary>Maximum zoom scale (1.0–2.0). Default 1.3.</summary>
    [Setting("ZoomMax", "Zoom max")]
    [SettingRange(1.0, 2.0, 0.05)]
    public double ZoomMax { get; set; } = 1.3;

    /// <summary>Multiplier for zoom phase increment (0.005–0.1). Default 0.02.</summary>
    [Setting("ZoomSpeed", "Zoom speed")]
    [SettingRange(0.005, 0.1, 0.005)]
    public double ZoomSpeed { get; set; } = 0.02;

    /// <summary>Zoom animation style. Default Sine.</summary>
    [Setting("ZoomStyle", "Zoom style")]
    public AsciiImageZoomStyle ZoomStyle { get; set; } = AsciiImageZoomStyle.Sine;

    /// <summary>Scroll ratio Y = ScrollX * this (0 = horizontal only, 1 = diagonal equal). Default 0.5.</summary>
    [Setting("ScrollRatioY", "Scroll ratio Y")]
    [SettingRange(0.0, 1.0, 0.1)]
    public double ScrollRatioY { get; set; } = 0.5;
}
