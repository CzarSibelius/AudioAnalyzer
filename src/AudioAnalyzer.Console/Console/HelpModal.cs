namespace AudioAnalyzer.Console;

/// <summary>Help modal content and presentation per ADR-0006.</summary>
internal static class HelpModal
{
    /// <summary>Draws help content only; does not clear or read input. Used by RunModal.</summary>
    public static void DrawContent()
    {
        int width = ConsoleHeader.GetConsoleWidth();
        string title = " HELP ";
        int pad = Math.Max(0, (width - title.Length - 2) / 2);
        System.Console.WriteLine("╔" + new string('═', width - 2) + "╗");
        System.Console.WriteLine("║" + new string(' ', pad) + title + new string(' ', width - pad - title.Length - 2) + "║");
        System.Console.WriteLine("╚" + new string('═', width - 2) + "╝");
        System.Console.WriteLine();
        System.Console.ForegroundColor = ConsoleColor.Cyan;
        System.Console.WriteLine("  KEYBOARD CONTROLS");
        System.Console.ResetColor();
        System.Console.WriteLine("  ─────────────────────────────────────");
        System.Console.WriteLine("  H         Show this help menu");
        System.Console.WriteLine("  Tab       Switch between Preset editor and Show play");
        System.Console.WriteLine("  V         Cycle to next preset (Preset editor only)");
        System.Console.WriteLine("  P         Cycle color palette (palette-aware visualizers)");
        System.Console.WriteLine("  +/-       Adjust beat sensitivity");
        System.Console.WriteLine("  [ / ]     Adjust oscilloscope gain (Layered text, when Oscilloscope layer selected)");
        System.Console.WriteLine("  D         Change audio input device");
        System.Console.WriteLine("  S         Preset modal (Preset editor) or Show edit modal (Show play), ESC close");
        System.Console.WriteLine("  ESC       Quit the application");
        System.Console.WriteLine("  F         Toggle full screen (visualizer only, no header/toolbar)");
        System.Console.WriteLine();
        System.Console.ForegroundColor = ConsoleColor.Cyan;
        System.Console.WriteLine("  PRESET SETTINGS MODAL (S)");
        System.Console.ResetColor();
        System.Console.WriteLine("  ─────────────────────────────────────");
        System.Console.WriteLine("  1-9       Select layer");
        System.Console.WriteLine("  ←→       Change layer type (left panel)");
        System.Console.WriteLine("  ENTER     Move to settings panel (when layer selected)");
        System.Console.WriteLine("  SPACE     Toggle layer enabled/disabled (when layer selected)");
        System.Console.WriteLine("  Shift+1-9 Toggle layer enabled/disabled by slot");
        System.Console.WriteLine("  ↑↓       Select layer or setting");
        System.Console.WriteLine("  ENTER     Cycle selected setting (or edit strings)");
        System.Console.WriteLine("  +/-       Cycle selected setting (when cycleable)");
        System.Console.WriteLine("  ↑↓       Confirm when editing strings");
        System.Console.WriteLine("  ←/ESC  Back to layer list from settings panel");
        System.Console.WriteLine("  R         Rename preset");
        System.Console.WriteLine("  N         New preset (duplicate of current)");
        System.Console.WriteLine("  ESC       Close modal");
        System.Console.WriteLine();
        System.Console.ForegroundColor = ConsoleColor.Cyan;
        System.Console.WriteLine("  DEVICE SELECTION MENU");
        System.Console.ResetColor();
        System.Console.WriteLine("  ─────────────────────────────────────");
        System.Console.WriteLine("  ↑/↓       Navigate devices");
        System.Console.WriteLine("  ENTER     Select device");
        System.Console.WriteLine("  ESC       Cancel and return");
        System.Console.WriteLine();
        System.Console.ForegroundColor = ConsoleColor.Cyan;
        System.Console.WriteLine("  SHOW EDIT MODAL (S when in Show play)");
        System.Console.ResetColor();
        System.Console.WriteLine("  ─────────────────────────────────────");
        System.Console.WriteLine("  Up/Down   Select entry");
        System.Console.WriteLine("  A         Add preset entry");
        System.Console.WriteLine("  D         Delete selected entry");
        System.Console.WriteLine("  P         Cycle preset for selected entry");
        System.Console.WriteLine("  Enter     Edit duration value");
        System.Console.WriteLine("  U         Toggle duration unit (Seconds/Beats)");
        System.Console.WriteLine("  R         Rename show");
        System.Console.WriteLine("  N         New show");
        System.Console.WriteLine("  ESC       Close modal");
        System.Console.WriteLine();
        System.Console.ForegroundColor = ConsoleColor.Cyan;
        System.Console.WriteLine("  PRESETS & SHOWS");
        System.Console.ResetColor();
        System.Console.WriteLine("  ─────────────────────────────────────");
        System.Console.WriteLine("  Preset editor: Each preset is a TextLayers config (9 layers + palette). V cycles.");
        System.Console.WriteLine("  Show play: Auto-cycles presets with per-entry duration (seconds or beats). Tab to switch.");
        System.Console.WriteLine();
        System.Console.ForegroundColor = ConsoleColor.DarkGray;
        System.Console.WriteLine("  Press any key to return...");
        System.Console.ResetColor();
    }

    /// <summary>Shows the help modal; any key closes it.</summary>
    public static void Show(Action? onEnter, Action? onClose)
    {
        ModalSystem.RunModal(DrawContent, _ => true, onClose, onEnter);
    }
}
