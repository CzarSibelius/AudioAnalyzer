using System.Diagnostics.CodeAnalysis;
using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Palette;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;
using Microsoft.Extensions.Logging;

namespace AudioAnalyzer.Console;

/// <summary>
/// Main application shell: orchestrates engine, renderer, header, modals, key handling, and device capture lifecycle.
/// </summary>
/// <remarks>
/// <para><strong>Responsibility.</strong> Shell is the host: it runs the main loop, owns device lifecycle and modal state,
/// routes keys to the renderer and main-loop key handler, and contains app logic (mode switch, preset cycle, palette cycle).
/// It configures the visualization orchestrator (header callbacks, render guard, console lock) and triggers redraws
/// (e.g. after key handling); it does not perform rendering or audio processing—that is delegated to
/// <see cref="IVisualizationOrchestrator"/> and the engine/renderer.</para>
/// </remarks>
[SuppressMessage("Reliability", "CA1001:Types that own disposable fields should be disposable", Justification = "Single Run/Shutdown lifecycle; _headerRefreshCts explicitly disposed in Shutdown().")]
internal sealed partial class ApplicationShell
{
    private readonly IDeviceCaptureController _deviceController;
    private readonly IVisualizerSettingsRepository _visualizerSettingsRepo;
    private readonly VisualizerSettings _visualizerSettings;
    private readonly IPresetRepository _presetRepository;
    private readonly IPaletteRepository _paletteRepo;
    private readonly AnalysisEngine _engine;
    private readonly IDisplayState _displayState;
    private readonly IVisualizationOrchestrator _orchestrator;
    private readonly IVisualizationRenderer _renderer;
    private readonly ShowPlaybackController _showPlaybackController;
    private readonly IHeaderContainer _headerContainer;
    private readonly IKeyHandler<MainLoopKeyContext> _keyHandler;
    private readonly IAppSettingsPersistence _settingsPersistence;
    private readonly IDeviceSelectionModal _deviceSelectionModal;
    private readonly IUiThemeSelectionModal _uiThemeSelectionModal;
    private readonly IHelpModal _helpModal;
    private readonly ISettingsModal _settingsModal;
    private readonly IShowEditModal _showEditModal;
    private readonly IScreenDumpService _screenDumpService;
    private readonly IKeyHandler<GeneralSettingsHubKeyContext> _generalSettingsHubKeyHandler;
    private readonly GeneralSettingsHubState _generalSettingsHubState;
    private readonly UiSettings _uiSettings;
    private readonly IModeTransitionService _modeTransitionService;
    private readonly IApplicationModeFactory _applicationModeFactory;
    private readonly AppSettings _appSettings;
    private readonly IBeatTimingConfigurator _beatTiming;
    private readonly ILogger<ApplicationShell> _logger;

    private CancellationTokenSource? _headerRefreshCts;
    private volatile bool _quitAfterDump;

    public ApplicationShell(
        IDeviceCaptureController deviceController,
        IVisualizerSettingsRepository visualizerSettingsRepo,
        VisualizerSettings visualizerSettings,
        IPresetRepository presetRepository,
        IPaletteRepository paletteRepo,
        AnalysisEngine engine,
        IDisplayState displayState,
        IVisualizationOrchestrator orchestrator,
        IVisualizationRenderer renderer,
        ShowPlaybackController showPlaybackController,
        IHeaderContainer headerContainer,
        IKeyHandler<MainLoopKeyContext> keyHandler,
        IAppSettingsPersistence settingsPersistence,
        IDeviceSelectionModal deviceSelectionModal,
        IUiThemeSelectionModal uiThemeSelectionModal,
        IHelpModal helpModal,
        ISettingsModal settingsModal,
        IShowEditModal showEditModal,
        IScreenDumpService screenDumpService,
        IKeyHandler<GeneralSettingsHubKeyContext> generalSettingsHubKeyHandler,
        GeneralSettingsHubState generalSettingsHubState,
        UiSettings uiSettings,
        IModeTransitionService modeTransitionService,
        IApplicationModeFactory applicationModeFactory,
        AppSettings appSettings,
        IBeatTimingConfigurator beatTiming,
        ILogger<ApplicationShell> logger)
    {
        _deviceController = deviceController ?? throw new ArgumentNullException(nameof(deviceController));
        _visualizerSettingsRepo = visualizerSettingsRepo;
        _visualizerSettings = visualizerSettings;
        _presetRepository = presetRepository;
        _paletteRepo = paletteRepo;
        _engine = engine;
        _displayState = displayState ?? throw new ArgumentNullException(nameof(displayState));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _renderer = renderer;
        _showPlaybackController = showPlaybackController ?? throw new ArgumentNullException(nameof(showPlaybackController));
        _headerContainer = headerContainer ?? throw new ArgumentNullException(nameof(headerContainer));
        _keyHandler = keyHandler ?? throw new ArgumentNullException(nameof(keyHandler));
        _settingsPersistence = settingsPersistence ?? throw new ArgumentNullException(nameof(settingsPersistence));
        _deviceSelectionModal = deviceSelectionModal ?? throw new ArgumentNullException(nameof(deviceSelectionModal));
        _uiThemeSelectionModal = uiThemeSelectionModal ?? throw new ArgumentNullException(nameof(uiThemeSelectionModal));
        _helpModal = helpModal ?? throw new ArgumentNullException(nameof(helpModal));
        _settingsModal = settingsModal ?? throw new ArgumentNullException(nameof(settingsModal));
        _showEditModal = showEditModal ?? throw new ArgumentNullException(nameof(showEditModal));
        _screenDumpService = screenDumpService ?? throw new ArgumentNullException(nameof(screenDumpService));
        _generalSettingsHubKeyHandler = generalSettingsHubKeyHandler ?? throw new ArgumentNullException(nameof(generalSettingsHubKeyHandler));
        _generalSettingsHubState = generalSettingsHubState ?? throw new ArgumentNullException(nameof(generalSettingsHubState));
        _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
        _modeTransitionService = modeTransitionService ?? throw new ArgumentNullException(nameof(modeTransitionService));
        _applicationModeFactory = applicationModeFactory ?? throw new ArgumentNullException(nameof(applicationModeFactory));
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        _beatTiming = beatTiming ?? throw new ArgumentNullException(nameof(beatTiming));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Runs the main loop. Does not return until the user quits.</summary>
    /// <param name="initialDeviceId">Initial audio device id (or null for loopback).</param>
    /// <param name="initialDeviceName">Display name of the initial device.</param>
    /// <param name="dumpAfterSeconds">If set, dump screen after this many seconds and exit (for automation).</param>
    /// <param name="dumpPath">Optional directory for the dump file when using dump-after.</param>
    public void Run(string? initialDeviceId, string initialDeviceName, int? dumpAfterSeconds = null, string? dumpPath = null)
    {
        _quitAfterDump = false;
        bool modalOpen = false;
        object consoleLock = new();

        _orchestrator.SetHeaderCallback(
            () => _headerContainer.DrawMain(_deviceController.CurrentDeviceName),
            () => _headerContainer.DrawHeaderOnly(_deviceController.CurrentDeviceName));
        _orchestrator.SetRenderGuard(() => !modalOpen);
        _orchestrator.SetConsoleLock(consoleLock);

        _headerRefreshCts = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(50));
            try
            {
                while (await timer.WaitForNextTickAsync(_headerRefreshCts.Token).ConfigureAwait(false))
                {
                    try
                    {
                        _orchestrator.RefreshHeaderIfNeeded();
                    }
                    catch (Exception ex)
                    {
                        LogHeaderRefreshFailed(ex);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when shutting down
            }
        });

        _deviceController.StartCapture(initialDeviceId, initialDeviceName);
        _orchestrator.RedrawWithFullHeader();

        int? dumpDelay = dumpAfterSeconds > 0 ? dumpAfterSeconds : null;
        if (dumpDelay is int d)
        {
            int delaySecs = d;
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(delaySecs)).ConfigureAwait(false);
                lock (consoleLock)
                {
                    _orchestrator.Redraw();
                    _screenDumpService.DumpToFile(stripAnsi: true, directory: dumpPath);
                    _quitAfterDump = true;
                }
            });
        }

        bool running = true;
        while (running)
        {
            if (_quitAfterDump)
            {
                running = false;
                break;
            }

            if (_visualizerSettings.ApplicationMode == ApplicationMode.ShowPlay)
            {
                _showPlaybackController.Tick();
            }

            if (System.Console.KeyAvailable)
            {
                var key = System.Console.ReadKey(true);
                if (_applicationModeFactory.GetActiveApplicationMode().UsesGeneralSettingsHubKeyHandling)
                {
                    var hubCtx = CreateGeneralSettingsHubKeyContext(consoleLock, open => modalOpen = open);
                    if (_generalSettingsHubKeyHandler.Handle(key, hubCtx))
                    {
                        _settingsPersistence.Save();
                        if (!_displayState.FullScreen)
                        {
                            _orchestrator.RedrawWithFullHeader();
                        }
                        else
                        {
                            _orchestrator.Redraw();
                        }

                        Thread.Sleep(0);
                        continue;
                    }
                }
                if (_renderer.HandleKey(key))
                {
                    _settingsPersistence.Save();
                    _orchestrator.Redraw();
                }
                else
                {
                    var ctx = CreateKeyContext(consoleLock, open => modalOpen = open);
                    if (_keyHandler.Handle(key, ctx))
                    {
                        if (ctx.ShouldQuit)
                        {
                            running = false;
                        }
                    }
                }
            }

            try
            {
                _orchestrator.Redraw();
            }
            catch (Exception ex)
            {
                LogMainLoopDisplayTickFailed(ex);
            }

            // Cooperative yield: avoid busy-spin without enforcing a ~60 Hz ceiling (ADR-0067).
            Thread.Sleep(0);
        }

        Shutdown();
    }

    private MainLoopKeyContext CreateKeyContext(object consoleLock, Action<bool> setModalOpen)
    {
        return new MainLoopKeyContext
        {
            SetModalOpen = setModalOpen,
            ConsoleLock = consoleLock,
            RefreshHeaderAndRedraw = () => _orchestrator.RedrawWithFullHeader(),
            SaveSettings = () => _settingsPersistence.Save(),
            SaveVisualizerSettings = () => _visualizerSettingsRepo.SaveVisualizerSettings(_visualizerSettings),
            GetDeviceName = () => _deviceController.CurrentDeviceName,
            DisplayState = _displayState,
            Orchestrator = _orchestrator,
            Engine = _engine,
            HeaderContainer = _headerContainer,
            OnModeSwitch = () => _modeTransitionService.CycleToNextMode(),
            OnPresetCycle = CycleToNextPreset,
            SettingsModal = _settingsModal,
            ShowEditModal = _showEditModal,
            StopCapture = _deviceController.StopCapture,
            StartCapture = _deviceController.StartCapture,
            RestartCapture = _deviceController.RestartCapture,
            DeviceSelectionModal = _deviceSelectionModal,
            HelpModal = _helpModal,
            GetApplicationMode = () => _visualizerSettings.ApplicationMode,
            OnPaletteCycle = CyclePalette,
            DumpScreen = () => _screenDumpService.DumpToFile(),
            AppSettings = _appSettings
        };
    }

    private GeneralSettingsHubKeyContext CreateGeneralSettingsHubKeyContext(object consoleLock, Action<bool> setModalOpen)
    {
        return new GeneralSettingsHubKeyContext
        {
            SetModalOpen = setModalOpen,
            SaveSettings = () => _settingsPersistence.Save(),
            GetDeviceName = () => _deviceController.CurrentDeviceName,
            StopCapture = _deviceController.StopCapture,
            StartCapture = _deviceController.StartCapture,
            RestartCapture = _deviceController.RestartCapture,
            DeviceSelectionModal = _deviceSelectionModal,
            UiThemeSelectionModal = _uiThemeSelectionModal,
            GetAudioAnalysisSnapshot = () => _engine.GetSnapshot(),
            UiSettings = _uiSettings,
            State = _generalSettingsHubState,
            DisplayState = _displayState,
            Orchestrator = _orchestrator,
            AppSettings = _appSettings,
            ApplyBeatTimingFromSettings = () =>
                _beatTiming.ApplyFromSettings(_appSettings.BpmSource, _deviceController.CurrentDeviceId)
        };
    }

    private void CycleToNextPreset()
    {
        if (_visualizerSettings.Presets is not { Count: > 0 })
        {
            return;
        }

        int currentIndex = 0;
        for (int i = 0; i < _visualizerSettings.Presets.Count; i++)
        {
            if (string.Equals(_visualizerSettings.Presets[i].Id, _visualizerSettings.ActivePresetId, StringComparison.OrdinalIgnoreCase))
            {
                currentIndex = i;
                break;
            }
        }

        int nextIndex = (currentIndex + 1) % _visualizerSettings.Presets.Count;
        var nextPresetInfo = _visualizerSettings.Presets[nextIndex];
        var nextPreset = _presetRepository.GetById(nextPresetInfo.Id);
        if (nextPreset == null)
        {
            return;
        }
        _visualizerSettings.ActivePresetId = nextPreset.Id;
        _visualizerSettings.TextLayers ??= new TextLayersVisualizerSettings();
        _visualizerSettings.TextLayers.CopyFrom(nextPreset.Config);
    }

    private void CyclePalette()
    {
        if (!_renderer.SupportsPaletteCycling())
        {
            return;
        }

        var all = _paletteRepo.GetAll();
        if (all.Count == 0)
        {
            return;
        }

        _visualizerSettings.TextLayers ??= new TextLayersVisualizerSettings();
        string? currentId = _visualizerSettings.TextLayers.PaletteId ?? "";
        int index = 0;
        for (int i = 0; i < all.Count; i++)
        {
            if (string.Equals(all[i].Id, currentId, StringComparison.OrdinalIgnoreCase))
            {
                index = (i + 1) % all.Count;
                break;
            }
        }
        var next = all[index];

        _visualizerSettings.TextLayers.PaletteId = next.Id;

        var def = _paletteRepo.GetById(next.Id);
        if (def != null && ColorPaletteParser.Parse(def) is { } palette && palette.Count > 0)
        {
            var displayName = def.Name?.Trim();
            _renderer.SetPalette(palette, string.IsNullOrEmpty(displayName) ? next.Id : displayName);
        }
        _settingsPersistence.Save();
        if (!_displayState.FullScreen)
        {
            _orchestrator.RedrawWithFullHeader();
        }
        else
        {
            _orchestrator.Redraw();
        }
    }

    [LoggerMessage(EventId = 7640, Level = LogLevel.Error, Message = "Header refresh failed")]
    private partial void LogHeaderRefreshFailed(Exception ex);

    [LoggerMessage(EventId = 7641, Level = LogLevel.Error, Message = "Main loop display tick failed")]
    private partial void LogMainLoopDisplayTickFailed(Exception ex);

    private void Shutdown()
    {
        _headerRefreshCts?.Cancel();
        _headerRefreshCts?.Dispose();
        _headerRefreshCts = null;

        _deviceController.Shutdown();
        System.Console.Clear();
        System.Console.CursorVisible = true;
        System.Console.WriteLine("Recording stopped.");
    }
}
