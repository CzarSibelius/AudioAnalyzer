using AudioAnalyzer.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace AudioAnalyzer.Platform.macOS.Audio;

/// <summary>Default factory for <see cref="MacOsCoreAudioTapAudioInput"/>.</summary>
public sealed class MacOsCoreAudioTapSystemAudioInputFactory : IMacOsCoreAudioTapSystemAudioInputFactory
{
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>Initializes a new instance of the <see cref="MacOsCoreAudioTapSystemAudioInputFactory"/> class.</summary>
    public MacOsCoreAudioTapSystemAudioInputFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <inheritdoc />
    public IAudioInput Create() => new MacOsCoreAudioTapAudioInput(_loggerFactory);
}
