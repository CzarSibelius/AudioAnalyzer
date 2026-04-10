namespace AudioAnalyzer.Application.Abstractions;

/// <summary>Lists video capture devices for settings UI (e.g. S modal webcam row). Empty when unavailable or non-Windows.</summary>
public interface IAsciiVideoDeviceCatalog
{
    /// <summary>Returns devices in stable enumeration order; may be empty.</summary>
    IReadOnlyList<AsciiVideoDeviceEntry> GetDevices();
}
