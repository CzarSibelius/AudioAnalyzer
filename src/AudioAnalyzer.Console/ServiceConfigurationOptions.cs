using System.IO.Abstractions;
using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Console;

/// <summary>Options for test or custom service configuration. When set, override the default implementations.</summary>
internal sealed class ServiceConfigurationOptions
{
    public IDisplayDimensions? DisplayDimensions { get; init; }
    public INowPlayingProvider? NowPlayingProvider { get; init; }

    /// <summary>Override ASCII video frame source for tests (e.g. fake frames without a camera).</summary>
    public IAsciiVideoFrameSource? AsciiVideoFrameSource { get; init; }

    /// <summary>Override webcam device list for tests (S modal display names).</summary>
    public IAsciiVideoDeviceCatalog? AsciiVideoDeviceCatalog { get; init; }
    public IPaletteRepository? PaletteRepository { get; init; }
    /// <summary>File system for repositories (e.g. MockFileSystem for tests). When null, uses the real file system.</summary>
    public IFileSystem? FileSystem { get; init; }
    /// <summary>Shows directory path when using a custom file system (e.g. for tests). When null, uses default next to executable.</summary>
    public string? ShowsDirectory { get; init; }

    /// <summary>UI themes directory when using a custom file system (e.g. for tests). When null, uses default next to executable.</summary>
    public string? ThemesDirectory { get; init; }

    /// <summary>Override UI theme repository for tests.</summary>
    public IUiThemeRepository? UiThemeRepository { get; init; }

    /// <summary>Override charset repository for tests.</summary>
    public ICharsetRepository? CharsetRepository { get; init; }

    /// <summary>Charsets directory when using a custom file system (e.g. tests).</summary>
    public string? CharsetsDirectory { get; init; }
}
