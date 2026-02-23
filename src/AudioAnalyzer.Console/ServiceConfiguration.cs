using System.IO.Abstractions;
using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.BeatDetection;
using AudioAnalyzer.Application.Fft;
using AudioAnalyzer.Application.VolumeAnalysis;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Infrastructure;
using AudioAnalyzer.Infrastructure.NowPlaying;
using AudioAnalyzer.Platform.Windows.NowPlaying;
using AudioAnalyzer.Visualizers;
using Microsoft.Extensions.DependencyInjection;

namespace AudioAnalyzer.Console;

/// <summary>Configures dependency injection for the Audio Analyzer application.</summary>
internal static class ServiceConfiguration
{
    public static ServiceProvider Build(
        FileSettingsRepository settingsRepo,
        IPresetRepository presetRepo,
        AppSettings settings,
        VisualizerSettings visualizerSettings,
        ServiceConfigurationOptions? options = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(settings);
        services.AddSingleton(visualizerSettings);
        services.AddSingleton(settings.UiSettings ?? new UiSettings());
        services.AddSingleton<IDisplayDimensions>(sp => options?.DisplayDimensions ?? new ConsoleDisplayDimensions());
        services.AddSingleton<ISettingsRepository>(_ => settingsRepo);
        services.AddSingleton<IVisualizerSettingsRepository>(_ => settingsRepo);
        services.AddSingleton<IPaletteRepository>(_ => options?.PaletteRepository ?? new FilePaletteRepository());
        services.AddSingleton<IPresetRepository>(presetRepo);
        services.AddSingleton<IFileSystem>(_ => options?.FileSystem ?? new FileSystem());
        services.AddSingleton<IShowRepository>(sp =>
            new FileShowRepository(sp.GetRequiredService<IFileSystem>(), options?.ShowsDirectory));
        services.AddSingleton<IAudioDeviceInfo, NAudioDeviceInfo>();
        services.AddSingleton<INowPlayingProvider>(sp =>
        {
            if (options?.NowPlayingProvider != null)
            {
                return options.NowPlayingProvider;
            }
            if (OperatingSystem.IsWindows())
            {
                var provider = new WindowsNowPlayingProvider();
                provider.Start();
                return provider;
            }
            return new NullNowPlayingProvider();
        });

        services.AddTextLayerRenderers();

        services.AddSingleton<IConsoleWriter, ConsoleWriter>();
        services.AddSingleton<IKeyHandler<TextLayersKeyContext>, TextLayersKeyHandler>();
        services.AddSingleton<ITextLayersToolbarBuilder, TextLayersToolbarBuilder>();

        services.AddSingleton<IVisualizer>(sp => new TextLayersVisualizer(
            sp.GetRequiredService<VisualizerSettings>().TextLayers ?? new TextLayersVisualizerSettings(),
            sp.GetRequiredService<IPaletteRepository>(),
            sp.GetRequiredService<IEnumerable<ITextLayerRenderer>>(),
            sp.GetRequiredService<IConsoleWriter>(),
            sp.GetRequiredService<IKeyHandler<TextLayersKeyContext>>(),
            sp.GetRequiredService<ITextLayersToolbarBuilder>(),
            sp.GetRequiredService<UiSettings>()));

        services.AddSingleton<IDisplayState, DisplayState>();
        services.AddSingleton<IVisualizationRenderer, VisualizationPaneLayout>();
        services.AddSingleton<IBeatDetector, BeatDetector>();
        services.AddSingleton<IVolumeAnalyzer, VolumeAnalyzer>();
        services.AddSingleton<IFftBandProcessor, FftBandProcessor>();
        services.AddSingleton<AnalysisEngine>();
        services.AddSingleton<IVisualizationOrchestrator, VisualizationOrchestrator>();
        services.AddSingleton<ShowPlaybackController>();
        services.AddSingleton<IScrollingTextEngine, ScrollingTextEngine>();
        services.AddSingleton<IScrollingTextViewportFactory, ScrollingTextViewportFactory>();
        services.AddSingleton<ITitleBarRenderer, TitleBarRenderer>();
        services.AddSingleton<IDeviceSelectionModal, DeviceSelectionModal>();
        services.AddSingleton<IHelpModal, HelpModal>();
        services.AddSingleton<ISettingsModalRenderer, SettingsModalRenderer>();
        services.AddSingleton<IKeyHandler<SettingsModalKeyContext>, SettingsModalKeyHandler>();
        services.AddSingleton<ISettingsModal, SettingsModal>();
        services.AddSingleton<IShowEditModal, ShowEditModal>();
        services.AddSingleton<IKeyHandler<MainLoopKeyContext>, MainLoopKeyHandler>();
        services.AddSingleton<IDeviceCaptureController, DeviceCaptureController>();
        services.AddSingleton<IAppSettingsPersistence, AppSettingsPersistence>();
        services.AddSingleton<IHeaderDrawer>(sp =>
        {
            var factory = sp.GetRequiredService<IScrollingTextViewportFactory>();
            return new HeaderDrawer(
                sp.GetRequiredService<ITitleBarRenderer>(),
                factory.CreateViewport(),
                factory.CreateViewport(),
                sp.GetRequiredService<INowPlayingProvider>(),
                sp.GetRequiredService<AnalysisEngine>(),
                sp.GetRequiredService<UiSettings>());
        });
        services.AddSingleton<ApplicationShell>();

        return services.BuildServiceProvider();
    }
}
