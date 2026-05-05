using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Infrastructure;

namespace AudioAnalyzer.Platform.macOS.Audio;

/// <summary>
/// Non-WASAPI device listing for macOS (<c>net10.0</c> host). Demo synthesis only until Core Audio capture lands (see ADR-0084 / PBI-013).
/// </summary>
public sealed class MacOsAudioDeviceInfo : IAudioDeviceInfo
{
    /// <inheritdoc />
    public IReadOnlyList<AudioDeviceEntry> GetDevices()
    {
        return new List<AudioDeviceEntry>
        {
            new() { Name = "Demo Mode (90 BPM)", Id = DemoAudioDevice.Prefix + "90" },
            new() { Name = "Demo Mode (120 BPM)", Id = DemoAudioDevice.Prefix + "120" },
            new() { Name = "Demo Mode (140 BPM)", Id = DemoAudioDevice.Prefix + "140" },
        };
    }

    /// <inheritdoc />
    public IAudioInput CreateCapture(string? deviceId)
    {
        if (string.IsNullOrEmpty(deviceId))
        {
            return new SyntheticAudioInput(120);
        }

        if (deviceId.StartsWith(DemoAudioDevice.Prefix, StringComparison.Ordinal))
        {
            int bpm = 120;
            if (deviceId.Length > DemoAudioDevice.Prefix.Length &&
                int.TryParse(deviceId.AsSpan(DemoAudioDevice.Prefix.Length), out var parsed))
            {
                bpm = Math.Clamp(parsed, 60, 180);
            }

            return new SyntheticAudioInput(bpm);
        }

        return new SyntheticAudioInput(120);
    }
}
