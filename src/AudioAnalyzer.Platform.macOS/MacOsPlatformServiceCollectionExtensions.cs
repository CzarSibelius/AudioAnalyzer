using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Infrastructure.NowPlaying;
using AudioAnalyzer.Platform.macOS.AsciiVideo;
using AudioAnalyzer.Platform.macOS.Audio;
using AudioAnalyzer.Platform.macOS.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AudioAnalyzer.Platform.macOS;

/// <summary>Registers macOS platform services (Core Audio, ASCII video, console host helpers).</summary>
public static class MacOsPlatformServiceCollectionExtensions
{
    /// <summary>Adds the macOS implementations of the cross-platform host abstractions.</summary>
    /// <param name="services">Service collection to add to.</param>
    /// <param name="nowPlayingOverride">Optional now-playing provider override (tests).</param>
    /// <param name="asciiVideoFrameSourceOverride">Optional ASCII video frame source override (tests).</param>
    /// <param name="asciiVideoDeviceCatalogOverride">Optional ASCII video device catalog override (tests).</param>
    /// <param name="platformOptions">Optional macOS-specific overrides (Core Audio enumeration / tap factory).</param>
    public static IServiceCollection AddMacOsPlatform(
        this IServiceCollection services,
        INowPlayingProvider? nowPlayingOverride = null,
        IAsciiVideoFrameSource? asciiVideoFrameSourceOverride = null,
        IAsciiVideoDeviceCatalog? asciiVideoDeviceCatalogOverride = null,
        MacOsPlatformOptions? platformOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IMacOsAudioEnumerator>(sp =>
            platformOptions?.AudioEnumerator
            ?? new MacOsCoreAudioEnumerator(
                sp.GetRequiredService<ILogger<MacOsCoreAudioEnumerator>>(),
                sp.GetRequiredService<ILoggerFactory>()));

        services.AddSingleton<IMacOsCoreAudioTapSystemAudioInputFactory>(sp =>
            platformOptions?.TapSystemAudioInputFactory
            ?? new MacOsCoreAudioTapSystemAudioInputFactory(sp.GetRequiredService<ILoggerFactory>()));

        services.AddSingleton<IAudioDeviceInfo>(sp => new MacOsAudioDeviceInfo(
            sp.GetRequiredService<ILogger<MacOsAudioDeviceInfo>>(),
            sp.GetRequiredService<IMacOsAudioEnumerator>(),
            sp.GetRequiredService<IMacOsCoreAudioTapSystemAudioInputFactory>()));

        services.AddSingleton<INowPlayingProvider>(_ =>
            nowPlayingOverride ?? new NullNowPlayingProvider());

        services.AddSingleton<IAsciiVideoFrameSource>(sp =>
            asciiVideoFrameSourceOverride
            ?? new MacOsAsciiVideoFrameSource(sp.GetRequiredService<ILogger<MacOsAsciiVideoFrameSource>>()));

        services.AddSingleton<IAsciiVideoDeviceCatalog>(_ =>
            asciiVideoDeviceCatalogOverride ?? new MacOsAsciiVideoDeviceCatalog());

        services.AddSingleton<IScreenDumpContentProvider, NullScreenDumpContentProvider>();
        services.AddSingleton<IConsoleBufferController, MacOsConsoleBufferController>();
        services.AddSingleton<ICapsLockState, MacOsCapsLockState>();
        services.AddSingleton<IHostContentLocator, MacOsHostContentLocator>();
        services.AddSingleton<IPlatformStartupDiagnostics, MacOsStartupDiagnostics>();
        services.AddSingleton<IDefaultDeviceFallbackPolicy, MacOsDefaultDeviceFallbackPolicy>();

        return services;
    }
}
