namespace AudioAnalyzer.Application.Abstractions;

/// <summary>Configuration for macOS Core Audio process tap capture.</summary>
public sealed record AudioCaptureOptions(
    bool CaptureAllProcesses = true,
    int[]? ProcessIds = null,
    bool Mono = false,
    int SampleRate = 48000,
    string? DeviceUid = null,
    int StreamIndex = 0);
