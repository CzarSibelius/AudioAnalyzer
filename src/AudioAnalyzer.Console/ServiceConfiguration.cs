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

        services.AddSingleton<TextLayerStateStore>();
        services.AddSingleton<ITextLayerStateStore>(sp => sp.GetRequiredService<TextLayerStateStore>());
        services.AddSingleton<ITextLayerStateStore<FallingLettersLayerState>>(sp => sp.GetRequiredService<TextLayerStateStore>());
        services.AddSingleton<ITextLayerStateStore<AsciiImageState>>(sp => sp.GetRequiredService<TextLayerStateStore>());
        services.AddSingleton<ITextLayerStateStore<GeissBackgroundState>>(sp => sp.GetRequiredService<TextLayerStateStore>());
        services.AddSingleton<ITextLayerStateStore<BeatCirclesState>>(sp => sp.GetRequiredService<TextLayerStateStore>());
        services.AddSingleton<ITextLayerStateStore<UnknownPleasuresState>>(sp => sp.GetRequiredService<TextLayerStateStore>());
        services.AddSingleton<ITextLayerStateStore<MaschineState>>(sp => sp.GetRequiredService<TextLayerStateStore>());
        services.AddTextLayerRenderers();

        services.AddSingleton<IConsoleWriter, ConsoleWriter>();
        services.AddSingleton<IConsoleDimensions, ConsoleDimensions>();
        services.AddSingleton(typeof(IKeyHandler<>), typeof(GenericKeyHandler<>));
        services.AddSingleton<IKeyHandlerConfig<TextLayersKeyContext>, TextLayersKeyHandlerConfig>();
        services.AddSingleton<ITextLayersToolbarBuilder, TextLayersToolbarBuilder>();

        services.AddSingleton<IVisualizer>(sp => new TextLayersVisualizer(
            sp.GetRequiredService<VisualizerSettings>().TextLayers ?? new TextLayersVisualizerSettings(),
            sp.GetRequiredService<IPaletteRepository>(),
            sp.GetRequiredService<IEnumerable<TextLayerRendererBase>>(),
            sp.GetRequiredService<IConsoleWriter>(),
            sp.GetRequiredService<IKeyHandler<TextLayersKeyContext>>(),
            sp.GetRequiredService<ITextLayersToolbarBuilder>(),
            sp.GetRequiredService<ITextLayerStateStore>(),
            sp.GetRequiredService<UiSettings>()));

        services.AddSingleton<IDisplayState, DisplayState>();
        services.AddSingleton<IUiComponentRenderer<ScrollingTextComponent>, ScrollingTextComponentRenderer>();
        services.AddSingleton<IUiComponentRenderer<HorizontalRowComponent>, HorizontalRowComponentRenderer>();
        services.AddSingleton<IUiComponentRenderer<IUiComponent>>(sp =>
            new UiComponentRenderer(
                sp.GetRequiredService<IUiComponentRenderer<HorizontalRowComponent>>(),
                sp.GetRequiredService<IUiComponentRenderer<VisualizerAreaComponent>>()));
        services.AddSingleton<IUiStateUpdater<HeaderContainer>, HeaderContainerStateUpdater>();
        services.AddSingleton<IUiStateUpdater<IUiComponent>>(sp =>
            new UiComponentStateUpdater(sp.GetRequiredService<IUiStateUpdater<HeaderContainer>>()));
        services.AddSingleton<ITitleBarContentProvider, TitleBarContentProvider>();
        services.AddSingleton<IHeaderContainer>(sp =>
            new HeaderContainer(
                sp.GetRequiredService<IUiComponentRenderer<IUiComponent>>(),
                sp.GetRequiredService<IUiStateUpdater<IUiComponent>>(),
                sp.GetRequiredService<IDisplayDimensions>(),
                sp.GetRequiredService<INowPlayingProvider>(),
                sp.GetRequiredService<AnalysisEngine>(),
                sp.GetRequiredService<UiSettings>(),
                sp.GetRequiredService<ITitleBarContentProvider>()));
        services.AddSingleton<IVisualizationRenderer>(sp =>
            new MainContentContainer(
                sp.GetRequiredService<IUiComponentRenderer<IUiComponent>>(),
                sp.GetRequiredService<IUiStateUpdater<IUiComponent>>(),
                sp.GetRequiredService<IVisualizer>(),
                sp.GetRequiredService<IDisplayState>(),
                sp.GetRequiredService<UiSettings>()));
        services.AddSingleton<IBeatDetector, BeatDetector>();
        services.AddSingleton<IVolumeAnalyzer, VolumeAnalyzer>();
        services.AddSingleton<IFftBandProcessor, FftBandProcessor>();
        services.AddSingleton<AnalysisEngine>();
        services.AddSingleton<IVisualizationOrchestrator, VisualizationOrchestrator>();
        services.AddSingleton<ShowPlaybackController>();
        services.AddSingleton<IScrollingTextEngine, ScrollingTextEngine>();
        services.AddSingleton<IScrollingTextViewportFactory, ScrollingTextViewportFactory>();
        services.AddSingleton<IUiComponentRenderer<VisualizerAreaComponent>, VisualizerAreaRenderer>();
        services.AddSingleton<IKeyHandlerConfig<DeviceSelectionKeyContext>, DeviceSelectionKeyHandlerConfig>();
        services.AddSingleton<IDeviceSelectionModal, DeviceSelectionModal>();
        services.AddSingleton<IHelpContentProvider, HelpContentProvider>();
        services.AddSingleton<IHelpModal, HelpModal>();
        services.AddSingleton<ISettingsModalRenderer, SettingsModalRenderer>();
        services.AddSingleton<IKeyHandlerConfig<SettingsModalKeyContext>, SettingsModalKeyHandlerConfig>();
        services.AddSingleton<ISettingsModal, SettingsModal>();
        services.AddSingleton<IKeyHandlerConfig<ShowEditModalKeyContext>, ShowEditModalKeyHandlerConfig>();
        services.AddSingleton<IShowEditModal, ShowEditModal>();
        services.AddSingleton<IKeyHandlerConfig<MainLoopKeyContext>, MainLoopKeyHandlerConfig>();
        services.AddSingleton<IDeviceCaptureController, DeviceCaptureController>();
        services.AddSingleton<IAppSettingsPersistence, AppSettingsPersistence>();
        services.AddSingleton<IScreenDumpService, ScreenDumpService>();
        services.AddSingleton<ApplicationShell>();

        return services.BuildServiceProvider();
    }
}
