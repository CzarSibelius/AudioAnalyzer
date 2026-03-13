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
    private ApplicationMode _currentMode = ApplicationMode.PresetEditor;

    public HelpModal(UiSettings uiSettings, IHelpContentProvider helpContentProvider, IConsoleDimensions consoleDimensions)
    {
        _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
        _helpContentProvider = helpContentProvider ?? throw new ArgumentNullException(nameof(helpContentProvider));
        _consoleDimensions = consoleDimensions ?? throw new ArgumentNullException(nameof(consoleDimensions));
    }

    /// <inheritdoc />
    public void Show(ApplicationMode? currentMode = null, Action? onEnter = null, Action? onClose = null)
    {
        _currentMode = currentMode ?? ApplicationMode.PresetEditor;
        ModalSystem.RunModal(() => DrawContent(), _ => true, onClose, onEnter);
    }

    /// <summary>Draws help content only; does not clear or read input. Used by RunModal. Uses palette colors per ADR-0033.</summary>
    private void DrawContent()
    {
        var palette = (_uiSettings ?? new UiSettings()).Palette ?? new UiPalette();
        string labelCode = AnsiConsole.ColorCode(palette.Label);
        string dimmedCode = AnsiConsole.ColorCode(palette.Dimmed);
        string reset = AnsiConsole.ResetCode;

        int width = _consoleDimensions.GetConsoleWidth();
        string title = " HELP ";
        int pad = Math.Max(0, (width - title.Length - 2) / 2);
        System.Console.WriteLine("╔" + new string('═', width - 2) + "╗");
        System.Console.WriteLine("║" + new string(' ', pad) + title + new string(' ', width - pad - title.Length - 2) + "║");
        System.Console.WriteLine("╚" + new string('═', width - 2) + "╝");
        System.Console.WriteLine();
        string currentView = _currentMode == ApplicationMode.ShowPlay ? "Show play" : "Preset editor";
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
