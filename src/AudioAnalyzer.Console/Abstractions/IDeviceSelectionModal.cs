namespace AudioAnalyzer.Console;

/// <summary>Device selection modal: displays audio input devices and returns the user's selection.</summary>
internal interface IDeviceSelectionModal
{
    /// <summary>Shows the device selection menu. Returns (deviceId, name) on selection, or (null, "") on cancel.</summary>
    /// <param name="currentDeviceName">Display name of the currently selected device.</param>
    /// <param name="setModalOpen">Called with true when modal opens and false when it closes.</param>
    (string? deviceId, string name) Show(string? currentDeviceName, Action<bool> setModalOpen);
}
