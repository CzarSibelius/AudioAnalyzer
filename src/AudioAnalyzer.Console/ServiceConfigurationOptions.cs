using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Console;

/// <summary>Options for test or custom service configuration. When set, override the default implementations.</summary>
internal sealed class ServiceConfigurationOptions
{
    public IDisplayDimensions? DisplayDimensions { get; init; }
    public INowPlayingProvider? NowPlayingProvider { get; init; }
    public IPaletteRepository? PaletteRepository { get; init; }
}
