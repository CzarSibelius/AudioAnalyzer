using System.Diagnostics.CodeAnalysis;
using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Palette;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;

namespace AudioAnalyzer.Console;

/// <summary>Main application shell: orchestrates engine, renderer, header, modals, key handling, and device capture lifecycle.</summary>
[SuppressMessage("Reliability", "CA1001:Types that own disposable fields should be disposable", Justification = "Single Run/Shutdown lifecycle; _headerRefreshCts explicitly disposed in Shutdown().")]
internal sealed class ApplicationShell
{
    private readonly IDeviceCaptureController _deviceController;
    private readonly IVisualizerSettingsRepository _visualizerSettingsRepo;
    private readonly VisualizerSettings _visualizerSettings;
    private readonly IPresetRepository _presetRepository;
    private readonly IShowRepository _showRepository;
    private readonly IPaletteRepository _paletteRepo;
    private readonly AnalysisEngine _engine;
    private readonly IVisualizationOrchestrator _orchestrator;
    private readonly IVisualizationRenderer _renderer;
    private readonly ShowPlaybackController _showPlaybackController;
    private readonly IHeaderDrawer _headerDrawer;
    private readonly IKeyHandler<MainLoopKeyContext> _keyHandler;
    private readonly IAppSettingsPersistence _settingsPersistence;
    private readonly IDeviceSelectionModal _deviceSelectionModal;
    private readonly IHelpModal _helpModal;
    private readonly ISettingsModal _settingsModal;
    private readonly IShowEditModal _showEditModal;

    private CancellationTokenSource? _headerRefreshCts;

    public ApplicationShell(
        IDeviceCaptureController deviceController,
        IVisualizerSettingsRepository visualizerSettingsRepo,
        VisualizerSettings visualizerSettings,
        IPresetRepository presetRepository,
        IShowRepository showRepository,
        IPaletteRepository paletteRepo,
        AnalysisEngine engine,
        IVisualizationOrchestrator orchestrator,
        IVisualizationRenderer renderer,
        ShowPlaybackController showPlaybackController,
        IHeaderDrawer headerDrawer,
        IKeyHandler<MainLoopKeyContext> keyHandler,
        IAppSettingsPersistence settingsPersistence,
        IDeviceSelectionModal deviceSelectionModal,
        IHelpModal helpModal,
        ISettingsModal settingsModal,
        IShowEditModal showEditModal)
    {
        _deviceController = deviceController ?? throw new ArgumentNullException(nameof(deviceController));
        _visualizerSettingsRepo = visualizerSettingsRepo;
        _visualizerSettings = visualizerSettings;
        _presetRepository = presetRepository;
        _showRepository = showRepository;
        _paletteRepo = paletteRepo;
        _engine = engine;
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _renderer = renderer;
        _showPlaybackController = showPlaybackController ?? throw new ArgumentNullException(nameof(showPlaybackController));
        _headerDrawer = headerDrawer ?? throw new ArgumentNullException(nameof(headerDrawer));
        _keyHandler = keyHandler ?? throw new ArgumentNullException(nameof(keyHandler));
        _settingsPersistence = settingsPersistence ?? throw new ArgumentNullException(nameof(settingsPersistence));
        _deviceSelectionModal = deviceSelectionModal ?? throw new ArgumentNullException(nameof(deviceSelectionModal));
        _helpModal = helpModal ?? throw new ArgumentNullException(nameof(helpModal));
        _settingsModal = settingsModal ?? throw new ArgumentNullException(nameof(settingsModal));
        _showEditModal = showEditModal ?? throw new ArgumentNullException(nameof(showEditModal));
    }

    /// <summary>Runs the main loop. Does not return until the user quits.</summary>
    /// <param name="initialDeviceId">Initial audio device id (or null for loopback).</param>
    /// <param name="initialDeviceName">Display name of the initial device.</param>
    public void Run(string? initialDeviceId, string initialDeviceName)
    {
        bool modalOpen = false;
        object consoleLock = new();

        _orchestrator.SetHeaderCallback(
            () => _headerDrawer.DrawMain(_deviceController.CurrentDeviceName),
            () => _headerDrawer.DrawHeaderOnly(_deviceController.CurrentDeviceName),
            6);
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
                        _ = ex; /* Header refresh failed: swallow to avoid background task crash */
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

        bool running = true;
        while (running)
        {
            if (_visualizerSettings.ApplicationMode == ApplicationMode.ShowPlay)
            {
                _showPlaybackController.Tick();
            }

            if (System.Console.KeyAvailable)
            {
                var key = System.Console.ReadKey(true);
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
            Thread.Sleep(50);
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
            Orchestrator = _orchestrator,
            Engine = _engine,
            HeaderDrawer = _headerDrawer,
            OnModeSwitch = () => HandleModeSwitch(consoleLock),
            OnPresetCycle = CycleToNextPreset,
            SettingsModal = _settingsModal,
            ShowEditModal = _showEditModal,
            StopCapture = _deviceController.StopCapture,
            StartCapture = _deviceController.StartCapture,
            RestartCapture = _deviceController.RestartCapture,
            DeviceSelectionModal = _deviceSelectionModal,
            HelpModal = _helpModal,
            GetApplicationMode = () => _visualizerSettings.ApplicationMode,
            OnPaletteCycle = CyclePalette
        };
    }

    private void HandleModeSwitch(object consoleLock)
    {
        var allShows = _showRepository.GetAll();
        if (allShows.Count == 0)
        {
            _showEditModal.Show(consoleLock, () =>
            {
                _settingsPersistence.Save();
                _visualizerSettingsRepo.SaveVisualizerSettings(_visualizerSettings);
            });
            allShows = _showRepository.GetAll();
            if (allShows.Count == 0)
            {
                return;
            }
        }

        if (_visualizerSettings.ApplicationMode == ApplicationMode.PresetEditor)
        {
            var showId = _visualizerSettings.ActiveShowId ?? allShows[0].Id;
            var show = _showRepository.GetById(showId);
            if (show == null)
            {
                showId = allShows[0].Id;
                show = _showRepository.GetById(showId);
            }
            if (show != null && show.Entries is { Count: > 0 })
            {
                _visualizerSettings.ApplicationMode = ApplicationMode.ShowPlay;
                _visualizerSettings.ActiveShowId = showId;
                _visualizerSettings.ActiveShowName = show.Name?.Trim();
                _showPlaybackController.Reset();
                _showPlaybackController.LoadCurrentEntry();
                _visualizerSettingsRepo.SaveVisualizerSettings(_visualizerSettings);
            }
        }
        else
        {
            _visualizerSettings.ApplicationMode = ApplicationMode.PresetEditor;
            _visualizerSettings.ActiveShowId = null;
            _visualizerSettings.ActiveShowName = null;
            _visualizerSettingsRepo.SaveVisualizerSettings(_visualizerSettings);
        }
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
        if (!_orchestrator.FullScreen)
        {
            _orchestrator.RedrawWithFullHeader();
        }
        else
        {
            _orchestrator.Redraw();
        }
    }

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
