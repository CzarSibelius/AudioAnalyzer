using System.IO.Abstractions;
using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Console;

/// <summary>Options for test or custom service configuration. When set, override the default implementations.</summary>
internal sealed class ServiceConfigurationOptions
{
    public IDisplayDimensions? DisplayDimensions { get; init; }
    public INowPlayingProvider? NowPlayingProvider { get; init; }
    public IPaletteRepository? PaletteRepository { get; init; }
    /// <summary>File system for repositories (e.g. MockFileSystem for tests). When null, uses the real file system.</summary>
    public IFileSystem? FileSystem { get; init; }
    /// <summary>Shows directory path when using a custom file system (e.g. for tests). When null, uses default next to executable.</summary>
    public string? ShowsDirectory { get; init; }
}
