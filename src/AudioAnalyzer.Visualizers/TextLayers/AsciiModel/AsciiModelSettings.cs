using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Layer-specific settings for AsciiModel. Only <see cref="AsciiModelLayer"/> reads these.</summary>
public sealed class AsciiModelSettings
{
    /// <summary>How shading maps to characters: legacy gradient or Harri-style shape matching.</summary>
    [Setting("RenderMode", "Render mode")]
    public AsciiModelRenderMode RenderMode { get; set; } = AsciiModelRenderMode.Shape;

    /// <summary>Exponent for global contrast on the 6D sampling vector when <see cref="RenderMode"/> is <see cref="AsciiModelRenderMode.Shape"/>. 1.0 disables; higher sharpens boundaries.</summary>
    [Setting("ShapeContrastExponent", "Shape contrast")]
    [SettingRange(1.0, 2.5, 0.05)]
    public double ShapeContrastExponent { get; set; } = 1.35;

    /// <summary>Minimum diffuse intensity (0–1). Blends with directional light: <c>ambient + (1 - ambient) * Lambert</c>; reduces fully black back faces.</summary>
    [Setting("Ambient", "Ambient")]
    [SettingRange(0.0, 1.0, 0.05)]
    public double Ambient { get; set; } = 0.2;

    /// <summary>Built-in light direction or custom angles when <see cref="LightingPreset"/> is <see cref="AsciiModelLightingPreset.Custom"/>.</summary>
    [Setting("LightingPreset", "Lighting preset")]
    public AsciiModelLightingPreset LightingPreset { get; set; } = AsciiModelLightingPreset.Classic;

    /// <summary>Horizontal angle in degrees (0 = +X, 90 = +Y) when <see cref="LightingPreset"/> is <see cref="AsciiModelLightingPreset.Custom"/>.</summary>
    [Setting("LightAzimuthDegrees", "Light azimuth °")]
    [SettingRange(-180.0, 180.0, 5.0)]
    public double LightAzimuthDegrees { get; set; } = 35.0;

    /// <summary>Vertical angle in degrees (0 = XY plane, 90 = +Z) when <see cref="LightingPreset"/> is <see cref="AsciiModelLightingPreset.Custom"/>.</summary>
    [Setting("LightElevationDegrees", "Light elevation °")]
    [SettingRange(-89.0, 89.0, 5.0)]
    public double LightElevationDegrees { get; set; } = 45.0;

    /// <summary>How a beat affects this layer. Default None.</summary>
    [Setting("BeatReaction", "Beat reaction")]
    public AsciiModelBeatReaction BeatReaction { get; set; } = AsciiModelBeatReaction.None;

    /// <summary>Path to folder containing .obj files. Can be null/empty.</summary>
    [Setting("ModelPath", "Model folder")]
    public string? ModelFolderPath { get; set; }

    /// <summary>File name (not full path) of the .obj to show within <see cref="ModelFolderPath"/>; null uses the first file in sorted order.</summary>
    [ExcludeFromSettingsModal]
    public string? SelectedModelFileName { get; set; }

    /// <summary>Rotation axis mode. Default Y.</summary>
    [Setting("RotationAxis", "Rotation axis")]
    public AsciiModelRotationAxis RotationAxis { get; set; } = AsciiModelRotationAxis.Y;

    /// <summary>Rotation direction. Default CounterClockwise.</summary>
    [Setting("RotationDirection", "Rotation direction")]
    public AsciiModelRotationDirection RotationDirection { get; set; } = AsciiModelRotationDirection.CounterClockwise;

    /// <summary>Base rotation speed multiplier (0.002–0.15 radians per frame step before SpeedMultiplier). Default 0.03.</summary>
    [Setting("RotationSpeed", "Rotation speed")]
    [SettingRange(0.002, 0.15, 0.002)]
    public double RotationSpeed { get; set; } = 0.03;

    /// <summary>When true, camera distance oscillates between ZoomMin and ZoomMax. Default true.</summary>
    [Setting("EnableZoom", "Zoom enabled")]
    public bool EnableZoom { get; set; } = true;

    /// <summary>Minimum camera-distance scale (0.5–1.5). Default 0.75.</summary>
    [Setting("ZoomMin", "Zoom min")]
    [SettingRange(0.5, 1.5, 0.05)]
    public double ZoomMin { get; set; } = 0.75;

    /// <summary>Maximum camera-distance scale (0.5–2.0). Default 1.35.</summary>
    [Setting("ZoomMax", "Zoom max")]
    [SettingRange(0.5, 2.0, 0.05)]
    public double ZoomMax { get; set; } = 1.35;

    /// <summary>Multiplier for zoom phase increment. Default 0.02.</summary>
    [Setting("ZoomSpeed", "Zoom speed")]
    [SettingRange(0.005, 0.1, 0.005)]
    public double ZoomSpeed { get; set; } = 0.02;

    /// <summary>Zoom animation style. Default Sine.</summary>
    [Setting("ZoomStyle", "Zoom style")]
    public AsciiImageZoomStyle ZoomStyle { get; set; } = AsciiImageZoomStyle.Sine;

    /// <summary>Skip drawing when triangle count exceeds this (performance). Default 50000.</summary>
    [Setting("MaxTriangles", "Max triangles")]
    [SettingRange(1000, 200000, 1000)]
    public int MaxTriangles { get; set; } = 50000;
}
