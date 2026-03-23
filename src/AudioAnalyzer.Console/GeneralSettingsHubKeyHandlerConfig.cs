using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using KeyHandling = AudioAnalyzer.Console.KeyHandling;

namespace AudioAnalyzer.Console;

/// <summary>Keyboard handling for the General Settings hub: menu navigation, device picker, application name edit.</summary>
internal sealed class GeneralSettingsHubKeyHandlerConfig : IKeyHandlerConfig<GeneralSettingsHubKeyContext>
{
    private const string Section = "General settings hub";

    private const int MenuAudio = 0;
    private const int MenuAppName = 1;
    private const int MenuCount = 2;

    private static IReadOnlyList<KeyHandling.KeyBindingEntry<GeneralSettingsHubKeyContext>> GetEntries() =>
    [
        new KeyHandling.KeyBindingEntry<GeneralSettingsHubKeyContext>(
            Matches: static k => k.Key is ConsoleKey.UpArrow,
            Action: static (_, ctx) => MoveSelection(ctx, -1),
            Key: "Up",
            Description: "Previous menu row",
            Section,
            ApplicableMode: ApplicationMode.Settings),
        new KeyHandling.KeyBindingEntry<GeneralSettingsHubKeyContext>(
            Matches: static k => k.Key is ConsoleKey.DownArrow,
            Action: static (_, ctx) => MoveSelection(ctx, 1),
            Key: "Down",
            Description: "Next menu row",
            Section,
            ApplicableMode: ApplicationMode.Settings),
        new KeyHandling.KeyBindingEntry<GeneralSettingsHubKeyContext>(
            Matches: static k => k.Key == ConsoleKey.Enter,
            Action: static (_, ctx) => OnEnter(ctx),
            Key: "Enter",
            Description: "Open selected item or confirm application name",
            Section,
            ApplicableMode: ApplicationMode.Settings),
    ];

    private static bool MoveSelection(GeneralSettingsHubKeyContext ctx, int delta)
    {
        if (ctx.State.IsEditingAppName)
        {
            return false;
        }

        ctx.State.SelectedIndex = (ctx.State.SelectedIndex + delta + MenuCount) % MenuCount;
        return true;
    }

    private static bool OnEnter(GeneralSettingsHubKeyContext ctx)
    {
        if (ctx.State.IsEditingAppName)
        {
            ctx.UiSettings.TitleBarAppName = string.IsNullOrWhiteSpace(ctx.State.RenameBuffer)
                ? null
                : ctx.State.RenameBuffer.Trim();
            ctx.State.IsEditingAppName = false;
            ctx.State.RenameBuffer = "";
            ctx.SaveSettings();
            return true;
        }

        return ctx.State.SelectedIndex switch
        {
            MenuAudio => OpenDevicePicker(ctx),
            MenuAppName => StartRename(ctx),
            _ => false
        };
    }

    private static bool StartRename(GeneralSettingsHubKeyContext ctx)
    {
        ctx.State.IsEditingAppName = true;
        ctx.State.RenameBuffer = ctx.UiSettings.TitleBarAppName?.Trim() ?? "";
        return true;
    }

    private static bool OpenDevicePicker(GeneralSettingsHubKeyContext ctx)
    {
        ctx.StopCapture();
        var (newId, newName) = ctx.DeviceSelectionModal.Show(ctx.GetDeviceName(), ctx.SetModalOpen);
        if (newName != "")
        {
            ctx.StartCapture(newId, newName);
        }
        else
        {
            ctx.RestartCapture();
        }

        return true;
    }

    private static readonly Lazy<IReadOnlyList<KeyHandling.KeyBindingEntry<GeneralSettingsHubKeyContext>>> s_entries =
        new(GetEntries);

    /// <inheritdoc />
    public IReadOnlyList<KeyBinding> GetBindings() =>
        s_entries.Value.Select(e => e.ToKeyBinding()).ToList();

    /// <inheritdoc />
    public bool Handle(ConsoleKeyInfo key, GeneralSettingsHubKeyContext ctx)
    {
        if (ctx.State.IsEditingAppName)
        {
            return HandleRenameKey(key, ctx);
        }

        foreach (var entry in s_entries.Value)
        {
            if (entry.Matches(key))
            {
                return entry.Action(key, ctx);
            }
        }

        return false;
    }

    private static bool HandleRenameKey(ConsoleKeyInfo key, GeneralSettingsHubKeyContext ctx)
    {
        if (key.Key == ConsoleKey.Enter)
        {
            ctx.UiSettings.TitleBarAppName = string.IsNullOrWhiteSpace(ctx.State.RenameBuffer)
                ? null
                : ctx.State.RenameBuffer.Trim();
            ctx.State.IsEditingAppName = false;
            ctx.State.RenameBuffer = "";
            ctx.SaveSettings();
            return true;
        }

        if (key.Key == ConsoleKey.Escape)
        {
            ctx.State.IsEditingAppName = false;
            ctx.State.RenameBuffer = "";
            return true;
        }

        if (key.Key == ConsoleKey.Backspace && ctx.State.RenameBuffer.Length > 0)
        {
            ctx.State.RenameBuffer = ctx.State.RenameBuffer[..^1];
            return true;
        }

        if (key.KeyChar is >= ' ' and <= '~')
        {
            ctx.State.RenameBuffer += key.KeyChar;
            return true;
        }

        return false;
    }
}
