using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using KeyHandling = AudioAnalyzer.Console.KeyHandling;

namespace AudioAnalyzer.Console;

/// <summary>Keys for the Preset editor layer picker overlay: navigate, Enter confirm, Esc cancel.</summary>
internal sealed class LayerPickerKeyHandlerConfig : IKeyHandlerConfig<LayerPickerKeyContext>
{
    private const string Section = "Layer picker";

    private static IReadOnlyList<KeyHandling.KeyBindingEntry<LayerPickerKeyContext>> GetEntries()
    {
        return
        [
            new KeyHandling.KeyBindingEntry<LayerPickerKeyContext>(
                Matches: k => k.Key is ConsoleKey.UpArrow or ConsoleKey.DownArrow
                    or ConsoleKey.Add or ConsoleKey.Subtract or ConsoleKey.OemPlus or ConsoleKey.OemMinus,
                Action: (key, context) =>
                {
                    int n = context.PickableTypes.Count;
                    if (n <= 0)
                    {
                        return false;
                    }

                    bool up = key.Key is ConsoleKey.UpArrow or ConsoleKey.Subtract or ConsoleKey.OemMinus;
                    if (up)
                    {
                        context.SelectedIndex = (context.SelectedIndex - 1 + n) % n;
                    }
                    else
                    {
                        context.SelectedIndex = (context.SelectedIndex + 1) % n;
                    }

                    return false;
                },
                Key: "↑/↓ or +/-",
                Description: "Move highlight",
                Section),
            new KeyHandling.KeyBindingEntry<LayerPickerKeyContext>(
                Matches: k => k.Key == ConsoleKey.Enter,
                Action: (_, context) =>
                {
                    context.ConfirmSelection = true;
                    return true;
                },
                Key: "Enter",
                Description: "Apply highlighted layer type to active slot",
                Section),
            new KeyHandling.KeyBindingEntry<LayerPickerKeyContext>(
                Matches: k => k.Key == ConsoleKey.Escape,
                Action: (_, _) => true,
                Key: "Escape",
                Description: "Cancel",
                Section),
        ];
    }

    private static readonly Lazy<IReadOnlyList<KeyHandling.KeyBindingEntry<LayerPickerKeyContext>>> s_entries =
        new(GetEntries);

    /// <inheritdoc />
    public IReadOnlyList<KeyBinding> GetBindings() =>
        s_entries.Value.Select(e => e.ToKeyBinding()).ToList();

    /// <inheritdoc />
    public bool Handle(ConsoleKeyInfo key, LayerPickerKeyContext context)
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
