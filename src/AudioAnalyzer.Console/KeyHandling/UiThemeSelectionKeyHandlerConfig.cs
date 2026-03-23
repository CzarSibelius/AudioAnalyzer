using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using KeyHandling = AudioAnalyzer.Console.KeyHandling;

namespace AudioAnalyzer.Console;

/// <summary>Up/Down navigate, Enter applies theme and saves, Esc cancels.</summary>
internal sealed class UiThemeSelectionKeyHandlerConfig : IKeyHandlerConfig<UiThemeSelectionKeyContext>
{
    private const string Section = "UI theme selection";

    private static int TotalCount(UiThemeSelectionKeyContext ctx) => 1 + ctx.Palettes.Count;

    private static IReadOnlyList<KeyHandling.KeyBindingEntry<UiThemeSelectionKeyContext>> GetEntries() =>
    [
        new KeyHandling.KeyBindingEntry<UiThemeSelectionKeyContext>(
            Matches: static k => k.Key is ConsoleKey.UpArrow or ConsoleKey.DownArrow,
            Action: static (key, ctx) =>
            {
                int n = TotalCount(ctx);
                if (n <= 0)
                {
                    return false;
                }

                if (key.Key == ConsoleKey.UpArrow)
                {
                    ctx.SelectedIndex = (ctx.SelectedIndex - 1 + n) % n;
                }
                else
                {
                    ctx.SelectedIndex = (ctx.SelectedIndex + 1) % n;
                }

                return false;
            },
            Key: "↑/↓",
            Description: "Navigate themes",
            Section),
        new KeyHandling.KeyBindingEntry<UiThemeSelectionKeyContext>(
            Matches: static k => k.Key == ConsoleKey.Enter,
            Action: static (_, ctx) =>
            {
                if (ctx.SelectedIndex == 0)
                {
                    ctx.UiSettings.UiThemePaletteId = null;
                }
                else
                {
                    ctx.UiSettings.UiThemePaletteId = ctx.Palettes[ctx.SelectedIndex - 1].Id;
                }

                ctx.SaveSettings();
                return true;
            },
            Key: "Enter",
            Description: "Apply theme",
            Section),
        new KeyHandling.KeyBindingEntry<UiThemeSelectionKeyContext>(
            Matches: static k => k.Key == ConsoleKey.Escape,
            Action: static (_, _) => true,
            Key: "Escape",
            Description: "Cancel",
            Section),
    ];

    private static readonly Lazy<IReadOnlyList<KeyHandling.KeyBindingEntry<UiThemeSelectionKeyContext>>> s_entries =
        new(GetEntries);

    /// <inheritdoc />
    public IReadOnlyList<KeyBinding> GetBindings() =>
        s_entries.Value.Select(e => e.ToKeyBinding()).ToList();

    /// <inheritdoc />
    public bool Handle(ConsoleKeyInfo key, UiThemeSelectionKeyContext context)
    {
        foreach (var entry in s_entries.Value)
        {
            if (entry.Matches(key))
            {
                return entry.Action(key, context);
            }
        }

        return false;
    }
}
