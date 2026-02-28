using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Mutable context passed to the device selection modal key handler.</summary>
internal sealed class DeviceSelectionKeyContext : IKeyHandlerContext
{
    /// <summary>Available audio devices. Read-only.</summary>
    public required IReadOnlyList<AudioDeviceEntry> Devices { get; init; }

    /// <summary>Index of the selected device. Mutated by the handler.</summary>
    public int SelectedIndex { get; set; }

    /// <summary>Selected device id after user confirms. Set by the handler.</summary>
    public string? ResultId { get; set; }

    /// <summary>Selected device name after user confirms. Set by the handler.</summary>
    public string ResultName { get; set; } = "";

    /// <summary>App settings to update when user selects a device.</summary>
    public required AppSettings Settings { get; init; }

    /// <summary>Repository to persist app settings.</summary>
    public required ISettingsRepository SettingsRepo { get; init; }
}
