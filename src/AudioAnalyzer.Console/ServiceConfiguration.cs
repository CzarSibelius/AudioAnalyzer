using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Infrastructure;
using AudioAnalyzer.Visualizers;
using Microsoft.Extensions.DependencyInjection;

namespace AudioAnalyzer.Console;

/// <summary>Configures dependency injection for the Audio Analyzer application.</summary>
internal static class ServiceConfiguration
{
    public static ServiceProvider Build(
        FileSettingsRepository settingsRepo,
        AppSettings settings,
        VisualizerSettings visualizerSettings)
    {
        var services = new ServiceCollection();
        services.AddSingleton(visualizerSettings);
        services.AddSingleton<IDisplayDimensions, ConsoleDisplayDimensions>();
        services.AddSingleton<ISettingsRepository>(_ => settingsRepo);
        services.AddSingleton<IVisualizerSettingsRepository>(_ => settingsRepo);
        services.AddSingleton<IPaletteRepository>(_ => new FilePaletteRepository());
        services.AddSingleton<IAudioDeviceInfo, NAudioDeviceInfo>();

        services.AddSingleton<IVisualizer, SpectrumBarsVisualizer>();
        services.AddSingleton<IVisualizer, VuMeterVisualizer>();
        services.AddSingleton<IVisualizer, WinampBarsVisualizer>();
        services.AddSingleton<IVisualizer>(sp => new TextLayersVisualizer(
            sp.GetRequiredService<VisualizerSettings>().TextLayers ?? new TextLayersVisualizerSettings(),
            sp.GetRequiredService<IPaletteRepository>()));

        services.AddSingleton<IVisualizationRenderer>(sp =>
        {
            var dimensions = sp.GetRequiredService<IDisplayDimensions>();
            var visualizers = sp.GetServices<IVisualizer>();
            return new VisualizationPaneLayout(dimensions, visualizers);
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
