namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Audio format DTO (replaces NAudio WaveFormat in Application layer).
/// </summary>
public sealed class AudioFormat
{
    public int SampleRate { get; init; }
    public int BitsPerSample { get; init; }
    public int Channels { get; init; }

    public int BytesPerSample => BitsPerSample / 8;
    public int BytesPerFrame => BytesPerSample * Channels;
}
