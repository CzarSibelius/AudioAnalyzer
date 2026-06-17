namespace AudioAnalyzer.Application.Abstractions;

/// <summary>Describes PCM delivered by a system audio capture session.</summary>
public sealed record AudioCaptureFormat(
    double SampleRate,
    int Channels,
    int BitsPerSample,
    bool IsFloat);
