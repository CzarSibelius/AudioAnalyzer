using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Infrastructure.AsciiVideo;

/// <summary>No-op frame source for non-Windows hosts and tests without a fake override.</summary>
public sealed class NullAsciiVideoFrameSource : IAsciiVideoFrameSource
{
    /// <inheritdoc />
    public void PrepareForFrame(AsciiVideoCaptureRequest? request)
    {
        _ = request;
    }

    /// <inheritdoc />
    public bool TryGetLatestFrame(out AsciiVideoFrameSnapshot? snapshot)
    {
        snapshot = null;
        return false;
    }

    /// <inheritdoc />
    public bool IsWebcamStarting => false;

    /// <inheritdoc />
    public bool IsWebcamSessionActive => false;

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
