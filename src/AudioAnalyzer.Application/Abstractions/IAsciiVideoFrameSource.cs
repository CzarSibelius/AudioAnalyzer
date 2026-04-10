namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Provides the latest decoded webcam (or future source) frame for the ASCII video layer.
/// Capture runs off the render thread; <see cref="TryGetLatestFrame"/> returns a snapshot copy for the consumer.
/// </summary>
public interface IAsciiVideoFrameSource : IDisposable
{
    /// <summary>
    /// Called once per main visualization frame before any layer draws.
    /// Pass <c>null</c> when no enabled ASCII video layer needs capture so the implementation can stop and release hardware.
    /// </summary>
    void PrepareForFrame(AsciiVideoCaptureRequest? request);

    /// <summary>
    /// Gets a copy of the most recent frame, or <c>false</c> if capture is inactive or no frame is available yet.
    /// </summary>
    bool TryGetLatestFrame(out AsciiVideoFrameSnapshot? snapshot);

    /// <summary>
    /// <c>true</c> while the implementation is opening the webcam (before <see cref="IsWebcamSessionActive"/> becomes <c>true</c> or startup fails).
    /// </summary>
    bool IsWebcamStarting { get; }

    /// <summary>
    /// <c>true</c> when a webcam capture session is streaming (reader started). <see cref="TryGetLatestFrame"/> may still return <c>false</c> until the first frame arrives.
    /// </summary>
    bool IsWebcamSessionActive { get; }
}
