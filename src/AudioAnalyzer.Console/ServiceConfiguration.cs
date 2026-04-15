using System.IO.Abstractions;
using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Application.BeatDetection;
using AudioAnalyzer.Infrastructure.Link;
using AudioAnalyzer.Application.Fft;
using AudioAnalyzer.Application.VolumeAnalysis;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Infrastructure;
using AudioAnalyzer.Infrastructure.AsciiVideo;
using AudioAnalyzer.Infrastructure.NowPlaying;
using AudioAnalyzer.Platform.Windows.AsciiVideo;
using AudioAnalyzer.Platform.Windows.NowPlaying;
using AudioAnalyzer.Visualizers;
using AudioAnalyzer.Infrastructure.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
        services.AddSingleton<ILoggerProvider>(sp =>
            BackgroundFileLoggerProvider.Create(sp.GetRequiredService<AppSettings>().Logging, sp.GetRequiredService<IFileSystem>()));
        services.AddLogging(logging =>
        {
            AppLoggingSettings logSettings = settings.Logging ?? new AppLoggingSettings();
            logging.SetMinimumLevel(logSettings.Enabled ? AppLoggingLevelParser.Parse(logSettings.MinimumLevel) : LogLevel.None);
        });
        services.AddSingleton(settings.UiSettings ?? new UiSettings());
        services.AddSingleton<IDisplayDimensions>(sp => options?.DisplayDimensions ?? new ConsoleDisplayDimensions());
        services.AddSingleton<ISettingsRepository>(_ => settingsRepo);
        services.AddSingleton<IVisualizerSettingsRepository>(_ => settingsRepo);
        services.AddSingleton<IPaletteRepository>(_ => options?.PaletteRepository ?? new FilePaletteRepository());
        services.AddSingleton<IUiThemeRepository>(_ =>
            options?.UiThemeRepository
            ?? new FileUiThemeRepository(options?.FileSystem ?? new FileSystem(), options?.ThemesDirectory));
        services.AddSingleton<IUiThemeResolver, UiThemeResolver>();
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

        services.AddSingleton<IAsciiVideoFrameSource>(sp =>
        {
            if (options?.AsciiVideoFrameSource != null)
            {
                return options.AsciiVideoFrameSource;
            }

            if (OperatingSystem.IsWindows())
            {
                return new WindowsAsciiVideoFrameSource(sp.GetRequiredService<ILogger<WindowsAsciiVideoFrameSource>>());
            }

            return new NullAsciiVideoFrameSource();
        });

        services.AddSingleton<IAsciiVideoDeviceCatalog>(_ =>
        {
            if (options?.AsciiVideoDeviceCatalog != null)
            {
                return options.AsciiVideoDeviceCatalog;
            }

            if (OperatingSystem.IsWindows())
            {
                return new WindowsAsciiVideoDeviceCatalog();
            }

            return new NullAsciiVideoDeviceCatalog();
        });

        services.AddSingleton<IDefaultTextLayersSettingsFactory>(_ => new DefaultTextLayersSettingsFactory());
        services.AddSingleton<TextLayerStateStore>();
        services.AddSingleton<ITextLayerStateStore>(sp => sp.GetRequiredService<TextLayerStateStore>());
        services.AddSingleton<ITextLayerStateStore<FallingLettersLayerState>>(sp => sp.GetRequiredService<TextLayerStateStore>());
        services.AddSingleton<ITextLayerStateStore<AsciiImageState>>(sp => sp.GetRequiredService<TextLayerStateStore>());
        services.AddSingleton<ITextLayerStateStore<AsciiModelState>>(sp => sp.GetRequiredService<TextLayerStateStore>());
        services.AddSingleton<ITextLayerStateStore<GeissBackgroundState>>(sp => sp.GetRequiredService<TextLayerStateStore>());
        services.AddSingleton<ITextLayerStateStore<FractalZoomState>>(sp => sp.GetRequiredService<TextLayerStateStore>());
        services.AddSingleton<ITextLayerStateStore<BeatCirclesState>>(sp => sp.GetRequiredService<TextLayerStateStore>());
        services.AddSingleton<ITextLayerStateStore<UnknownPleasuresState>>(sp => sp.GetRequiredService<TextLayerStateStore>());
        services.AddSingleton<ITextLayerStateStore<MaschineState>>(sp => sp.GetRequiredService<TextLayerStateStore>());
        services.AddSingleton<ITextLayerStateStore<AsciiVideoState>>(sp => sp.GetRequiredService<TextLayerStateStore>());
        services.AddSingleton<ITextLayerStateStore<BufferDistortionState>>(sp => sp.GetRequiredService<TextLayerStateStore>());
        services.AddSingleton<ITextLayerStateStore<WaveformStripLayerState>>(sp => sp.GetRequiredService<TextLayerStateStore>());
        services.AddTextLayerRenderers();

        services.AddSingleton<IConsoleWriter, ConsoleWriter>();
        services.AddSingleton<IConsoleDimensions, ConsoleDimensions>();
        services.AddSingleton(typeof(IKeyHandler<>), typeof(GenericKeyHandler<>));
        services.AddSingleton<IKeyHandlerConfig<TextLayersKeyContext>, TextLayersKeyHandlerConfig>();
        services.AddSingleton<ITextLayersToolbarBuilder, TextLayersToolbarBuilder>();
        services.AddSingleton<ITextLayerBoundsEditSession, TextLayerBoundsEditSession>();
        services.AddSingleton<PresetEditorApplicationMode>();
        services.AddSingleton<ShowPlayApplicationMode>();
        services.AddSingleton<SettingsApplicationMode>();
        services.AddSingleton<IApplicationModeFactory, ApplicationModeFactory>();
        services.AddSingleton<IApplicationModeHeaderProvider, ApplicationModeHeaderProvider>();
        services.AddSingleton<IShowPlayToolbarInfo, ShowPlayToolbarInfo>();
        services.AddSingleton<IModeTransitionService, ModeTransitionService>();

        services.AddSingleton<IVisualizer>(sp => new TextLayersVisualizer(
            sp.GetRequiredService<VisualizerSettings>().TextLayers ?? new TextLayersVisualizerSettings(),
            sp.GetRequiredService<IPaletteRepository>(),
            sp.GetRequiredService<IEnumerable<TextLayerRendererBase>>(),
            sp.GetRequiredService<IConsoleWriter>(),
            sp.GetRequiredService<IKeyHandler<TextLayersKeyContext>>(),
            sp.GetRequiredService<ITextLayersToolbarBuilder>(),
            sp.GetRequiredService<ITextLayerStateStore>(),
            sp.GetRequiredService<VisualizerSettings>(),
            sp.GetRequiredService<IFileSystem>(),
            sp.GetRequiredService<IAsciiVideoFrameSource>(),
            sp.GetRequiredService<IShowPlayToolbarInfo>(),
            sp.GetRequiredService<UiSettings>(),
            sp.GetRequiredService<ITextLayerBoundsEditSession>()));
        services.AddSingleton<IFullLayerRuntimeReset>(sp => (IFullLayerRuntimeReset)sp.GetRequiredService<IVisualizer>());

        services.AddSingleton<IDisplayState, DisplayState>();
        services.AddSingleton<IUiComponentRenderer<ScrollingTextComponent>, ScrollingTextComponentRenderer>();
        services.AddSingleton<IUiComponentRenderer<HorizontalRowComponent>, HorizontalRowComponentRenderer>();
        services.AddSingleton<IUiComponentRenderer<IUiComponent>>(sp =>
            new UiComponentRenderer(
                sp.GetRequiredService<IUiComponentRenderer<HorizontalRowComponent>>(),
                sp.GetRequiredService<IUiComponentRenderer<VisualizerAreaComponent>>(),
                sp.GetRequiredService<IUiComponentRenderer<GeneralSettingsHubAreaComponent>>()));
        services.AddSingleton<IUiStateUpdater<HeaderContainer>, HeaderContainerStateUpdater>();
        services.AddSingleton<IUiStateUpdater<IUiComponent>>(sp =>
            new UiComponentStateUpdater(sp.GetRequiredService<IUiStateUpdater<HeaderContainer>>()));
        services.AddSingleton<ITitleBarNavigationContext, TitleBarNavigationContext>();
        services.AddSingleton<ITitleBarBreadcrumbFormatter, TitleBarBreadcrumbFormatter>();
        services.AddSingleton<ITitleBarContentProvider, TitleBarContentProvider>();
        services.AddSingleton<IHeaderContainer>(sp =>
            new HeaderContainer(
                sp.GetRequiredService<IUiComponentRenderer<IUiComponent>>(),
                sp.GetRequiredService<IUiStateUpdater<IUiComponent>>(),
                sp.GetRequiredService<IDisplayDimensions>(),
                sp.GetRequiredService<INowPlayingProvider>(),
                sp.GetRequiredService<AnalysisEngine>(),
                sp.GetRequiredService<UiSettings>(),
                sp.GetRequiredService<IUiThemeResolver>(),
                sp.GetRequiredService<ITitleBarContentProvider>(),
                sp.GetRequiredService<IApplicationModeFactory>(),
                sp.GetRequiredService<IDisplayFrameClock>(),
                sp.GetRequiredService<ILogger<HeaderContainer>>()));
        services.AddSingleton<Lazy<IDeviceCaptureController>>(sp =>
            new Lazy<IDeviceCaptureController>(() => sp.GetRequiredService<IDeviceCaptureController>()));
        services.AddSingleton<IVisualizationRenderer>(sp =>
            new MainContentContainer(
                sp.GetRequiredService<IUiComponentRenderer<IUiComponent>>(),
                sp.GetRequiredService<IUiStateUpdater<IUiComponent>>(),
                sp.GetRequiredService<IVisualizer>(),
                sp.GetRequiredService<IDisplayState>(),
                sp.GetRequiredService<UiSettings>(),
                sp.GetRequiredService<IUiThemeResolver>(),
                sp.GetRequiredService<ITextLayerBoundsEditSession>(),
                sp.GetRequiredService<IApplicationModeFactory>(),
                sp.GetRequiredService<Lazy<IDeviceCaptureController>>(),
                sp.GetRequiredService<ILogger<MainContentContainer>>()));
        services.AddSingleton<IBeatDetector, BeatDetector>();
        services.AddSingleton(sp => new AudioDerivedBeatTimingSource(sp.GetRequiredService<IBeatDetector>()));
        services.AddSingleton<DemoBeatTimingSource>();
        services.AddSingleton<LinkSessionNative>();
        services.AddSingleton<ILinkSession>(sp => sp.GetRequiredService<LinkSessionNative>());
        services.AddSingleton<LinkBeatTimingSource>();
        services.AddSingleton<BeatTimingRouter>();
        services.AddSingleton<IBeatTimingSource>(sp => sp.GetRequiredService<BeatTimingRouter>());
        services.AddSingleton<IBeatTimingConfigurator>(sp => sp.GetRequiredService<BeatTimingRouter>());
        services.AddSingleton<IVolumeAnalyzer, VolumeAnalyzer>();
        services.AddSingleton<IFftBandProcessor, FftBandProcessor>();
        services.AddSingleton<IWaveformOverviewRebuildPolicy, VisualizerSettingsWaveformOverviewRebuildPolicy>();
        services.AddSingleton<AnalysisEngine>(sp => new AnalysisEngine(
            sp.GetRequiredService<IBeatTimingSource>(),
            sp.GetRequiredService<IVolumeAnalyzer>(),
            sp.GetRequiredService<IFftBandProcessor>(),
            sp.GetRequiredService<IWaveformOverviewRebuildPolicy>()));
        services.AddSingleton<IWaveformHistoryConfigurator>(sp => sp.GetRequiredService<AnalysisEngine>());
        services.AddSingleton<IWaveformRetainedHistoryReset>(sp => sp.GetRequiredService<AnalysisEngine>());
        services.AddSingleton<ILayerRuntimeResetCoordinator, LayerRuntimeResetCoordinator>();
        services.AddSingleton<MainRenderFpsMeter>();
        services.AddSingleton<IDisplayFrameClock, DisplayFrameClock>();
        services.AddSingleton<IVisualizationOrchestrator>(sp =>
            new VisualizationOrchestrator(
                sp.GetRequiredService<AnalysisEngine>(),
                sp.GetRequiredService<IVisualizationRenderer>(),
                sp.GetRequiredService<IDisplayDimensions>(),
                sp.GetRequiredService<IDisplayState>(),
                sp.GetRequiredService<IApplicationModeHeaderProvider>(),
                sp.GetRequiredService<UiSettings>(),
                sp.GetRequiredService<MainRenderFpsMeter>(),
                sp.GetRequiredService<IDisplayFrameClock>(),
                sp.GetRequiredService<ILogger<VisualizationOrchestrator>>()));
        services.AddSingleton(sp => new ShowPlaybackController(
            sp.GetRequiredService<VisualizerSettings>(),
            sp.GetRequiredService<IShowRepository>(),
            sp.GetRequiredService<IPresetRepository>(),
            sp.GetRequiredService<AnalysisEngine>(),
            new Lazy<IVisualizer>(() => sp.GetRequiredService<IVisualizer>())));
        services.AddSingleton<IScrollingTextEngine, ScrollingTextEngine>();
        services.AddSingleton<IScrollingTextViewportFactory, ScrollingTextViewportFactory>();
        services.AddSingleton<IUiComponentRenderer<VisualizerAreaComponent>>(sp =>
            new VisualizerAreaRenderer(
                sp.GetRequiredService<IVisualizer>(),
                sp.GetRequiredService<ILogger<VisualizerAreaRenderer>>()));
        services.AddSingleton<GeneralSettingsHubState>();
        services.AddSingleton<IUiComponentRenderer<GeneralSettingsHubAreaComponent>>(sp =>
            new GeneralSettingsHubAreaRenderer(
                sp.GetRequiredService<GeneralSettingsHubState>(),
                sp.GetRequiredService<UiSettings>(),
                sp.GetRequiredService<AppSettings>(),
                sp.GetRequiredService<IPaletteRepository>(),
                sp.GetRequiredService<IUiThemeRepository>(),
                sp.GetRequiredService<IUiComponentRenderer<HorizontalRowComponent>>()));
        services.AddSingleton<IKeyHandlerConfig<GeneralSettingsHubKeyContext>, GeneralSettingsHubKeyHandlerConfig>();
        services.AddSingleton<IKeyHandler<GeneralSettingsHubKeyContext>, GenericKeyHandler<GeneralSettingsHubKeyContext>>();
        services.AddSingleton<IKeyHandlerConfig<UiThemeSelectionKeyContext>, UiThemeSelectionKeyHandlerConfig>();
        services.AddSingleton<IKeyHandler<UiThemeSelectionKeyContext>, GenericKeyHandler<UiThemeSelectionKeyContext>>();
        services.AddSingleton<IUiThemeSelectionModal, UiThemeSelectionModal>();
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
