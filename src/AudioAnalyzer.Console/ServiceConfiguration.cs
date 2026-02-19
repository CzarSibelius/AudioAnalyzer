using System.IO.Abstractions;
using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
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

        services.AddSingleton<IVisualizer>(sp => new TextLayersVisualizer(
            sp.GetRequiredService<VisualizerSettings>().TextLayers ?? new TextLayersVisualizerSettings(),
            sp.GetRequiredService<IPaletteRepository>(),
            sp.GetRequiredService<IEnumerable<ITextLayerRenderer>>(),
            sp.GetRequiredService<IConsoleWriter>(),
            sp.GetRequiredService<UiSettings>()));

        services.AddSingleton<IVisualizationRenderer>(sp =>
        {
            var dimensions = sp.GetRequiredService<IDisplayDimensions>();
            var visualizers = sp.GetServices<IVisualizer>();
            var visualizerSettings = sp.GetRequiredService<VisualizerSettings>();
            var uiSettings = sp.GetRequiredService<UiSettings>();
            return new VisualizationPaneLayout(dimensions, visualizers, visualizerSettings, uiSettings);
        });
        services.AddSingleton<AnalysisEngine>(sp =>
        {
            var renderer = sp.GetRequiredService<IVisualizationRenderer>();
            var dimensions = sp.GetRequiredService<IDisplayDimensions>();
            return new AnalysisEngine(renderer, dimensions);
        });
        services.AddSingleton<ShowPlaybackController>();
        services.AddSingleton<IDeviceSelectionModal, DeviceSelectionModal>();
        services.AddSingleton<IHelpModal, HelpModal>();
        services.AddSingleton<ISettingsModal, SettingsModal>();
        services.AddSingleton<IShowEditModal, ShowEditModal>();
        services.AddSingleton<ApplicationShell>();

        return services.BuildServiceProvider();
    }
}
