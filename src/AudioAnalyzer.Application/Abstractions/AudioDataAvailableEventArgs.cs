namespace AudioAnalyzer.Application.Abstractions;

/// <summary>Event args for audio data available. Buffer may be reused after the handler returns.</summary>
public sealed class AudioDataAvailableEventArgs : EventArgs
{
    public byte[] Buffer { get; init; } = null!;
    public int BytesRecorded { get; init; }
    public AudioFormat Format { get; init; } = null!;
}
