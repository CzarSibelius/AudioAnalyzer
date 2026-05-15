namespace AudioAnalyzer.Console;

/// <summary>Controls audio capture device lifecycle: start, stop, switch, and shutdown. Per ADR-0018 lock ordering.</summary>
internal interface IDeviceCaptureController
{
    /// <summary>Current device display name for header.</summary>
    string CurrentDeviceName { get; }

    /// <summary>Current device id passed to <see cref="StartCapture"/> (e.g. <c>demo:120</c>), or null for default loopback.</summary>
    string? CurrentDeviceId { get; }

    /// <summary>Starts or replaces capture with the given device.</summary>
    /// <param name="deviceId">Device id (or null for loopback).</param>
    /// <param name="name">Display name of the device.</param>
    void StartCapture(string? deviceId, string name);

    /// <summary>Stops the current capture without disposing (pause-like).</summary>
    void StopCapture();

    /// <summary>
    /// Stops, disposes, and clears the active capture before opening device enumeration UI.
    /// Required on macOS so Core Audio can enumerate devices without blocking on an existing Audio Queue client.
    /// Current device id/name are kept so <see cref="RestartCapture"/> can recreate capture after cancel.
    /// </summary>
    void ReleaseCaptureForDeviceSelection();

    /// <summary>Restarts the current capture. Use when device selection is cancelled.</summary>
    void RestartCapture();

    /// <summary>Stops, disposes, and cleans up. Call on shutdown.</summary>
    void Shutdown();
}
