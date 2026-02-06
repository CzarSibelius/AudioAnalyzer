using System.IO;
using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

static int GetConsoleWidth()
{
    try { return Console.WindowWidth; }
    catch (IOException) { return 80; }
}

var services = new ServiceCollection();
services.AddSingleton<IDisplayDimensions, ConsoleDisplayDimensions>();
services.AddSingleton<CompositeVisualizationRenderer>(sp =>
{
    var dimensions = sp.GetRequiredService<IDisplayDimensions>();
    return new CompositeVisualizationRenderer(dimensions);
});
services.AddSingleton<IVisualizationRenderer>(sp => sp.GetRequiredService<CompositeVisualizationRenderer>());
services.AddSingleton<ISettingsRepository>(_ => new FileSettingsRepository());
services.AddSingleton<IAudioDeviceInfo, NAudioDeviceInfo>();
services.AddSingleton<AnalysisEngine>(sp =>
{
    var renderer = sp.GetRequiredService<IVisualizationRenderer>();
    var dimensions = sp.GetRequiredService<IDisplayDimensions>();
    return new AnalysisEngine(renderer, dimensions);
});

var provider = services.BuildServiceProvider();
var settingsRepo = provider.GetRequiredService<ISettingsRepository>();
var deviceInfo = provider.GetRequiredService<IAudioDeviceInfo>();
var engine = provider.GetRequiredService<AnalysisEngine>();
var compositeRenderer = provider.GetRequiredService<CompositeVisualizationRenderer>();

var settings = settingsRepo.Load();

static VisualizationMode ParseMode(string? mode)
{
    return mode switch
    {
        "oscilloscope" => VisualizationMode.Oscilloscope,
        "vumeter" => VisualizationMode.VuMeter,
        "winamp" => VisualizationMode.WinampBars,
        "geiss" => VisualizationMode.Geiss,
        _ => VisualizationMode.SpectrumBars
    };
}

engine.SetVisualizationMode(ParseMode(settings.VisualizationMode));
engine.BeatSensitivity = settings.BeatSensitivity;
compositeRenderer.SetShowBeatCircles(settings.BeatCircles);

var (initialDeviceId, initialName) = ShowDeviceSelectionMenu(deviceInfo, settingsRepo, settings, null);
if (initialName == "")
{
    Console.WriteLine("No device selected.");
    return;
}

IAudioInput? currentInput = null;
string currentDeviceName = initialName;
object deviceLock = new();

engine.SetHeaderCallback(() => DrawMainUI(currentDeviceName), 6);

void StartCapture(string? deviceId, string name)
{
    lock (deviceLock)
    {
        currentInput?.Stop();
        currentInput?.Dispose();
        currentInput = deviceInfo.CreateCapture(deviceId);
        currentDeviceName = name;
        currentInput.DataAvailable += (_, e) =>
        {
            lock (deviceLock)
            {
                if (currentInput == null) return;
                engine.ProcessAudio(e.Buffer, e.BytesRecorded, e.Format);
            }
        };
        currentInput.Start();
    }
}

StartCapture(initialDeviceId, initialName);
DrawMainUI(currentDeviceName);

bool running = true;
while (running)
{
    if (Console.KeyAvailable)
    {
        var key = Console.ReadKey(true);
        switch (key.Key)
        {
            case ConsoleKey.Escape:
                running = false;
                break;
            case ConsoleKey.D:
                lock (deviceLock) currentInput?.Stop();
                var (newId, newName) = ShowDeviceSelectionMenu(deviceInfo, settingsRepo, settings, currentDeviceName);
                if (newName != "")
                {
                    StartCapture(newId, newName);
                }
                else
                {
                    lock (deviceLock) currentInput?.Start();
                }
                DrawMainUI(currentDeviceName);
                break;
            case ConsoleKey.H:
                ShowHelpMenu();
                DrawMainUI(currentDeviceName);
                break;
            case ConsoleKey.V:
                engine.NextVisualizationMode();
                DrawMainUI(currentDeviceName);
                break;
            case ConsoleKey.OemPlus:
            case ConsoleKey.Add:
                engine.BeatSensitivity += 0.1;
                break;
            case ConsoleKey.OemMinus:
            case ConsoleKey.Subtract:
                engine.BeatSensitivity -= 0.1;
                break;
            case ConsoleKey.B:
                compositeRenderer.SetShowBeatCircles(!compositeRenderer.GetShowBeatCircles());
                break;
            case ConsoleKey.S:
                settings.VisualizationMode = engine.CurrentMode switch
                {
                    VisualizationMode.Oscilloscope => "oscilloscope",
                    VisualizationMode.VuMeter => "vumeter",
                    VisualizationMode.WinampBars => "winamp",
                    VisualizationMode.Geiss => "geiss",
                    _ => "spectrum"
                };
                settings.BeatSensitivity = engine.BeatSensitivity;
                settings.BeatCircles = compositeRenderer.GetShowBeatCircles();
                settingsRepo.Save(settings);
                Console.SetCursorPosition(0, 6);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"Settings saved! Mode: {engine.GetModeName()}".PadRight(GetConsoleWidth() - 1));
                Console.ResetColor();
                Thread.Sleep(600);
                DrawMainUI(currentDeviceName);
                break;
        }
    }
    Thread.Sleep(50);
}

lock (deviceLock)
{
    currentInput?.Stop();
    currentInput?.Dispose();
    currentInput = null;
}
Console.Clear();
Console.CursorVisible = true;
Console.WriteLine("Recording stopped.");

void DrawMainUI(string deviceName)
{
    Console.Clear();
    Console.CursorVisible = false;
    int width = GetConsoleWidth();
    string title = " AUDIO ANALYZER - Real-time Frequency Spectrum ";
    int padding = Math.Max(0, (width - title.Length - 2) / 2);
    Console.WriteLine("╔" + new string('═', width - 2) + "╗");
    Console.WriteLine("║" + new string(' ', padding) + title + new string(' ', width - padding - title.Length - 2) + "║");
    Console.WriteLine("╚" + new string('═', width - 2) + "╝");
    Console.WriteLine($"\nInput: {deviceName}");
    Console.WriteLine("Press H for help, D to change device, ESC to quit\n");
}

void ShowHelpMenu()
{
    Console.Clear();
    Console.CursorVisible = false;
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
    Console.WriteLine("  B         Toggle beat circles (Geiss mode)");
    Console.WriteLine("  +/-       Adjust beat sensitivity");
    Console.WriteLine("  S         Save current settings");
    Console.WriteLine("  D         Change audio input device");
    Console.WriteLine("  ESC       Quit the application");
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("  DEVICE SELECTION MENU");
    Console.ResetColor();
    Console.WriteLine("  ─────────────────────────────────────");
    Console.WriteLine("  ↑/↓       Navigate devices");
    Console.WriteLine("  ENTER     Select device");
    Console.WriteLine("  S         Save selection as default");
    Console.WriteLine("  ESC       Cancel and return");
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("  VISUALIZATION MODES (press V to cycle)");
    Console.ResetColor();
    Console.WriteLine("  ─────────────────────────────────────");
    Console.WriteLine("  Spectrum Analyzer  Frequency bars with peak hold");
    Console.WriteLine("  Oscilloscope       Waveform display");
    Console.WriteLine("  VU Meter           Classic stereo level meters");
    Console.WriteLine("  Winamp Style       Classic music player bars");
    Console.WriteLine("  Geiss              Psychedelic plasma visualization");
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine("  Press any key to return...");
    Console.ResetColor();
    Console.ReadKey(true);
}

static (string? deviceId, string name) ShowDeviceSelectionMenu(IAudioDeviceInfo deviceInfo, ISettingsRepository settingsRepo, AppSettings settings, string? currentDeviceName)
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

    Console.Clear();
    Console.CursorVisible = false;

    while (true)
    {
        Console.SetCursorPosition(0, 0);
        int width = GetConsoleWidth();
        string title = " SELECT AUDIO INPUT ";
        int pad = Math.Max(0, (width - title.Length - 2) / 2);
        Console.WriteLine("╔" + new string('═', width - 2) + "╗");
        Console.WriteLine("║" + new string(' ', pad) + title + new string(' ', width - pad - title.Length - 2) + "║");
        Console.WriteLine("╚" + new string('═', width - 2) + "╝");
        Console.WriteLine();
        Console.WriteLine("  Use ↑/↓ to select, ENTER to confirm, ESC to cancel");
        Console.WriteLine("  Press 'S' to save selection as default");
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
                Console.ForegroundColor = ConsoleColor.Cyan;

            string prefix = i == selectedIndex ? " ► " : "   ";
            string suffix = isCurrent ? " (current)" : "";
            string line = $"{prefix}{devices[i].Name}{suffix}";
            if (line.Length < width - 1) line = line.PadRight(width - 1);
            else line = line[..(width - 1)];
            Console.WriteLine(line);
            Console.ResetColor();
        }
        Console.WriteLine(new string(' ', width - 1));

        var key = Console.ReadKey(true);
        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                selectedIndex = (selectedIndex - 1 + devices.Count) % devices.Count;
                break;
            case ConsoleKey.DownArrow:
                selectedIndex = (selectedIndex + 1) % devices.Count;
                break;
            case ConsoleKey.Enter:
                var selected = devices[selectedIndex];
                try
                {
                    return (selected.Id, selected.Name);
                }
                catch (Exception ex)
                {
                    Console.Clear();
                    Console.WriteLine($"Error opening device: {ex.Message}");
                    Console.WriteLine("Press any key to try again...");
                    Console.ReadKey(true);
                    Console.Clear();
                }
                break;
            case ConsoleKey.S:
                var toSave = devices[selectedIndex];
                settings.InputMode = toSave.Id == null ? "loopback" : "device";
                if (toSave.Id != null)
                {
                    if (toSave.Id.StartsWith("capture:", StringComparison.Ordinal))
                        settings.DeviceName = toSave.Id.Substring(8);
                    else if (toSave.Id.StartsWith("loopback:", StringComparison.Ordinal))
                        settings.DeviceName = toSave.Id.Substring(9);
                    else
                        settings.DeviceName = toSave.Id;
                }
                else
                    settings.DeviceName = null;
                settingsRepo.Save(settings);
                Console.SetCursorPosition(0, 6 + devices.Count + 1);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✓ Saved as default!".PadRight(width - 1));
                Console.ResetColor();
                Thread.Sleep(800);
                break;
            case ConsoleKey.Escape:
                return (null, "");
        }
    }
}
