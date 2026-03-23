using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Help modal content and presentation. Content is dynamic from key handler bindings per ADR-0049.</summary>
internal sealed class HelpModal : IHelpModal
{
    private const int KeyColumnWidth = 14;

    private readonly UiSettings _uiSettings;
    private readonly IHelpContentProvider _helpContentProvider;
    private readonly IConsoleDimensions _consoleDimensions;
    private readonly ITitleBarNavigationContext _navigation;
    private readonly ITitleBarBreadcrumbFormatter _breadcrumbFormatter;
    private ApplicationMode _currentMode = ApplicationMode.PresetEditor;

    public HelpModal(
        UiSettings uiSettings,
        IHelpContentProvider helpContentProvider,
        IConsoleDimensions consoleDimensions,
        ITitleBarNavigationContext navigation,
        ITitleBarBreadcrumbFormatter breadcrumbFormatter)
    {
        _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
        _helpContentProvider = helpContentProvider ?? throw new ArgumentNullException(nameof(helpContentProvider));
        _consoleDimensions = consoleDimensions ?? throw new ArgumentNullException(nameof(consoleDimensions));
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        _breadcrumbFormatter = breadcrumbFormatter ?? throw new ArgumentNullException(nameof(breadcrumbFormatter));
    }

    /// <inheritdoc />
    public void Show(ApplicationMode? currentMode = null, Action? onEnter = null, Action? onClose = null)
    {
        _currentMode = currentMode ?? ApplicationMode.PresetEditor;
        ModalSystem.RunModal(
            DrawContent,
            _ => true,
            onClose: () =>
            {
                _navigation.View = TitleBarViewKind.Main;
                onClose?.Invoke();
            },
            onEnter: () =>
            {
                _navigation.View = TitleBarViewKind.HelpModal;
                onEnter?.Invoke();
            });
    }

    /// <summary>Draws help content only; does not clear or read input. Used by RunModal. Uses palette colors per ADR-0033.</summary>
    private void DrawContent()
    {
        var palette = (_uiSettings ?? new UiSettings()).Palette ?? new UiPalette();
        string labelCode = AnsiConsole.ColorCode(palette.Label);
        string dimmedCode = AnsiConsole.ColorCode(palette.Dimmed);
        string reset = AnsiConsole.ResetCode;

        int width = _consoleDimensions.GetConsoleWidth();
        TitleBarBreadcrumbRow.Write(0, width, _breadcrumbFormatter);

        string currentView = _currentMode switch
        {
            ApplicationMode.ShowPlay => "Show play",
            ApplicationMode.Settings => "General settings",
            _ => "Preset editor",
        };
        System.Console.SetCursorPosition(0, 1);
        System.Console.WriteLine(dimmedCode + "  Current: " + currentView + reset);
        System.Console.WriteLine();

        IReadOnlyList<HelpSection> sections = _helpContentProvider.GetSections(_currentMode);
        foreach (var section in sections)
        {
            System.Console.WriteLine(labelCode + "  " + section.SectionTitle.ToUpperInvariant() + reset);
            System.Console.WriteLine("  ─────────────────────────────────────");
            foreach (var binding in section.Bindings)
            {
                string keyPadded = AnsiConsole.PadToDisplayWidth(binding.Key, KeyColumnWidth);
                System.Console.WriteLine("  " + keyPadded + "  " + binding.Description);
            }
            System.Console.WriteLine();
        }

        System.Console.WriteLine(labelCode + "  PRESETS & SHOWS" + reset);
        System.Console.WriteLine("  ─────────────────────────────────────");
        System.Console.WriteLine("  Preset editor: Each preset is a TextLayers config (up to " + TextLayersLimits.MaxLayerCount + " layers + palette). V cycles.");
        System.Console.WriteLine("  Show play: Auto-cycles presets with per-entry duration (seconds or beats). Tab to switch.");
        System.Console.WriteLine();
        System.Console.WriteLine(dimmedCode + "  Press any key to return..." + reset);
    }
}
