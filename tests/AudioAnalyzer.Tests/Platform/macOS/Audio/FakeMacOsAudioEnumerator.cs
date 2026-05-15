using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Platform.macOS.Audio;

namespace AudioAnalyzer.Tests.Platform.macOS.Audio;

internal sealed class FakeMacOsAudioEnumerator : IMacOsAudioEnumerator
{
    public List<MacOsPhysicalAudioDevice> PhysicalInputs { get; } = new();

    /// <summary>Device id string → capture instance returned by <see cref="TryCreateCapture"/>.</summary>
    public Dictionary<string, IAudioInput> Captures { get; } = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public IReadOnlyList<MacOsPhysicalAudioDevice> GetPhysicalInputs() => PhysicalInputs;

    /// <inheritdoc />
    public bool TryCreateCapture(string deviceId, out IAudioInput? input)
    {
        return Captures.TryGetValue(deviceId, out input);
    }
}
