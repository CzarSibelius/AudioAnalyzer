namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Managed view of Ableton Link (native shim). Read-only tempo/beat queries; optional enable/disable.
/// </summary>
public interface ILinkSession : IDisposable
{
    /// <summary>True when the native <c>link_shim</c> library loaded and created a session.</summary>
    bool IsAvailable { get; }

    /// <summary>Whether Link networking is enabled.</summary>
    bool IsEnabled { get; }

    /// <summary>Enables or disables Link discovery (non-realtime).</summary>
    void SetEnabled(bool enabled);

    /// <summary>
    /// App-thread capture: session tempo (BPM), peer count, and beat at host time for <paramref name="quantum"/>.
    /// </summary>
    void Capture(out double tempoBpm, out int numPeers, out double beat, double quantum);
}
