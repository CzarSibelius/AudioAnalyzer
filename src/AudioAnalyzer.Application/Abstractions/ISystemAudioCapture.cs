namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// macOS-only system/output audio capture via Core Audio process taps (macOS 14.2+). Implementations
/// live in the macOS platform project and are resolved through dependency injection.
/// </summary>
public interface ISystemAudioCapture : IAsyncDisposable
{
    /// <summary>Format reported by the native tap/aggregate input stream after capture starts.</summary>
    AudioCaptureFormat Format { get; }

    /// <summary>Starts capture if not already running.</summary>
    void Start();

    /// <summary>Stops capture and releases native tap resources.</summary>
    void StopCapture();

    /// <summary>PCM chunks (interleaved); buffer memory is valid only until the enumerator advances.</summary>
    IAsyncEnumerable<ReadOnlyMemory<byte>> CaptureAsync(CancellationToken cancellationToken = default);
}
