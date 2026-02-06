using NAudio.CoreAudioApi;
using NAudio.Wave;

// Load settings for default selection
var settings = Settings.Load();

// Show device selection menu
var (initialDevice, initialName) = ShowDeviceSelectionMenu(settings, null);
if (initialDevice == null)
{
    Console.WriteLine("No device selected.");
    return;
}

var audioAnalyzer = new AudioAnalyzer();
IWaveIn? currentDevice = null;
string currentDeviceName = "";
object deviceLock = new();

// Set up header callback so AudioAnalyzer can redraw consistently
audioAnalyzer.SetHeaderCallback(() => DrawMainUI(currentDeviceName), 6);

// Apply saved visualization mode
audioAnalyzer.SetVisualizationMode(settings.VisualizationMode switch
{
    "oscilloscope" => VisualizationMode.Oscilloscope,
    "vumeter" => VisualizationMode.VuMeter,
    "winamp" => VisualizationMode.WinampBars,
    "geiss" => VisualizationMode.Geiss,
    _ => VisualizationMode.SpectrumBars
});

// Apply saved beat sensitivity
audioAnalyzer.BeatSensitivity = settings.BeatSensitivity;

// Apply saved beat circles setting
audioAnalyzer.ShowBeatCircles = settings.BeatCircles;

// Function to set up and start a capture device
void StartCapture(IWaveIn device, string name)
{
    lock (deviceLock)
    {
        // Stop and dispose old device if exists
        if (currentDevice != null)
        {
            try
            {
                currentDevice.StopRecording();
                currentDevice.Dispose();
            }
            catch { }
        }

        currentDevice = device;
        currentDeviceName = name;

        currentDevice.DataAvailable += (sender, e) =>
        {
            lock (deviceLock)
            {
                if (currentDevice == null) return;
                var waveFormat = currentDevice switch
                {
                    WasapiLoopbackCapture loopback => loopback.WaveFormat,
                    WasapiCapture wasapi => wasapi.WaveFormat,
                    _ => null
                };
                if (waveFormat != null)
                {
                    audioAnalyzer.ProcessAudio(e.Buffer, e.BytesRecorded, waveFormat);
                }
            }
        };

        currentDevice.StartRecording();
    }
}

// Start with initial device
StartCapture(initialDevice, initialName);

// Draw initial UI
DrawMainUI(currentDeviceName);

// Main loop
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
                // Pause current device
                lock (deviceLock)
                {
                    currentDevice?.StopRecording();
                }

                // Show device menu
                var (newDevice, newName) = ShowDeviceSelectionMenu(settings, currentDeviceName);

                if (newDevice != null)
                {
                    // Switch to new device
                    StartCapture(newDevice, newName);
                }
                else
                {
                    // User cancelled, restart old device
                    lock (deviceLock)
                    {
                        currentDevice?.StartRecording();
                    }
                }

                // Redraw UI
                DrawMainUI(currentDeviceName);
                break;

            case ConsoleKey.H:
                ShowHelpMenu();
                DrawMainUI(currentDeviceName);
                break;

            case ConsoleKey.V:
                audioAnalyzer.NextVisualizationMode();
                DrawMainUI(currentDeviceName);
                break;

            case ConsoleKey.OemPlus:
            case ConsoleKey.Add:
                // Decrease sensitivity (higher threshold = less sensitive)
                audioAnalyzer.BeatSensitivity += 0.1;
                break;

            case ConsoleKey.OemMinus:
            case ConsoleKey.Subtract:
                // Increase sensitivity (lower threshold = more sensitive)
                audioAnalyzer.BeatSensitivity -= 0.1;
                break;

            case ConsoleKey.B:
                // Toggle beat circles in Geiss mode
                audioAnalyzer.ShowBeatCircles = !audioAnalyzer.ShowBeatCircles;
                break;

            case ConsoleKey.S:
                // Save current settings
                settings.VisualizationMode = audioAnalyzer.CurrentMode switch
                {
                    VisualizationMode.Oscilloscope => "oscilloscope",
                    VisualizationMode.VuMeter => "vumeter",
                    VisualizationMode.WinampBars => "winamp",
                    VisualizationMode.Geiss => "geiss",
                    _ => "spectrum"
                };
                settings.BeatSensitivity = audioAnalyzer.BeatSensitivity;
                settings.BeatCircles = audioAnalyzer.ShowBeatCircles;
                settings.Save();
                // Brief visual feedback - flash the mode text
                Console.SetCursorPosition(0, 6);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"Settings saved! Mode: {audioAnalyzer.GetModeName()}".PadRight(Console.WindowWidth - 1));
                Console.ResetColor();
                Thread.Sleep(600);
                DrawMainUI(currentDeviceName);
                break;
        }
    }
    Thread.Sleep(50);
}

// Cleanup
lock (deviceLock)
{
    currentDevice?.StopRecording();
    currentDevice?.Dispose();
    currentDevice = null;
}

Console.Clear();
Console.CursorVisible = true;
Console.WriteLine("Recording stopped.");

void DrawMainUI(string deviceName)
{
    Console.Clear();
    Console.CursorVisible = false;

    int width = Console.WindowWidth;
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

    int width = Console.WindowWidth;
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
    Console.WriteLine();
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
    Console.WriteLine();
    Console.WriteLine("  ↑/↓       Navigate devices");
    Console.WriteLine("  ENTER     Select device");
    Console.WriteLine("  S         Save selection as default");
    Console.WriteLine("  ESC       Cancel and return");
    Console.WriteLine();

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("  VISUALIZATION MODES (press V to cycle)");
    Console.ResetColor();
    Console.WriteLine("  ─────────────────────────────────────");
    Console.WriteLine();
    Console.WriteLine("  Spectrum Analyzer  Frequency bars with peak hold");
    Console.WriteLine("  Oscilloscope       Waveform display");
    Console.WriteLine("  VU Meter           Classic stereo level meters");
    Console.WriteLine("  Winamp Style       Classic music player bars");
    Console.WriteLine("  Geiss              Psychedelic plasma visualization");
    Console.WriteLine();

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("  AUDIO SOURCES");
    Console.ResetColor();
    Console.WriteLine("  ─────────────────────────────────────");
    Console.WriteLine();
    Console.WriteLine("  🎤        Microphone / capture device");
    Console.WriteLine("  🔊        Speaker loopback (system audio)");
    Console.WriteLine();

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("  SETTINGS");
    Console.ResetColor();
    Console.WriteLine("  ─────────────────────────────────────");
    Console.WriteLine();
    Console.WriteLine("  Default device is stored in appsettings.json");
    Console.WriteLine("  Press 'S' in device menu to save current selection");
    Console.WriteLine();

    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine("  Press any key to return...");
    Console.ResetColor();

    Console.ReadKey(true);
}

static (IWaveIn? device, string name) ShowDeviceSelectionMenu(Settings settings, string? currentDeviceName)
{
    var enumerator = new MMDeviceEnumerator();
    var menuItems = new List<(string name, Func<IWaveIn?> create, string settingsMode, string? settingsDevice)>();

    // Add system loopback option
    menuItems.Add(("System Audio (Loopback)", () => new WasapiLoopbackCapture(), "loopback", null));

    // Add capture devices (microphones)
    var captureDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
    foreach (var device in captureDevices)
    {
        var d = device; // Capture for closure
        menuItems.Add(($"🎤 {device.FriendlyName}", () => new WasapiCapture(d), "device", device.FriendlyName));
    }

    // Add render devices (for loopback on specific output)
    var renderDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
    foreach (var device in renderDevices)
    {
        var d = device; // Capture for closure
        menuItems.Add(($"🔊 {device.FriendlyName} (Loopback)", () => new WasapiLoopbackCapture(d), "device", device.FriendlyName));
    }

    if (menuItems.Count == 0)
    {
        Console.WriteLine("No audio devices found!");
        return (null, "");
    }

    // Find default selection - prefer current device, then settings
    int selectedIndex = 0;
    if (!string.IsNullOrEmpty(currentDeviceName))
    {
        for (int i = 0; i < menuItems.Count; i++)
        {
            if (menuItems[i].name == currentDeviceName)
            {
                selectedIndex = i;
                break;
            }
        }
    }
    else if (settings.InputMode == "loopback")
    {
        selectedIndex = 0;
    }
    else if (!string.IsNullOrEmpty(settings.DeviceName))
    {
        for (int i = 0; i < menuItems.Count; i++)
        {
            if (menuItems[i].name.Contains(settings.DeviceName, StringComparison.OrdinalIgnoreCase))
            {
                selectedIndex = i;
                break;
            }
        }
    }

    Console.Clear();
    Console.CursorVisible = false;

    // Draw menu
    while (true)
    {
        Console.SetCursorPosition(0, 0);

        int width = Console.WindowWidth;
        string title = " SELECT AUDIO INPUT ";
        int pad = Math.Max(0, (width - title.Length - 2) / 2);
        Console.WriteLine("╔" + new string('═', width - 2) + "╗");
        Console.WriteLine("║" + new string(' ', pad) + title + new string(' ', width - pad - title.Length - 2) + "║");
        Console.WriteLine("╚" + new string('═', width - 2) + "╝");
        Console.WriteLine();
        Console.WriteLine("  Use ↑/↓ to select, ENTER to confirm, ESC to cancel");
        Console.WriteLine("  Press 'S' to save selection as default");
        Console.WriteLine();

        for (int i = 0; i < menuItems.Count; i++)
        {
            bool isCurrent = currentDeviceName != null && menuItems[i].name == currentDeviceName;

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
            string line = $"{prefix}{menuItems[i].name}{suffix}";

            // Pad or truncate to fit width
            if (line.Length < width - 1)
                line = line.PadRight(width - 1);
            else
                line = line[..(width - 1)];

            Console.WriteLine(line);
            Console.ResetColor();
        }

        // Clear any remaining lines from previous render
        Console.WriteLine(new string(' ', width - 1));

        var key = Console.ReadKey(true);

        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                selectedIndex = (selectedIndex - 1 + menuItems.Count) % menuItems.Count;
                break;
            case ConsoleKey.DownArrow:
                selectedIndex = (selectedIndex + 1) % menuItems.Count;
                break;
            case ConsoleKey.Enter:
                var selected = menuItems[selectedIndex];
                try
                {
                    return (selected.create(), selected.name);
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
                // Save as default
                var toSave = menuItems[selectedIndex];
                settings.InputMode = toSave.settingsMode;
                settings.DeviceName = toSave.settingsDevice;
                settings.Save();

                // Show brief confirmation
                Console.SetCursorPosition(0, 6 + menuItems.Count + 1);
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
