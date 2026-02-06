using System.Text.Json;

public class Settings
{
    /// <summary>
    /// Audio input mode: "loopback" for system audio, "microphone" for mic input, or "device" for specific device
    /// </summary>
    public string InputMode { get; set; } = "loopback";

    /// <summary>
    /// Specific device name to use when InputMode is "device".
    /// Use --list-devices to see available devices.
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// Visualization mode: "spectrum", "oscilloscope", "vumeter", or "winamp"
    /// </summary>
    public string VisualizationMode { get; set; } = "spectrum";

    /// <summary>
    /// Beat detection sensitivity (0.5 = very sensitive, 2.0 = less sensitive). Default is 1.3
    /// </summary>
    public double BeatSensitivity { get; set; } = 1.3;

    /// <summary>
    /// Show expanding circles on beat in Geiss mode
    /// </summary>
    public bool BeatCircles { get; set; } = true;

    private static readonly string SettingsPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

    public static Settings Load()
    {
        if (!File.Exists(SettingsPath))
        {
            var defaultSettings = new Settings();
            defaultSettings.Save();
            return defaultSettings;
        }

        try
        {
            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<Settings>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new Settings();
        }
        catch
        {
            return new Settings();
        }
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(SettingsPath, json);
    }
}
