namespace AudioAnalyzer.Application.Abstractions;

/// <summary>One enumerated video capture device for ASCII video settings UI.</summary>
/// <param name="Index">Zero-based index (matches <c>MediaFrameSourceGroup</c> order on Windows).</param>
/// <param name="DisplayName">Human-readable name from the platform, or a fallback.</param>
public sealed record AsciiVideoDeviceEntry(int Index, string DisplayName);
