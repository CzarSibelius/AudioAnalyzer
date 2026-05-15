using AudioAnalyzer.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace AudioAnalyzer.Platform.macOS.Audio;

/// <summary>Default factory for <see cref="MacOsScreenCaptureKitSystemAudioInput"/>.</summary>
public sealed class MacOsScreenCaptureKitSystemAudioInputFactory : IMacOsScreenCaptureKitSystemAudioInputFactory
{
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>Initializes a new instance of the <see cref="MacOsScreenCaptureKitSystemAudioInputFactory"/> class.</summary>
    public MacOsScreenCaptureKitSystemAudioInputFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <inheritdoc />
    public IAudioInput Create() =>
        new MacOsScreenCaptureKitSystemAudioInput(_loggerFactory.CreateLogger<MacOsScreenCaptureKitSystemAudioInput>());
}
