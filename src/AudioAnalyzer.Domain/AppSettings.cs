namespace AudioAnalyzer.Domain;

/// <summary>
/// Application configuration (domain model). Persistence is handled via ISettingsRepository.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Audio input mode: "loopback" for system audio, or "device" for specific device.
    /// </summary>
    public string InputMode { get; set; } = "loopback";

    /// <summary>
    /// Specific device name when InputMode is "device".
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// Visualization mode: "spectrum", "oscilloscope", "vumeter", "winamp", or "geiss".
    /// </summary>
    public string VisualizationMode { get; set; } = "spectrum";

    /// <summary>
    /// Beat detection sensitivity (0.5 = very sensitive, 2.0 = less sensitive). Default is 1.3.
    /// </summary>
    public double BeatSensitivity { get; set; } = 1.3;

    /// <summary>
    /// Show expanding circles on beat in Geiss mode.
    /// Deprecated: use VisualizerSettings.Geiss.BeatCircles. Kept for backward compatibility when loading.
    /// </summary>
    public bool BeatCircles { get; set; } = true;

    /// <summary>
    /// Oscilloscope amplitude gain (1.0 = no boost, higher = more visible waveform). Default is 2.5; range typically 1.0–10.0.
    /// Deprecated: use VisualizerSettings.Oscilloscope.Gain. Kept for backward compatibility when loading.
    /// </summary>
    public double OscilloscopeGain { get; set; } = 2.5;

    /// <summary>
    /// Id of the currently selected palette (e.g. filename without extension). Resolved via IPaletteRepository.
    /// </summary>
    public string? SelectedPaletteId { get; set; }
}
