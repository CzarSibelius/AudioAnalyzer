namespace AudioAnalyzer.Console;

/// <summary>Controls audio capture device lifecycle: start, stop, switch, and shutdown. Per ADR-0018 lock ordering.</summary>
internal interface IDeviceCaptureController
{
    /// <summary>Current device display name for header.</summary>
    string CurrentDeviceName { get; }

    /// <summary>Starts or replaces capture with the given device.</summary>
    /// <param name="deviceId">Device id (or null for loopback).</param>
    /// <param name="name">Display name of the device.</param>
    void StartCapture(string? deviceId, string name);

    /// <summary>Stops the current capture. Use before device selection modal. Does not dispose.</summary>
    void StopCapture();

    /// <summary>Restarts the current capture. Use when device selection is cancelled.</summary>
    void RestartCapture();

    /// <summary>Stops, disposes, and cleans up. Call on shutdown.</summary>
    void Shutdown();
}
