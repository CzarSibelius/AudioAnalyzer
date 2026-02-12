using System.IO;
using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Infrastructure;
using AudioAnalyzer.Visualizers;
using Microsoft.Extensions.DependencyInjection;

static int GetConsoleWidth()
{
    try { return Console.WindowWidth; }
    catch (IOException) { return 80; }
}

// Load settings before building the renderer so visualizer settings are available for DI
var settingsRepo = new FileSettingsRepository();
var settings = settingsRepo.Load();
settings.VisualizerSettings ??= new VisualizerSettings();
var visualizerSettings = settings.VisualizerSettings;

var services = new ServiceCollection();
services.AddSingleton(visualizerSettings);
services.AddSingleton<IDisplayDimensions, ConsoleDisplayDimensions>();
services.AddSingleton<ISettingsRepository>(_ => settingsRepo);
services.AddSingleton<IPaletteRepository>(_ => new FilePaletteRepository());
services.AddSingleton<IAudioDeviceInfo, NAudioDeviceInfo>();

services.AddSingleton<IVisualizer, SpectrumBarsVisualizer>();
services.AddSingleton<IVisualizer, VuMeterVisualizer>();
services.AddSingleton<IVisualizer, WinampBarsVisualizer>();
services.AddSingleton<IVisualizer>(sp => new OscilloscopeVisualizer(
    sp.GetRequiredService<VisualizerSettings>().Oscilloscope ?? new OscilloscopeVisualizerSettings()));
services.AddSingleton<IVisualizer>(sp => new GeissVisualizer(
    sp.GetRequiredService<VisualizerSettings>().Geiss ?? new GeissVisualizerSettings()));
services.AddSingleton<IVisualizer>(sp => new UnknownPleasuresVisualizer(
    sp.GetRequiredService<VisualizerSettings>().UnknownPleasures));
services.AddSingleton<IVisualizer>(sp => new TextLayersVisualizer(
    sp.GetRequiredService<VisualizerSettings>().TextLayers ?? new TextLayersVisualizerSettings()));

services.AddSingleton<IVisualizationRenderer>(sp =>
{
    var dimensions = sp.GetRequiredService<IDisplayDimensions>();
    var visualizers = sp.GetServices<IVisualizer>();
    return new CompositeVisualizationRenderer(dimensions, visualizers);
});
services.AddSingleton<AnalysisEngine>(sp =>
{
    var renderer = sp.GetRequiredService<IVisualizationRenderer>();
    var dimensions = sp.GetRequiredService<IDisplayDimensions>();
    return new AnalysisEngine(renderer, dimensions);
});

var provider = services.BuildServiceProvider();
var paletteRepo = provider.GetRequiredService<IPaletteRepository>();
var deviceInfo = provider.GetRequiredService<IAudioDeviceInfo>();
var engine = provider.GetRequiredService<AnalysisEngine>();
var renderer = provider.GetRequiredService<IVisualizationRenderer>();

const string CapturePrefix = "capture:";
const string LoopbackPrefix = "loopback:";

static (string? deviceId, string name) TryResolveDeviceFromSettings(IReadOnlyList<AudioDeviceEntry> devices, AppSettings settings)
{
    if (devices.Count == 0)
    {
        return (null, "");
    }

    if (settings.InputMode == "loopback" && string.IsNullOrEmpty(settings.DeviceName))
    {
        var first = devices[0];
        return (first.Id, first.Name);
    }

    if (settings.InputMode == "device" && !string.IsNullOrEmpty(settings.DeviceName))
    {
        var captureId = CapturePrefix + settings.DeviceName;
        var loopbackId = LoopbackPrefix + settings.DeviceName;
        foreach (var d in devices)
        {
            if (d.Id == captureId || d.Id == loopbackId)
            {
                return (d.Id, d.Name);
            }
        }
    }

    return (null, "");
}

VisualizationMode ParseMode(string? mode)
{
    return renderer.GetModeFromTechnicalName(mode ?? "") ?? VisualizationMode.SpectrumBars;
}

void ResolveAndSetPaletteForMode(VisualizationMode mode, AppSettings appSettings, IPaletteRepository repo, IVisualizationRenderer visRenderer)
{
    IReadOnlyList<PaletteColor>? palette;
    string? displayName;
    string? paletteId = null;

    switch (mode)
    {
        case VisualizationMode.Geiss:
            paletteId = appSettings.VisualizerSettings?.Geiss?.PaletteId;
            break;
        case VisualizationMode.UnknownPleasures:
            paletteId = appSettings.VisualizerSettings?.UnknownPleasures?.PaletteId;
            if (string.IsNullOrWhiteSpace(paletteId))
            {
                var legacyPalette = ColorPaletteParser.Parse(appSettings.VisualizerSettings?.UnknownPleasures?.Palette);
                if (legacyPalette != null && legacyPalette.Count > 0)
                {
                    visRenderer.SetPaletteForMode(mode, legacyPalette, "Custom");
                    return;
                }
            }
            break;
        case VisualizationMode.TextLayers:
            paletteId = appSettings.VisualizerSettings?.TextLayers?.PaletteId;
            break;
        default:
            return;
    }

    if (!string.IsNullOrWhiteSpace(paletteId))
    {
        var def = repo.GetById(paletteId);
        if (def != null && (palette = ColorPaletteParser.Parse(def)) != null && palette.Count > 0)
        {
            displayName = def.Name?.Trim();
            visRenderer.SetPaletteForMode(mode, palette, string.IsNullOrEmpty(displayName) ? paletteId : displayName);
            return;
        }
    }

    palette = ColorPaletteParser.DefaultPalette;
    displayName = "Default";
    visRenderer.SetPaletteForMode(mode, palette, displayName);
}

engine.SetVisualizationMode(ParseMode(settings.VisualizationMode));
engine.BeatSensitivity = settings.BeatSensitivity;
ResolveAndSetPaletteForMode(VisualizationMode.Geiss, settings, paletteRepo, renderer);
ResolveAndSetPaletteForMode(VisualizationMode.UnknownPleasures, settings, paletteRepo, renderer);
ResolveAndSetPaletteForMode(VisualizationMode.TextLayers, settings, paletteRepo, renderer);

var devices = deviceInfo.GetDevices();
var (initialDeviceId, initialName) = TryResolveDeviceFromSettings(devices, settings);
if (initialName == "")
{
    (initialDeviceId, initialName) = ShowDeviceSelectionMenu(deviceInfo, settingsRepo, settings, null, _ => { });
}

if (initialName == "")
{
    Console.WriteLine("No device selected.");
    return;
}

IAudioInput? currentInput = null;
string currentDeviceName = initialName;
object deviceLock = new();

engine.SetHeaderCallback(() => DrawMainUI(currentDeviceName), () => DrawHeaderOnly(currentDeviceName), 6);
bool modalOpen = false;
engine.SetRenderGuard(() => !modalOpen);

void StartCapture(string? deviceId, string name)
{
    lock (deviceLock)
    {
        currentInput?.StopCapture();
        currentInput?.Dispose();
        currentInput = deviceInfo.CreateCapture(deviceId);
        currentDeviceName = name;
        currentInput.DataAvailable += (_, e) =>
        {
            lock (deviceLock)
            {
                if (currentInput == null)
                {
                    return;
                }

                engine.ProcessAudio(e.Buffer, e.BytesRecorded, e.Format);
            }
        };
        currentInput.Start();
    }
}

StartCapture(initialDeviceId, initialName);
DrawMainUI(currentDeviceName);
engine.Redraw();

void SaveSettingsToRepository()
{
    settings.VisualizationMode = renderer.GetTechnicalName(engine.CurrentMode);
    settings.BeatSensitivity = engine.BeatSensitivity;
    settings.VisualizerSettings ??= new VisualizerSettings();
    settings.VisualizerSettings.Geiss ??= new GeissVisualizerSettings();
    settings.VisualizerSettings.Oscilloscope ??= new OscilloscopeVisualizerSettings();
    settings.VisualizerSettings.UnknownPleasures ??= new UnknownPleasuresVisualizerSettings();
    settings.VisualizerSettings.TextLayers ??= new TextLayersVisualizerSettings();
    settings.OscilloscopeGain = settings.VisualizerSettings.Oscilloscope.Gain;
    settings.BeatCircles = settings.VisualizerSettings.Geiss.BeatCircles;
    settingsRepo.Save(settings);
}

bool running = true;
while (running)
{
    if (Console.KeyAvailable)
    {
        var key = Console.ReadKey(true);
        if (renderer.HandleKey(key.Key, engine.CurrentMode))
        {
            engine.Redraw();
        }
        else
        {
            switch (key.Key)
            {
            case ConsoleKey.Escape:
                running = false;
                break;
            case ConsoleKey.D:
                lock (deviceLock)
                {
                    currentInput?.StopCapture();
                }

                var (newId, newName) = ShowDeviceSelectionMenu(deviceInfo, settingsRepo, settings, currentDeviceName, open => modalOpen = open);
                if (newName != "")
                {
                    StartCapture(newId, newName);
                }
                else
                {
                    lock (deviceLock)
                    {
                        currentInput?.Start();
                    }
                }
                if (!engine.FullScreen)
                {
                    DrawMainUI(currentDeviceName);
                }
                engine.Redraw();
                break;
            case ConsoleKey.H:
                RunModal(DrawHelpContent, _ => true, onEnter: () => modalOpen = true, onClose: () =>
                {
                    modalOpen = false;
                    if (engine.FullScreen)
                    {
                        engine.Redraw();
                    }
                    else
                    {
                        DrawMainUI(currentDeviceName);
                        engine.Redraw();
                    }
                });
                break;
            case ConsoleKey.V:
                engine.NextVisualizationMode();
                SaveSettingsToRepository();
                if (!engine.FullScreen)
                {
                    DrawMainUI(currentDeviceName);
                }
                engine.Redraw();
                break;
            case ConsoleKey.OemPlus:
            case ConsoleKey.Add:
                engine.BeatSensitivity += 0.1;
                SaveSettingsToRepository();
                break;
            case ConsoleKey.OemMinus:
            case ConsoleKey.Subtract:
                engine.BeatSensitivity -= 0.1;
                SaveSettingsToRepository();
                break;
            case ConsoleKey.B:
                visualizerSettings.Geiss ??= new GeissVisualizerSettings();
                visualizerSettings.Geiss.BeatCircles = !visualizerSettings.Geiss.BeatCircles;
                SaveSettingsToRepository();
                break;
            case ConsoleKey.Oem4:   // [ (increase gain)
                visualizerSettings.Oscilloscope ??= new OscilloscopeVisualizerSettings();
                visualizerSettings.Oscilloscope.Gain = Math.Clamp(visualizerSettings.Oscilloscope.Gain + 0.5, 1.0, 10.0);
                SaveSettingsToRepository();
                engine.Redraw();
                break;
            case ConsoleKey.Oem6:   // ] (decrease gain)
                visualizerSettings.Oscilloscope ??= new OscilloscopeVisualizerSettings();
                visualizerSettings.Oscilloscope.Gain = Math.Clamp(visualizerSettings.Oscilloscope.Gain - 0.5, 1.0, 10.0);
                SaveSettingsToRepository();
                engine.Redraw();
                break;
            case ConsoleKey.P:
                CyclePalette();
                break;
            case ConsoleKey.F:
                engine.FullScreen = !engine.FullScreen;
                if (engine.FullScreen)
                {
                    Console.Clear();
                    Console.CursorVisible = false;
                    engine.Redraw();
                }
                else
                {
                    DrawMainUI(currentDeviceName);
                    engine.Redraw();
                }
                break;
            }
        }
    }
    Thread.Sleep(50);
}

void CyclePalette()
{
    if (!renderer.SupportsPaletteCycling(engine.CurrentMode))
    {
        return;
    }

    var all = paletteRepo.GetAll();
    if (all.Count == 0)
    {
        return;
    }

    string? currentId = null;
    switch (engine.CurrentMode)
    {
        case VisualizationMode.Geiss:
            visualizerSettings.Geiss ??= new GeissVisualizerSettings();
            currentId = visualizerSettings.Geiss.PaletteId;
            break;
        case VisualizationMode.UnknownPleasures:
            visualizerSettings.UnknownPleasures ??= new UnknownPleasuresVisualizerSettings();
            currentId = visualizerSettings.UnknownPleasures.PaletteId;
            break;
        case VisualizationMode.TextLayers:
            visualizerSettings.TextLayers ??= new TextLayersVisualizerSettings();
            currentId = visualizerSettings.TextLayers.PaletteId;
            break;
        default:
            return;
    }

    currentId ??= "";
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

    switch (engine.CurrentMode)
    {
        case VisualizationMode.Geiss:
            visualizerSettings.Geiss!.PaletteId = next.Id;
            break;
        case VisualizationMode.UnknownPleasures:
            visualizerSettings.UnknownPleasures!.PaletteId = next.Id;
            break;
        case VisualizationMode.TextLayers:
            visualizerSettings.TextLayers!.PaletteId = next.Id;
            break;
    }

    var def = paletteRepo.GetById(next.Id);
    if (def != null && ColorPaletteParser.Parse(def) is { } palette && palette.Count > 0)
    {
        var displayName = def.Name?.Trim();
        renderer.SetPaletteForMode(engine.CurrentMode, palette, string.IsNullOrEmpty(displayName) ? next.Id : displayName);
    }
    SaveSettingsToRepository();
    if (!engine.FullScreen)
    {
        DrawMainUI(currentDeviceName);
    }
    engine.Redraw();
}

lock (deviceLock)
{
    currentInput?.StopCapture();
    currentInput?.Dispose();
    currentInput = null;
}
Console.Clear();
Console.CursorVisible = true;
Console.WriteLine("Recording stopped.");

void DrawMainUI(string deviceName)
{
    try
    {
        if (OperatingSystem.IsWindows())
        {
            int w = Console.WindowWidth;
            int h = Math.Max(15, Console.WindowHeight);
            if (w >= 10 && h >= 15)
            {
                Console.BufferWidth = w;
                Console.BufferHeight = h;
            }
        }
    }
    catch { /* Buffer size not supported */ }

    Console.Clear();
    Console.CursorVisible = false;
    DrawHeaderOnly(deviceName);
}

void DrawHeaderOnly(string deviceName)
{
    int width = Math.Max(10, GetConsoleWidth());
    string title = " AUDIO ANALYZER - Real-time Frequency Spectrum ";
    title = VisualizerViewport.TruncateToWidth(title, width - 2);
    int padding = Math.Max(0, (width - title.Length - 2) / 2);
    string line1 = VisualizerViewport.TruncateToWidth("╔" + new string('═', width - 2) + "╗", width).PadRight(width);
    string line2 = VisualizerViewport.TruncateToWidth("║" + new string(' ', padding) + title + new string(' ', width - padding - title.Length - 2) + "║", width).PadRight(width);
    string line3 = VisualizerViewport.TruncateToWidth("╚" + new string('═', width - 2) + "╝", width).PadRight(width);
    string line4 = VisualizerViewport.TruncateToWidth($"Input: {deviceName}", width).PadRight(width);
    string line5 = VisualizerViewport.TruncateToWidth("Press H for help, D device, F full screen, ESC quit", width).PadRight(width);
    string line6 = new string(' ', width);
    try
    {
        Console.SetCursorPosition(0, 0);
        Console.Write(line1);
        Console.SetCursorPosition(0, 1);
        Console.Write(line2);
        Console.SetCursorPosition(0, 2);
        Console.Write(line3);
        Console.SetCursorPosition(0, 3);
        Console.Write(line4);
        Console.SetCursorPosition(0, 4);
        Console.Write(line5);
        Console.SetCursorPosition(0, 5);
        Console.Write(line6);
    }
    catch { }
}

// Modal system per ADR-0006: dialogs drawn on top, capture input until closed, dismiss by key, on close run onClose to restore base view.
// When onEnter is set, it is called when the modal opens so the console layer can suppress rendering (e.g. engine render guard).
static void RunModal(Action drawContent, Func<ConsoleKeyInfo, bool> handleKey, Action? onClose = null, Action? onEnter = null)
{
    Console.CursorVisible = false;
    onEnter?.Invoke();
    while (true)
    {
        Console.Clear();
        drawContent();
        var key = Console.ReadKey(true);
        if (handleKey(key))
        {
            break;
        }
    }
    onClose?.Invoke();
}

// Draws help content only; does not clear or read input. Used by RunModal (ADR-0006).
void DrawHelpContent()
{
    int width = GetConsoleWidth();
    string title = " HELP ";
    int pad = Math.Max(0, (width - title.Length - 2) / 2);
    Console.WriteLine("╔" + new string('═', width - 2) + "╗");
    Console.WriteLine("║" + new string(' ', pad) + title + new string(' ', width - pad - title.Length - 2) + "║");
    Console.WriteLine("╚" + new string('═', width - 2) + "╝");
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("  KEYBOARD CONTROLS");
    Console.ResetColor();
    Console.WriteLine("  ─────────────────────────────────────");
    Console.WriteLine("  H         Show this help menu");
    Console.WriteLine("  V         Change visualization mode");
    Console.WriteLine("  P         Cycle color palette (palette-aware visualizers)");
    Console.WriteLine("  B         Toggle beat circles (Geiss mode)");
    Console.WriteLine("  +/-       Adjust beat sensitivity");
    Console.WriteLine("  [ / ]     Adjust oscilloscope gain (Oscilloscope mode)");
    Console.WriteLine("  D         Change audio input device");
    Console.WriteLine("  ESC       Quit the application");
    Console.WriteLine("  F         Toggle full screen (visualizer only, no header/toolbar)");
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("  DEVICE SELECTION MENU");
    Console.ResetColor();
    Console.WriteLine("  ─────────────────────────────────────");
    Console.WriteLine("  ↑/↓       Navigate devices");
    Console.WriteLine("  ENTER     Select device");
    Console.WriteLine("  ESC       Cancel and return");
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("  VISUALIZATION MODES (press V to cycle)");
    Console.ResetColor();
    Console.WriteLine("  ─────────────────────────────────────");
    string Desc(VisualizationMode m) => m switch
    {
        VisualizationMode.SpectrumBars => "Frequency bars with peak hold",
        VisualizationMode.Oscilloscope => "Waveform display ( [ ] = gain)",
        VisualizationMode.VuMeter => "Classic stereo level meters",
        VisualizationMode.WinampBars => "Classic music player bars",
        VisualizationMode.Geiss => "Psychedelic plasma visualization",
        VisualizationMode.UnknownPleasures => "Stacked waveform snapshots",
        VisualizationMode.TextLayers => "Layered text (1–9 = switch layer text)",
        _ => ""
    };
    foreach (VisualizationMode mode in Enum.GetValues<VisualizationMode>())
    {
        string desc = Desc(mode);
        if (renderer.SupportsPaletteCycling(mode))
        {
            desc += " (P = cycle palette)";
        }

        Console.WriteLine($"  {renderer.GetDisplayName(mode),-22} {desc}");
    }
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine("  Press any key to return...");
    Console.ResetColor();
}

// Device selection as a modal per ADR-0006; uses RunModal with draw + handleKey, returns selection via out state.
// setModalOpen is called with true when the modal opens and false when it closes, so the engine can skip rendering while the modal is visible.
static (string? deviceId, string name) ShowDeviceSelectionMenu(IAudioDeviceInfo deviceInfo, ISettingsRepository settingsRepo, AppSettings settings, string? currentDeviceName, Action<bool> setModalOpen)
{
    var devices = deviceInfo.GetDevices();
    if (devices.Count == 0)
    {
        Console.WriteLine("No audio devices found!");
        return (null, "");
    }

    int selectedIndex = 0;
    if (!string.IsNullOrEmpty(currentDeviceName))
    {
        for (int i = 0; i < devices.Count; i++)
        {
            if (devices[i].Name == currentDeviceName) { selectedIndex = i; break; }
        }
    }
    else if (settings.InputMode == "loopback")
    {
        selectedIndex = 0;
    }
    else if (!string.IsNullOrEmpty(settings.DeviceName))
    {
        for (int i = 0; i < devices.Count; i++)
        {
            if (devices[i].Name.Contains(settings.DeviceName, StringComparison.OrdinalIgnoreCase))
            {
                selectedIndex = i;
                break;
            }
        }
    }

    string? resultId = null;
    string resultName = "";

    void DrawDeviceContent()
    {
        int width = GetConsoleWidth();
        string title = " SELECT AUDIO INPUT ";
        int pad = Math.Max(0, (width - title.Length - 2) / 2);
        Console.WriteLine("╔" + new string('═', width - 2) + "╗");
        Console.WriteLine("║" + new string(' ', pad) + title + new string(' ', width - pad - title.Length - 2) + "║");
        Console.WriteLine("╚" + new string('═', width - 2) + "╝");
        Console.WriteLine();
        Console.WriteLine("  Use ↑/↓ to select, ENTER to confirm, ESC to cancel");
        Console.WriteLine();

        for (int i = 0; i < devices.Count; i++)
        {
            bool isCurrent = currentDeviceName != null && devices[i].Name == currentDeviceName;
            if (i == selectedIndex)
            {
                Console.BackgroundColor = ConsoleColor.White;
                Console.ForegroundColor = ConsoleColor.Black;
            }
            else if (isCurrent)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
            }

            string prefix = i == selectedIndex ? " ► " : "   ";
            string suffix = isCurrent ? " (current)" : "";
            string line = $"{prefix}{devices[i].Name}{suffix}";
            if (line.Length < width - 1)
            {
                line = line.PadRight(width - 1);
            }
            else
            {
                line = line[..(width - 1)];
            }

            Console.WriteLine(line);
            Console.ResetColor();
        }
        Console.WriteLine(new string(' ', width - 1));
    }

    bool HandleDeviceKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                selectedIndex = (selectedIndex - 1 + devices.Count) % devices.Count;
                return false;
            case ConsoleKey.DownArrow:
                selectedIndex = (selectedIndex + 1) % devices.Count;
                return false;
            case ConsoleKey.Enter:
                var selected = devices[selectedIndex];
                settings.InputMode = selected.Id == null ? "loopback" : "device";
                if (selected.Id != null)
                {
                    if (selected.Id.StartsWith("capture:", StringComparison.Ordinal))
                    {
                        settings.DeviceName = selected.Id.Substring(8);
                    }
                    else if (selected.Id.StartsWith("loopback:", StringComparison.Ordinal))
                    {
                        settings.DeviceName = selected.Id.Substring(9);
                    }
                    else
                    {
                        settings.DeviceName = selected.Id;
                    }
                }
                else
                {
                    settings.DeviceName = null;
                }
                settingsRepo.Save(settings);
                resultId = selected.Id;
                resultName = selected.Name;
                return true;
            case ConsoleKey.Escape:
                return true;
            default:
                return false;
        }
    }

    RunModal(DrawDeviceContent, HandleDeviceKey, onClose: () => setModalOpen(false), onEnter: () => setModalOpen(true));
    return (resultId, resultName);
}
