namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Abstraction for audio capture. Start/Stop and receive audio data via event.
/// </summary>
public interface IAudioInput : IDisposable
{
    /// <summary>
    /// Raised when audio data is available. Buffer may be reused after the handler returns.
    /// </summary>
    event EventHandler<AudioDataAvailableEventArgs>? DataAvailable;

    void Start();
    void Stop();
}

public sealed class AudioDataAvailableEventArgs : EventArgs
{
    public byte[] Buffer { get; init; } = null!;
    public int BytesRecorded { get; init; }
    public AudioFormat Format { get; init; } = null!;
}
