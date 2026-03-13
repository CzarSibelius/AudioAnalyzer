using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Device selection modal per ADR-0006. Key handling via IKeyHandler per ADR-0047.</summary>
internal sealed class DeviceSelectionModal : IDeviceSelectionModal
{
    private readonly IAudioDeviceInfo _deviceInfo;
    private readonly IKeyHandler<DeviceSelectionKeyContext> _keyHandler;
    private readonly ISettingsRepository _settingsRepo;
    private readonly AppSettings _settings;
    private readonly UiSettings _uiSettings;
    private readonly IConsoleDimensions _consoleDimensions;

    public DeviceSelectionModal(
        IAudioDeviceInfo deviceInfo,
        IKeyHandler<DeviceSelectionKeyContext> keyHandler,
        ISettingsRepository settingsRepo,
        AppSettings settings,
        UiSettings uiSettings,
        IConsoleDimensions consoleDimensions)
    {
        _deviceInfo = deviceInfo ?? throw new ArgumentNullException(nameof(deviceInfo));
        _keyHandler = keyHandler ?? throw new ArgumentNullException(nameof(keyHandler));
        _settingsRepo = settingsRepo ?? throw new ArgumentNullException(nameof(settingsRepo));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
        _consoleDimensions = consoleDimensions ?? throw new ArgumentNullException(nameof(consoleDimensions));
    }

    /// <inheritdoc />
    public (string? deviceId, string name) Show(string? currentDeviceName, Action<bool> setModalOpen)
    {
        var devices = _deviceInfo.GetDevices();
        if (devices.Count == 0)
        {
            System.Console.WriteLine("No audio devices found!");
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
        else if (_settings.InputMode == "loopback")
        {
            selectedIndex = 0;
        }
        else if (!string.IsNullOrEmpty(_settings.DeviceName))
        {
            for (int i = 0; i < devices.Count; i++)
            {
                if (devices[i].Name.Contains(_settings.DeviceName, StringComparison.OrdinalIgnoreCase))
                {
                    selectedIndex = i;
                    break;
                }
            }
        }

        var context = new DeviceSelectionKeyContext
        {
            Devices = devices,
            SelectedIndex = selectedIndex,
            Settings = _settings,
            SettingsRepo = _settingsRepo
        };

        var palette = (_uiSettings ?? new UiSettings()).Palette ?? new UiPalette();
        var selBg = palette.Background ?? PaletteColor.FromConsoleColor(ConsoleColor.DarkBlue);
        var selFg = palette.Highlighted;
        var currentColor = palette.Highlighted;

        void DrawDeviceContent()
        {
            int width = _consoleDimensions.GetConsoleWidth();
            string title = " SELECT AUDIO INPUT ";
            int pad = Math.Max(0, (width - title.Length - 2) / 2);
            System.Console.WriteLine("╔" + new string('═', width - 2) + "╗");
            System.Console.WriteLine("║" + new string(' ', pad) + title + new string(' ', width - pad - title.Length - 2) + "║");
            System.Console.WriteLine("╚" + new string('═', width - 2) + "╝");
            System.Console.WriteLine();
            System.Console.WriteLine("  Use ↑/↓ to select, ENTER to confirm, ESC to cancel");
            System.Console.WriteLine();

            for (int i = 0; i < devices.Count; i++)
            {
                bool isCurrent = currentDeviceName != null && devices[i].Name == currentDeviceName;
                string prefix = i == context.SelectedIndex ? " ► " : "   ";
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

                string lineToWrite;
                if (i == context.SelectedIndex)
                {
                    lineToWrite = AnsiConsole.BackgroundCode(selBg) + AnsiConsole.ColorCode(selFg) + line + AnsiConsole.ResetCode;
                }
                else if (isCurrent)
                {
                    lineToWrite = AnsiConsole.ColorCode(currentColor) + line + AnsiConsole.ResetCode;
                }
                else
                {
                    lineToWrite = line;
                }
                System.Console.WriteLine(lineToWrite);
            }
            System.Console.WriteLine(new string(' ', width - 1));
        }

        bool HandleKey(ConsoleKeyInfo key) => _keyHandler.Handle(key, context);

        ModalSystem.RunModal(DrawDeviceContent, HandleKey, onClose: () => setModalOpen(false), onEnter: () => setModalOpen(true));
        return (context.ResultId, context.ResultName);
    }
}
