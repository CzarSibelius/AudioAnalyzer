using AudioAnalyzer.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
#if WINDOWS
using AudioAnalyzer.Platform.Windows;
using AudioAnalyzer.Platform.Windows.Hosting;
#elif MACOS
using AudioAnalyzer.Platform.macOS;
using AudioAnalyzer.Platform.macOS.Hosting;
#else
#error AudioAnalyzer.Console must be built for net10.0-windows… or the pinned net10.0-macos* host TFM.
#endif

namespace AudioAnalyzer.Console;

/// <summary>
/// The single compile-time operating-system switch for the host. Selects the platform project
/// (referenced per target framework) for content-path resolution and DI registration so the rest
/// of the host code is free of operating-system conditionals.
/// </summary>
internal static class PlatformSelection
{
    /// <summary>Creates the platform content locator (used before the DI container is built).</summary>
    public static IHostContentLocator CreateContentLocator() =>
#if WINDOWS
        new WindowsHostContentLocator();
#elif MACOS
        new MacOsHostContentLocator();
#endif

    /// <summary>Registers the platform services (audio, now-playing, ASCII video, console host helpers).</summary>
    public static void AddPlatformServices(IServiceCollection services, ServiceConfigurationOptions? options)
    {
        ArgumentNullException.ThrowIfNull(services);
#if WINDOWS
        services.AddWindowsPlatform(
            options?.NowPlayingProvider,
            options?.AsciiVideoFrameSource,
            options?.AsciiVideoDeviceCatalog);
#elif MACOS
        services.AddMacOsPlatform(
            options?.NowPlayingProvider,
            options?.AsciiVideoFrameSource,
            options?.AsciiVideoDeviceCatalog,
            options?.PlatformOverrides as MacOsPlatformOptions);
#endif
    }
}
