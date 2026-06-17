namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Chooses the default audio device when fresh settings request the loopback / "what you hear"
/// input but no system-audio entry is present. Implemented per platform (e.g. macOS prefers Demo
/// over the first listed device) and injected so device resolution does not branch on the operating system.
/// </summary>
public interface IDefaultDeviceFallbackPolicy
{
    /// <summary>
    /// Resolves the fallback device id and name from the available devices. Called after the
    /// system-audio (null id) and cross-platform tap entries have been ruled out.
    /// </summary>
    (string? deviceId, string name) ResolveLoopbackFallback(IReadOnlyList<AudioDeviceEntry> devices);
}
