using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Layer-specific settings for AsciiVideo. Only <see cref="AsciiVideoLayer"/> reads these.</summary>
public sealed class AsciiVideoSettings
{
    /// <summary>Input source. <see cref="AsciiVideoSourceKind.File"/> is not implemented yet.</summary>
    [Setting("SourceKind", "Source")]
    [SettingChoices("Webcam", "File")]
    public AsciiVideoSourceKind SourceKind { get; set; } = AsciiVideoSourceKind.Webcam;

    /// <summary>Zero-based index into the platform&apos;s enumerated video capture devices.</summary>
    [Setting("WebcamDeviceIndex", "Webcam device")]
    [SettingRange(0, 8, 1)]
    public int WebcamDeviceIndex { get; set; }

    /// <summary>Optional maximum capture width (0 = no cap).</summary>
    [Setting("MaxCaptureWidth", "Max capture width")]
    [SettingRange(0, 1920, 80)]
    public int MaxCaptureWidth { get; set; }

    /// <summary>Optional maximum capture height (0 = no cap).</summary>
    [Setting("MaxCaptureHeight", "Max capture height")]
    [SettingRange(0, 1080, 60)]
    public int MaxCaptureHeight { get; set; }

    /// <summary>Color source: layer palette or per-pixel colors from the frame.</summary>
    [Setting("PaletteSource", "Palette source")]
    public AsciiImagePaletteSource PaletteSource { get; set; } = AsciiImagePaletteSource.LayerPalette;

    /// <summary>Charset id for luminance→glyph mapping (<c>charsets/*.json</c>, ADR-0080). Unset uses <see cref="CharsetIds.AsciiRampClassic"/>.</summary>
    [Setting("CharsetId", "Charset")]
    [CharsetSetting]
    public string? CharsetId { get; set; }

    /// <summary>When true, mirrors the frame left–right in the layer viewport.</summary>
    [Setting("FlipHorizontal", "Flip horizontal")]
    public bool FlipHorizontal { get; set; }
}
