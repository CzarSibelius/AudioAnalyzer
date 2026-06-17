using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Platform.Windows.AsciiVideo;
using AudioAnalyzer.Platform.Windows.Audio;
using AudioAnalyzer.Platform.Windows.Hosting;
using AudioAnalyzer.Platform.Windows.NowPlaying;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AudioAnalyzer.Platform.Windows;

/// <summary>Registers Windows platform services (audio, now-playing, ASCII video, console host helpers).</summary>
public static class WindowsPlatformServiceCollectionExtensions
{
    /// <summary>Adds the Windows implementations of the cross-platform host abstractions.</summary>
    /// <param name="services">Service collection to add to.</param>
    /// <param name="nowPlayingOverride">Optional now-playing provider override (tests).</param>
    /// <param name="asciiVideoFrameSourceOverride">Optional ASCII video frame source override (tests).</param>
    /// <param name="asciiVideoDeviceCatalogOverride">Optional ASCII video device catalog override (tests).</param>
    public static IServiceCollection AddWindowsPlatform(
        this IServiceCollection services,
        INowPlayingProvider? nowPlayingOverride = null,
        IAsciiVideoFrameSource? asciiVideoFrameSourceOverride = null,
        IAsciiVideoDeviceCatalog? asciiVideoDeviceCatalogOverride = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IAudioDeviceInfo, WindowsAudioDeviceInfo>();

        services.AddSingleton<INowPlayingProvider>(_ =>
        {
            if (nowPlayingOverride != null)
            {
                return nowPlayingOverride;
            }

            var provider = new WindowsNowPlayingProvider();
            provider.Start();
            return provider;
        });

        services.AddSingleton<IAsciiVideoFrameSource>(sp =>
            asciiVideoFrameSourceOverride
            ?? new WindowsAsciiVideoFrameSource(sp.GetRequiredService<ILogger<WindowsAsciiVideoFrameSource>>()));

        services.AddSingleton<IAsciiVideoDeviceCatalog>(_ =>
            asciiVideoDeviceCatalogOverride ?? new WindowsAsciiVideoDeviceCatalog());

        services.AddSingleton<IScreenDumpContentProvider, WindowsConsoleScreenDumpContentProvider>();
        services.AddSingleton<IConsoleBufferController, WindowsConsoleBufferController>();
        services.AddSingleton<ICapsLockState, WindowsCapsLockState>();
        services.AddSingleton<IHostContentLocator, WindowsHostContentLocator>();
        services.AddSingleton<IPlatformStartupDiagnostics, WindowsStartupDiagnostics>();
        services.AddSingleton<IDefaultDeviceFallbackPolicy, WindowsDefaultDeviceFallbackPolicy>();

        return services;
    }
}
