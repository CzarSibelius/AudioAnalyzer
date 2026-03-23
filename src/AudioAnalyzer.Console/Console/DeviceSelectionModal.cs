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
    private readonly IUiThemeResolver _uiThemeResolver;
    private readonly IConsoleDimensions _consoleDimensions;
    private readonly ITitleBarNavigationContext _navigation;
    private readonly ITitleBarBreadcrumbFormatter _breadcrumbFormatter;

    public DeviceSelectionModal(
        IAudioDeviceInfo deviceInfo,
        IKeyHandler<DeviceSelectionKeyContext> keyHandler,
        ISettingsRepository settingsRepo,
        AppSettings settings,
        UiSettings uiSettings,
        IUiThemeResolver uiThemeResolver,
        IConsoleDimensions consoleDimensions,
        ITitleBarNavigationContext navigation,
        ITitleBarBreadcrumbFormatter breadcrumbFormatter)
    {
        _deviceInfo = deviceInfo ?? throw new ArgumentNullException(nameof(deviceInfo));
        _keyHandler = keyHandler ?? throw new ArgumentNullException(nameof(keyHandler));
        _settingsRepo = settingsRepo ?? throw new ArgumentNullException(nameof(settingsRepo));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
        _uiThemeResolver = uiThemeResolver ?? throw new ArgumentNullException(nameof(uiThemeResolver));
        _consoleDimensions = consoleDimensions ?? throw new ArgumentNullException(nameof(consoleDimensions));
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        _breadcrumbFormatter = breadcrumbFormatter ?? throw new ArgumentNullException(nameof(breadcrumbFormatter));
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

        var palette = _uiThemeResolver.GetEffectiveUiPalette();
        var selBg = palette.Background ?? PaletteColor.FromConsoleColor(ConsoleColor.DarkBlue);
        var selFg = palette.Highlighted;
        var currentColor = palette.Highlighted;

        void DrawDeviceContent()
        {
            int width = _consoleDimensions.GetConsoleWidth();
            TitleBarBreadcrumbRow.Write(0, width, _breadcrumbFormatter);

            System.Console.SetCursorPosition(0, 1);
            System.Console.WriteLine("  Use ↑/↓ to select, ENTER to confirm, ESC to cancel");
            System.Console.WriteLine();

            SettingsSurfacesListDrawing.DrawAudioDeviceList(
                3,
                width,
                devices,
                context.SelectedIndex,
                currentDeviceName,
                selBg,
                selFg,
                currentColor);
        }

        bool HandleKey(ConsoleKeyInfo key) => _keyHandler.Handle(key, context);

        ModalSystem.RunModal(
            DrawDeviceContent,
            HandleKey,
            onClose: () =>
            {
                setModalOpen(false);
                _navigation.View = TitleBarViewKind.Main;
            },
            onEnter: () =>
            {
                setModalOpen(true);
                _navigation.View = TitleBarViewKind.DeviceAudioInputModal;
            });
        return (context.ResultId, context.ResultName);
    }
}
