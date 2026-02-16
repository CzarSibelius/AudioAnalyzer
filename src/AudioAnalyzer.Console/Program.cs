using System.IO;
using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Console;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

static int GetConsoleWidth()
{
    try { return Console.WindowWidth; }
    catch (IOException) { return 80; }
}

// Load settings before building the renderer so visualizer settings are available for DI
var settingsRepo = new FileSettingsRepository();
var settings = settingsRepo.LoadAppSettings();
var visualizerSettings = settingsRepo.LoadVisualizerSettings();

var provider = ServiceConfiguration.Build(settingsRepo, settings, visualizerSettings);
var paletteRepo = provider.GetRequiredService<IPaletteRepository>();
var deviceInfo = provider.GetRequiredService<IAudioDeviceInfo>();
var engine = provider.GetRequiredService<AnalysisEngine>();
var renderer = provider.GetRequiredService<IVisualizationRenderer>();

engine.BeatSensitivity = settings.BeatSensitivity;

var devices = deviceInfo.GetDevices();
var (initialDeviceId, initialName) = DeviceResolver.TryResolveFromSettings(devices, settings);
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
object consoleLock = new();
engine.SetConsoleLock(consoleLock);

void StartCapture(string? deviceId, string name)
{
    IAudioInput? oldInput;
    lock (deviceLock)
    {
        oldInput = currentInput;
        currentInput = null;
    }
    oldInput?.StopCapture();
    oldInput?.Dispose();

    lock (deviceLock)
    {
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
    settings.BeatSensitivity = engine.BeatSensitivity;
    settings.BeatCircles = visualizerSettings.TextLayers?.Layers?.FirstOrDefault(l => l.LayerType == TextLayerType.BeatCircles)?.Enabled ?? true;
    settings.OscilloscopeGain = visualizerSettings.TextLayers?.Layers?.FirstOrDefault(l => l.LayerType == TextLayerType.Oscilloscope)?.Gain ?? 2.5;
    settingsRepo.SaveAppSettings(settings);
    visualizerSettings.TextLayers ??= new TextLayersVisualizerSettings();
    settingsRepo.SaveVisualizerSettings(visualizerSettings);
}

bool running = true;
while (running)
{
    if (Console.KeyAvailable)
    {
        var key = Console.ReadKey(true);
        if (renderer.HandleKey(key))
        {
            SaveSettingsToRepository();
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
                    IAudioInput? inputToStop;
                    lock (deviceLock)
                    {
                        inputToStop = currentInput;
                    }
                    inputToStop?.StopCapture();

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
                    CycleToNextPreset();
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
                case ConsoleKey.S:
                    ShowTextLayersSettingsModal(engine, visualizerSettings, consoleLock, SaveSettingsToRepository);
                    lock (consoleLock)
                    {
                        if (!engine.FullScreen)
                        {
                            DrawMainUI(currentDeviceName);
                        }
                        engine.Redraw();
                    }
                    break;
            }
        }
    }
    Thread.Sleep(50);
}

void CycleToNextPreset()
{
    if (visualizerSettings.Presets is not { Count: > 0 })
    {
        return;
    }

    int currentIndex = 0;
    for (int i = 0; i < visualizerSettings.Presets.Count; i++)
    {
        if (string.Equals(visualizerSettings.Presets[i].Id, visualizerSettings.ActivePresetId, StringComparison.OrdinalIgnoreCase))
        {
            currentIndex = i;
            break;
        }
    }

    int nextIndex = (currentIndex + 1) % visualizerSettings.Presets.Count;
    var nextPreset = visualizerSettings.Presets[nextIndex];
    visualizerSettings.ActivePresetId = nextPreset.Id;
    visualizerSettings.TextLayers ??= new TextLayersVisualizerSettings();
    visualizerSettings.TextLayers.CopyFrom(nextPreset.Config);
}

void CyclePalette()
{
    if (!renderer.SupportsPaletteCycling())
    {
        return;
    }

    var all = paletteRepo.GetAll();
    if (all.Count == 0)
    {
        return;
    }

    visualizerSettings.TextLayers ??= new TextLayersVisualizerSettings();
    string? currentId = visualizerSettings.TextLayers.PaletteId ?? "";
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

    visualizerSettings.TextLayers.PaletteId = next.Id;

    var def = paletteRepo.GetById(next.Id);
    if (def != null && ColorPaletteParser.Parse(def) is { } palette && palette.Count > 0)
    {
        var displayName = def.Name?.Trim();
        renderer.SetPalette(palette, string.IsNullOrEmpty(displayName) ? next.Id : displayName);
    }
    SaveSettingsToRepository();
    if (!engine.FullScreen)
    {
        DrawMainUI(currentDeviceName);
    }
    engine.Redraw();
}

IAudioInput? toDispose;
lock (deviceLock)
{
    toDispose = currentInput;
    currentInput = null;
}
toDispose?.StopCapture();
toDispose?.Dispose();
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
    catch (Exception ex) { _ = ex; /* Buffer size not supported: swallow to avoid crash */ }

    Console.Clear();
    Console.CursorVisible = false;
    DrawHeaderOnly(deviceName);
}

void DrawHeaderOnly(string deviceName)
{
    int width = Math.Max(10, GetConsoleWidth());
    string title = " AUDIO ANALYZER - Real-time Frequency Spectrum ";
    title = VisualizerViewport.TruncateWithEllipsis(title, width - 2);
    int padding = Math.Max(0, (width - title.Length - 2) / 2);
    string line1 = VisualizerViewport.TruncateToWidth("╔" + new string('═', width - 2) + "╗", width).PadRight(width);
    string line2 = VisualizerViewport.TruncateToWidth("║" + new string(' ', padding) + title + new string(' ', width - padding - title.Length - 2) + "║", width).PadRight(width);
    string line3 = VisualizerViewport.TruncateToWidth("╚" + new string('═', width - 2) + "╝", width).PadRight(width);
    string line4 = VisualizerViewport.TruncateWithEllipsis($"Input: {deviceName}", width).PadRight(width);
    string line5 = VisualizerViewport.TruncateWithEllipsis("Press H for help, D device, F full screen, ESC quit", width).PadRight(width);
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
    catch (Exception ex) { _ = ex; /* Console write unavailable: swallow to avoid crash */ }
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

// Overlay modal: does NOT clear the whole screen; draws only into the top overlayRowCount rows.
// Leaves the visualizer area visible below. Same lifecycle as RunModal (onEnter, onClose).
// consoleLock: when set, acquired during clear+draw so overlay and engine render don't interleave on the console.
static void RunOverlayModal(int overlayRowCount, Action drawContent, Func<ConsoleKeyInfo, bool> handleKey, object? consoleLock = null, Action? onClose = null, Action? onEnter = null)
{
    Console.CursorVisible = false;
    onEnter?.Invoke();
    while (true)
    {
        void ClearAndDraw()
        {
            int width = GetConsoleWidth();
            string blank = new string(' ', width);
            for (int r = 0; r < overlayRowCount; r++)
            {
                try
                {
                    Console.SetCursorPosition(0, r);
                    Console.Write(blank);
                }
                catch (Exception ex) { _ = ex; /* Console write failed in overlay clear */ }
            }
            drawContent();
        }

        if (consoleLock != null)
        {
            lock (consoleLock)
            {
                ClearAndDraw();
            }
        }
        else
        {
            ClearAndDraw();
        }

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
    Console.WriteLine("  V         Cycle to next preset");
    Console.WriteLine("  P         Cycle color palette (palette-aware visualizers)");
    Console.WriteLine("  +/-       Adjust beat sensitivity");
    Console.WriteLine("  [ / ]     Adjust oscilloscope gain (Layered text, when Oscilloscope layer selected)");
    Console.WriteLine("  D         Change audio input device");
    Console.WriteLine("  S         TextLayers settings modal (Layered text mode; ↑/↓ select, ESC close)");
    Console.WriteLine("  ESC       Quit the application");
    Console.WriteLine("  F         Toggle full screen (visualizer only, no header/toolbar)");
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("  PRESET SETTINGS MODAL (S)");
    Console.ResetColor();
    Console.WriteLine("  ─────────────────────────────────────");
    Console.WriteLine("  1-9       Select layer");
    Console.WriteLine("  \u2190\u2192       Change layer type");
    Console.WriteLine("  Shift+1-9 Toggle layer enabled/disabled");
    Console.WriteLine("  \u2191\u2193       Select layer (alternate)");
    Console.WriteLine("  R         Rename preset");
    Console.WriteLine("  N         New preset (duplicate of current)");
    Console.WriteLine("  ESC       Close modal");
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
    Console.WriteLine("  PRESETS (press V to cycle)");
    Console.ResetColor();
    Console.WriteLine("  ─────────────────────────────────────");
    Console.WriteLine("  Each preset is a named TextLayers configuration (9 layers + palette).");
    Console.WriteLine("  Edit layers with S; presets saved automatically.");
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
                settingsRepo.SaveAppSettings(settings);
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

const int TextLayersSettingsOverlayRows = 14;

static void ShowTextLayersSettingsModal(AnalysisEngine analysisEngine, VisualizerSettings visualizerSettings, object consoleLock, Action saveSettings)
{
    var textLayers = visualizerSettings.TextLayers ?? new TextLayersVisualizerSettings();
    var layers = textLayers.Layers ?? new List<TextLayerSettings>();
    var sortedLayers = layers.OrderBy(l => l.ZOrder).ToList();
    if (sortedLayers.Count == 0)
    {
        sortedLayers = new List<TextLayerSettings>();
    }

    int selectedIndex = 0;
    bool renaming = false;
    string renameBuffer = "";
    const int LeftColWidth = 28;
    int width = GetConsoleWidth();
    int rightColWidth = Math.Max(10, width - LeftColWidth - 2);

    void DrawSettingsContent()
    {
        for (int r = 0; r < TextLayersSettingsOverlayRows; r++)
        {
            try
            {
                Console.SetCursorPosition(0, r);
            }
            catch (Exception ex) { _ = ex; /* Console position failed */ }
        }

        if (width < 40)
        {
            return;
        }

        try
        {
            var activePreset = visualizerSettings.Presets?.FirstOrDefault(p =>
                string.Equals(p.Id, visualizerSettings.ActivePresetId, StringComparison.OrdinalIgnoreCase))
                ?? visualizerSettings.Presets?.FirstOrDefault();
            string presetName = activePreset?.Name?.Trim() ?? "Preset 1";
            string title = renaming
                ? $" New preset name (Enter confirm, Esc cancel): {renameBuffer}_ "
                : $" Preset: {presetName} (R rename) ";
            string titleTruncated = VisualizerViewport.TruncateWithEllipsis(title, width - 2);
            int pad = Math.Max(0, (width - titleTruncated.Length - 2) / 2);
            Console.SetCursorPosition(0, 0);
            Console.Write(VisualizerViewport.TruncateToWidth("╔" + new string('═', width - 2) + "╗", width).PadRight(width));
            Console.SetCursorPosition(0, 1);
            Console.Write(VisualizerViewport.TruncateToWidth("║" + new string(' ', pad) + titleTruncated + new string(' ', width - pad - titleTruncated.Length - 2) + "║", width).PadRight(width));
            Console.SetCursorPosition(0, 2);
            Console.Write(VisualizerViewport.TruncateToWidth("╚" + new string('═', width - 2) + "╝", width).PadRight(width));
            Console.SetCursorPosition(0, 3);
            string hint = renaming ? "  Type new name, Enter to save, Esc to cancel" : "  1-9 select, \u2190\u2192 type, Shift+1-9 toggle, R rename, N new preset, ESC close";
            Console.Write(VisualizerViewport.TruncateWithEllipsis(hint, width).PadRight(width));
            Console.SetCursorPosition(0, 4);
            Console.Write(VisualizerViewport.TruncateToWidth("  ─" + new string('─', LeftColWidth - 2) + "┬" + new string('─', rightColWidth) + "─", width).PadRight(width));

            var selectedLayer = sortedLayers.Count > 0 && selectedIndex < sortedLayers.Count
                ? sortedLayers[selectedIndex]
                : null;

            for (int r = 5; r < TextLayersSettingsOverlayRows; r++)
            {
                Console.SetCursorPosition(LeftColWidth + 3, r);
                Console.Write(new string(' ', rightColWidth));
            }

            for (int i = 0; i < sortedLayers.Count && i < 9; i++)
            {
                int row = 5 + i;
                if (row >= TextLayersSettingsOverlayRows)
                {
                    break;
                }

                var layer = sortedLayers[i];
                string prefix = i == selectedIndex ? " ► " : "   ";
                string enabledMark = layer.Enabled ? "●" : "○";
                string leftLine = $"{prefix}{enabledMark} {i + 1}. {layer.LayerType}";
                leftLine = VisualizerViewport.TruncateWithEllipsis(leftLine, LeftColWidth).PadRight(LeftColWidth);

                Console.SetCursorPosition(0, row);
                if (i == selectedIndex)
                {
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    Console.ForegroundColor = ConsoleColor.White;
                }
                Console.Write(leftLine);
                Console.ResetColor();
                Console.Write(" │ ");
                Console.Write(new string(' ', rightColWidth));
            }

            if (selectedLayer != null)
            {
                var rightLines = new List<string>
                {
                    $"Enabled: {selectedLayer.Enabled}",
                    $"Layer type: {selectedLayer.LayerType}",
                    $"Z order: {selectedLayer.ZOrder}",
                    $"Beat reaction: {selectedLayer.BeatReaction}",
                    $"Speed: {selectedLayer.SpeedMultiplier:F1}",
                    $"Color index: {selectedLayer.ColorIndex}",
                    $"Palette: {selectedLayer.PaletteId ?? "(inherit)"}",
                    selectedLayer.TextSnippets is { Count: > 0 }
                        ? $"Snippets: {string.Join(", ", selectedLayer.TextSnippets.Take(3))}{(selectedLayer.TextSnippets.Count > 3 ? "..." : "")}"
                        : "Snippets: (none)"
                };
                if (selectedLayer.LayerType == TextLayerType.AsciiImage)
                {
                    rightLines.Add($"Image path: {selectedLayer.ImageFolderPath ?? "(none)"}");
                    rightLines.Add($"Image movement: {selectedLayer.AsciiImageMovement}");
                }

                for (int i = 0; i < rightLines.Count && (5 + i) < TextLayersSettingsOverlayRows; i++)
                {
                    Console.SetCursorPosition(LeftColWidth + 3, 5 + i);
                    string line = VisualizerViewport.TruncateWithEllipsis(rightLines[i], rightColWidth);
                    Console.Write(line.PadRight(rightColWidth));
                }
            }

            for (int row = 5 + sortedLayers.Count; row < TextLayersSettingsOverlayRows; row++)
            {
                Console.SetCursorPosition(0, row);
                Console.Write(new string(' ', width));
            }
        }
        catch (Exception ex) { _ = ex; /* Draw settings modal failed */ }
    }

    int DigitFromKey(ConsoleKey key)
    {
        return key switch
        {
            ConsoleKey.D1 or ConsoleKey.NumPad1 => 1,
            ConsoleKey.D2 or ConsoleKey.NumPad2 => 2,
            ConsoleKey.D3 or ConsoleKey.NumPad3 => 3,
            ConsoleKey.D4 or ConsoleKey.NumPad4 => 4,
            ConsoleKey.D5 or ConsoleKey.NumPad5 => 5,
            ConsoleKey.D6 or ConsoleKey.NumPad6 => 6,
            ConsoleKey.D7 or ConsoleKey.NumPad7 => 7,
            ConsoleKey.D8 or ConsoleKey.NumPad8 => 8,
            ConsoleKey.D9 or ConsoleKey.NumPad9 => 9,
            _ => 0
        };
    }

    bool HandleSettingsKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                selectedIndex = sortedLayers.Count > 0
                    ? (selectedIndex - 1 + sortedLayers.Count) % sortedLayers.Count
                    : 0;
                return false;
            case ConsoleKey.DownArrow:
                selectedIndex = sortedLayers.Count > 0
                    ? (selectedIndex + 1) % sortedLayers.Count
                    : 0;
                return false;
            case ConsoleKey.LeftArrow:
                if (sortedLayers.Count > 0 && selectedIndex < sortedLayers.Count)
                {
                    var layer = sortedLayers[selectedIndex];
                    layer.LayerType = TextLayerSettings.CycleTypeBackward(layer);
                    saveSettings();
                }
                return false;
            case ConsoleKey.RightArrow:
                if (sortedLayers.Count > 0 && selectedIndex < sortedLayers.Count)
                {
                    var layer = sortedLayers[selectedIndex];
                    layer.LayerType = TextLayerSettings.CycleTypeForward(layer);
                    saveSettings();
                }
                return false;
            case ConsoleKey.Escape:
                if (renaming)
                {
                    renaming = false;
                    return false;
                }
                return true;
            case ConsoleKey.Enter:
                if (renaming)
                {
                    var preset = visualizerSettings.Presets?.FirstOrDefault(p =>
                        string.Equals(p.Id, visualizerSettings.ActivePresetId, StringComparison.OrdinalIgnoreCase))
                        ?? visualizerSettings.Presets?.FirstOrDefault();
                    if (preset != null && !string.IsNullOrWhiteSpace(renameBuffer))
                    {
                        preset.Name = renameBuffer.Trim();
                        saveSettings();
                    }
                    renaming = false;
                    return false;
                }
                return false;
            case ConsoleKey.R:
                if (!renaming && visualizerSettings.Presets is { Count: > 0 })
                {
                    var p = visualizerSettings.Presets.FirstOrDefault(x =>
                        string.Equals(x.Id, visualizerSettings.ActivePresetId, StringComparison.OrdinalIgnoreCase))
                        ?? visualizerSettings.Presets[0];
                    renameBuffer = p.Name?.Trim() ?? "";
                    renaming = true;
                }
                return false;
            case ConsoleKey.Backspace:
                if (renaming && renameBuffer.Length > 0)
                {
                    renameBuffer = renameBuffer[..^1];
                }
                return false;
            case ConsoleKey.N:
                if (!renaming && visualizerSettings.Presets is { Count: >= 0 })
                {
                    var newPreset = new Preset
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        Name = $"Preset {visualizerSettings.Presets.Count + 1}",
                        Config = textLayers.DeepCopy()
                    };
                    visualizerSettings.Presets.Add(newPreset);
                    visualizerSettings.ActivePresetId = newPreset.Id;
                    visualizerSettings.TextLayers ??= new TextLayersVisualizerSettings();
                    visualizerSettings.TextLayers.CopyFrom(newPreset.Config);
                    textLayers = visualizerSettings.TextLayers;
                    sortedLayers = textLayers.Layers.OrderBy(l => l.ZOrder).ToList();
                    saveSettings();
                }
                return false;
            default:
                if (renaming)
                {
                    if (key.KeyChar is >= ' ' and <= '~' or (char)0)
                    {
                        if (key.KeyChar >= ' ')
                        {
                            renameBuffer += key.KeyChar;
                        }
                    }
                    return false;
                }
                int digit = DigitFromKey(key.Key);
                if (digit == 0)
                {
                    return false;
                }
                int layerIdx = digit - 1;
                if (layerIdx >= sortedLayers.Count)
                {
                    return false;
                }
                if (key.Modifiers.HasFlag(ConsoleModifiers.Shift))
                {
                    var l = sortedLayers[layerIdx];
                    l.Enabled = !l.Enabled;
                    saveSettings();
                }
                else
                {
                    selectedIndex = layerIdx;
                }
                return false;
        }
    }

    RunOverlayModal(
        TextLayersSettingsOverlayRows,
        DrawSettingsContent,
        HandleSettingsKey,
        consoleLock,
        onClose: () => analysisEngine.SetOverlayActive(false),
        onEnter: () => analysisEngine.SetOverlayActive(true, TextLayersSettingsOverlayRows));
}
