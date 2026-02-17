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
        VisualizerSettings visualizerSettings)
    {
        var services = new ServiceCollection();
        services.AddSingleton(visualizerSettings);
        services.AddSingleton<IDisplayDimensions, ConsoleDisplayDimensions>();
        services.AddSingleton<ISettingsRepository>(_ => settingsRepo);
        services.AddSingleton<IVisualizerSettingsRepository>(_ => settingsRepo);
        services.AddSingleton<IPaletteRepository>(_ => new FilePaletteRepository());
        services.AddSingleton<IPresetRepository>(presetRepo);
        services.AddSingleton<IAudioDeviceInfo, NAudioDeviceInfo>();
        services.AddSingleton<INowPlayingProvider>(sp =>
        {
            if (OperatingSystem.IsWindows())
            {
                var provider = new WindowsNowPlayingProvider();
                provider.Start();
                return provider;
            }
            return new NullNowPlayingProvider();
        });

        services.AddSingleton<IVisualizer>(sp => new TextLayersVisualizer(
            sp.GetRequiredService<VisualizerSettings>().TextLayers ?? new TextLayersVisualizerSettings(),
            sp.GetRequiredService<IPaletteRepository>()));

        services.AddSingleton<IVisualizationRenderer>(sp =>
        {
            var dimensions = sp.GetRequiredService<IDisplayDimensions>();
            var visualizers = sp.GetServices<IVisualizer>();
            var visualizerSettings = sp.GetRequiredService<VisualizerSettings>();
            var nowPlayingProvider = sp.GetRequiredService<INowPlayingProvider>();
            return new VisualizationPaneLayout(dimensions, visualizers, visualizerSettings, nowPlayingProvider);
        });
        services.AddSingleton<AnalysisEngine>(sp =>
        {
            var renderer = sp.GetRequiredService<IVisualizationRenderer>();
            var dimensions = sp.GetRequiredService<IDisplayDimensions>();
            return new AnalysisEngine(renderer, dimensions);
        });

        return services.BuildServiceProvider();
    }
}
