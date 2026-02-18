using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;

namespace AudioAnalyzer.Console;

/// <summary>Main application shell: orchestrates engine, renderer, header, modals, key handling, and device capture lifecycle.</summary>
internal sealed class ApplicationShell
{
    private readonly IAudioDeviceInfo _deviceInfo;
    private readonly ISettingsRepository _settingsRepo;
    private readonly IVisualizerSettingsRepository _visualizerSettingsRepo;
    private readonly AppSettings _settings;
    private readonly VisualizerSettings _visualizerSettings;
    private readonly IPresetRepository _presetRepository;
    private readonly IShowRepository _showRepository;
    private readonly IPaletteRepository _paletteRepo;
    private readonly AnalysisEngine _engine;
    private readonly IVisualizationRenderer _renderer;
    private readonly INowPlayingProvider _nowPlayingProvider;
    private readonly ShowPlaybackController _showPlaybackController;

    private IAudioInput? _currentInput;
    private string _currentDeviceName = "";
    private readonly object _deviceLock = new();

    public ApplicationShell(
        IAudioDeviceInfo deviceInfo,
        ISettingsRepository settingsRepo,
        IVisualizerSettingsRepository visualizerSettingsRepo,
        AppSettings settings,
        VisualizerSettings visualizerSettings,
        IPresetRepository presetRepository,
        IShowRepository showRepository,
        IPaletteRepository paletteRepo,
        AnalysisEngine engine,
        IVisualizationRenderer renderer,
        INowPlayingProvider nowPlayingProvider)
    {
        _deviceInfo = deviceInfo;
        _settingsRepo = settingsRepo;
        _visualizerSettingsRepo = visualizerSettingsRepo;
        _settings = settings;
        _visualizerSettings = visualizerSettings;
        _presetRepository = presetRepository;
        _showRepository = showRepository;
        _paletteRepo = paletteRepo;
        _engine = engine;
        _renderer = renderer;
        _nowPlayingProvider = nowPlayingProvider;
        _showPlaybackController = new ShowPlaybackController(visualizerSettings, showRepository, presetRepository, engine);
    }

    /// <summary>Runs the main loop. Does not return until the user quits.</summary>
    /// <param name="initialDeviceId">Initial audio device id (or null for loopback).</param>
    /// <param name="initialDeviceName">Display name of the initial device.</param>
    public void Run(string? initialDeviceId, string initialDeviceName)
    {
        _currentDeviceName = initialDeviceName;

        bool modalOpen = false;
        object consoleLock = new();

        string GetModeName() =>
            _visualizerSettings.ApplicationMode == ApplicationMode.ShowPlay ? "Show play" : "Preset editor";
        _engine.SetHeaderCallback(
            () => ConsoleHeader.DrawMain(_currentDeviceName, _nowPlayingProvider.GetNowPlayingText(), GetModeName()),
            () => ConsoleHeader.DrawHeaderOnly(_currentDeviceName, _nowPlayingProvider.GetNowPlayingText(), GetModeName()),
            6);
        _engine.SetRenderGuard(() => !modalOpen);
        _engine.SetConsoleLock(consoleLock);

        StartCapture(initialDeviceId, initialDeviceName);
        ConsoleHeader.DrawMain(_currentDeviceName, _nowPlayingProvider.GetNowPlayingText(),
            _visualizerSettings.ApplicationMode == ApplicationMode.ShowPlay ? "Show play" : "Preset editor");
        _engine.Redraw();

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
                    SaveSettings();
                    _engine.Redraw();
                }
                else
                {
                    switch (key.Key)
                    {
                        case ConsoleKey.Tab:
                            HandleModeSwitch(consoleLock);
                            if (!_engine.FullScreen)
                            {
                                ConsoleHeader.DrawMain(_currentDeviceName, _nowPlayingProvider.GetNowPlayingText(),
                                    _visualizerSettings.ApplicationMode == ApplicationMode.ShowPlay ? "Show play" : "Preset editor");
                            }
                            _engine.Redraw();
                            break;
                        case ConsoleKey.V:
                            if (_visualizerSettings.ApplicationMode == ApplicationMode.PresetEditor)
                            {
                                CycleToNextPreset();
                                SaveSettings();
                                if (!_engine.FullScreen)
                                {
                                    ConsoleHeader.DrawMain(_currentDeviceName, _nowPlayingProvider.GetNowPlayingText(),
                                        _visualizerSettings.ApplicationMode == ApplicationMode.ShowPlay ? "Show play" : "Preset editor");
                                }
                                _engine.Redraw();
                            }
                            break;
                        case ConsoleKey.S:
                            if (_visualizerSettings.ApplicationMode == ApplicationMode.PresetEditor)
                            {
                                SettingsModal.Show(_engine, _visualizerSettings, _presetRepository, _paletteRepo, consoleLock, SaveSettings);
                            }
                            else
                            {
                                ShowEditModal.Show(_engine, _visualizerSettings, _showRepository, _presetRepository, consoleLock, () =>
                                {
                                    SaveSettings();
                                    _visualizerSettingsRepo.SaveVisualizerSettings(_visualizerSettings);
                                });
                            }
                            lock (consoleLock)
                            {
                                if (!_engine.FullScreen)
                                {
                                    ConsoleHeader.DrawMain(_currentDeviceName, _nowPlayingProvider.GetNowPlayingText(),
                                        _visualizerSettings.ApplicationMode == ApplicationMode.ShowPlay ? "Show play" : "Preset editor");
                                }
                                _engine.Redraw();
                            }
                            break;
                        case ConsoleKey.Escape:
                            running = false;
                            break;
                        case ConsoleKey.D:
                            IAudioInput? inputToStop;
                            lock (_deviceLock)
                            {
                                inputToStop = _currentInput;
                            }
                            inputToStop?.StopCapture();

                            var (newId, newName) = DeviceSelectionModal.Show(_deviceInfo, _settingsRepo, _settings, _currentDeviceName, open => modalOpen = open);
                            if (newName != "")
                            {
                                StartCapture(newId, newName);
                            }
                            else
                            {
                                lock (_deviceLock)
                                {
                                    _currentInput?.Start();
                                }
                            }
                            if (!_engine.FullScreen)
                            {
                                ConsoleHeader.DrawMain(_currentDeviceName, _nowPlayingProvider.GetNowPlayingText(),
                                    _visualizerSettings.ApplicationMode == ApplicationMode.ShowPlay ? "Show play" : "Preset editor");
                            }
                            _engine.Redraw();
                            break;
                        case ConsoleKey.H:
                            HelpModal.Show(onEnter: () => modalOpen = true, onClose: () =>
                            {
                                modalOpen = false;
                                if (_engine.FullScreen)
                                {
                                    _engine.Redraw();
                                }
                                else
                                {
                                    ConsoleHeader.DrawMain(_currentDeviceName, _nowPlayingProvider.GetNowPlayingText(),
                                        _visualizerSettings.ApplicationMode == ApplicationMode.ShowPlay ? "Show play" : "Preset editor");
                                    _engine.Redraw();
                                }
                            });
                            break;
                        case ConsoleKey.OemPlus:
                        case ConsoleKey.Add:
                            _engine.BeatSensitivity += 0.1;
                            SaveSettings();
                            break;
                        case ConsoleKey.OemMinus:
                        case ConsoleKey.Subtract:
                            _engine.BeatSensitivity -= 0.1;
                            SaveSettings();
                            break;
                        case ConsoleKey.P:
                            CyclePalette();
                            break;
                        case ConsoleKey.F:
                            _engine.FullScreen = !_engine.FullScreen;
                            if (_engine.FullScreen)
                            {
                                System.Console.Clear();
                                System.Console.CursorVisible = false;
                                _engine.Redraw();
                            }
                            else
                            {
                                ConsoleHeader.DrawMain(_currentDeviceName, _nowPlayingProvider.GetNowPlayingText(),
                                    _visualizerSettings.ApplicationMode == ApplicationMode.ShowPlay ? "Show play" : "Preset editor");
                                _engine.Redraw();
                            }
                            break;
                    }
                }
            }
            Thread.Sleep(50);
        }

        Shutdown();
    }

    private void StartCapture(string? deviceId, string name)
    {
        IAudioInput? oldInput;
        lock (_deviceLock)
        {
            oldInput = _currentInput;
            _currentInput = null;
        }
        oldInput?.StopCapture();
        oldInput?.Dispose();

        lock (_deviceLock)
        {
            _currentInput = _deviceInfo.CreateCapture(deviceId);
            _currentDeviceName = name;
            _currentInput.DataAvailable += (_, e) =>
            {
                lock (_deviceLock)
                {
                    if (_currentInput == null)
                    {
                        return;
                    }

                    _engine.ProcessAudio(e.Buffer, e.BytesRecorded, e.Format);
                }
            };
            _currentInput.Start();
        }
    }

    private void SaveSettings()
    {
        _settings.BeatSensitivity = _engine.BeatSensitivity;
        _settings.BeatCircles = _visualizerSettings.TextLayers?.Layers?.FirstOrDefault(l => l.LayerType == TextLayerType.BeatCircles)?.Enabled ?? true;
        var oscLayer = _visualizerSettings.TextLayers?.Layers?.FirstOrDefault(l => l.LayerType == TextLayerType.Oscilloscope);
        _settings.OscilloscopeGain = oscLayer?.GetCustom<OscilloscopeSettings>()?.Gain ?? 2.5;
        _settingsRepo.SaveAppSettings(_settings);
        _visualizerSettings.TextLayers ??= new TextLayersVisualizerSettings();
        _visualizerSettingsRepo.SaveVisualizerSettings(_visualizerSettings);
    }

    private void HandleModeSwitch(object consoleLock)
    {
        var allShows = _showRepository.GetAll();
        if (allShows.Count == 0)
        {
            ShowEditModal.Show(_engine, _visualizerSettings, _showRepository, _presetRepository, consoleLock, () =>
            {
                SaveSettings();
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
        SaveSettings();
        if (!_engine.FullScreen)
        {
            ConsoleHeader.DrawMain(_currentDeviceName, _nowPlayingProvider.GetNowPlayingText(),
                _visualizerSettings.ApplicationMode == ApplicationMode.ShowPlay ? "Show play" : "Preset editor");
        }
        _engine.Redraw();
    }

    private void Shutdown()
    {
        IAudioInput? toDispose;
        lock (_deviceLock)
        {
            toDispose = _currentInput;
            _currentInput = null;
        }
        toDispose?.StopCapture();
        toDispose?.Dispose();
        System.Console.Clear();
        System.Console.CursorVisible = true;
        System.Console.WriteLine("Recording stopped.");
    }
}
