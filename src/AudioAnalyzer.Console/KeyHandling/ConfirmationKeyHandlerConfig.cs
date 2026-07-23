using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using KeyHandling = AudioAnalyzer.Console.KeyHandling;

namespace AudioAnalyzer.Console;

/// <summary>Keys for the reusable yes/no confirmation modal: Y/Enter confirm, N/Esc cancel (ADR-0093).</summary>
internal sealed class ConfirmationKeyHandlerConfig : IKeyHandlerConfig<ConfirmationKeyContext>
{
    private const string Section = "Confirmation";

    private static IReadOnlyList<KeyHandling.KeyBindingEntry<ConfirmationKeyContext>> GetEntries() =>
    [
        new KeyHandling.KeyBindingEntry<ConfirmationKeyContext>(
            Matches: static k => k.Key is ConsoleKey.Y or ConsoleKey.Enter,
            Action: static (_, ctx) =>
            {
                ctx.Result = true;
                return true;
            },
            Key: "Y/Enter",
            Description: "Confirm",
            Section),
        new KeyHandling.KeyBindingEntry<ConfirmationKeyContext>(
            Matches: static k => k.Key is ConsoleKey.N or ConsoleKey.Escape,
            Action: static (_, ctx) =>
            {
                ctx.Result = false;
                return true;
            },
            Key: "N/Esc",
            Description: "Cancel",
            Section),
    ];

    private static readonly Lazy<IReadOnlyList<KeyHandling.KeyBindingEntry<ConfirmationKeyContext>>> s_entries =
        new(GetEntries);

    /// <inheritdoc />
    public IReadOnlyList<KeyBinding> GetBindings() =>
        s_entries.Value.Select(e => e.ToKeyBinding()).ToList();

    /// <inheritdoc />
    public bool Handle(ConsoleKeyInfo key, ConfirmationKeyContext context)
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
