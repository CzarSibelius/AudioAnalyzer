using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using KeyHandling = AudioAnalyzer.Console.KeyHandling;

namespace AudioAnalyzer.Console;

/// <summary>Keyboard handling for the General Settings hub: menu navigation, device picker, BPM source, application name and default asset folder edits.</summary>
internal sealed class GeneralSettingsHubKeyHandlerConfig : IKeyHandlerConfig<GeneralSettingsHubKeyContext>
{
    private const string Section = "General settings hub";

    private const int MenuAudio = 0;
    private const int MenuBpmSource = 1;
    private const int MenuAppName = 2;
    private const int MenuDefaultAssetFolder = 3;
    private const int MenuTheme = 4;
    private const int MenuShowRenderFps = 5;
    private const int MenuCount = 6;

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
            Description: "Open selected item or confirm text edit",
            Section,
            ApplicableMode: ApplicationMode.Settings),
        new KeyHandling.KeyBindingEntry<GeneralSettingsHubKeyContext>(
            Matches: static k => k.Key == ConsoleKey.T,
            Action: static (_, ctx) => OnThemeHotkey(ctx),
            Key: "T",
            Description: "Open UI theme palette list (when UI theme row selected)",
            Section,
            ApplicableMode: ApplicationMode.Settings),
    ];

    private static bool MoveSelection(GeneralSettingsHubKeyContext ctx, int delta)
    {
        if (ctx.State.EditMode != GeneralSettingsHubEditMode.None)
        {
            return false;
        }

        ctx.State.SelectedIndex = (ctx.State.SelectedIndex + delta + MenuCount) % MenuCount;
        return true;
    }

    private static bool OnEnter(GeneralSettingsHubKeyContext ctx)
    {
        return ctx.State.SelectedIndex switch
        {
            MenuAudio => OpenDevicePicker(ctx),
            MenuBpmSource => CycleBpmSource(ctx),
            MenuAppName => StartApplicationNameEdit(ctx),
            MenuDefaultAssetFolder => StartDefaultAssetFolderEdit(ctx),
            MenuTheme => OpenThemePicker(ctx),
            MenuShowRenderFps => ToggleShowRenderFps(ctx),
            _ => false
        };
    }

    private static bool ToggleShowRenderFps(GeneralSettingsHubKeyContext ctx)
    {
        ctx.UiSettings.ShowRenderFps = !ctx.UiSettings.ShowRenderFps;
        ctx.SaveSettings();
        return true;
    }

    private static bool CycleBpmSource(GeneralSettingsHubKeyContext ctx)
    {
        int next = ((int)ctx.AppSettings.BpmSource + 1) % Enum.GetValues<BpmSource>().Length;
        ctx.AppSettings.BpmSource = (BpmSource)next;
        ctx.ApplyBeatTimingFromSettings();
        return true;
    }

    private static bool OnThemeHotkey(GeneralSettingsHubKeyContext ctx)
    {
        if (ctx.State.EditMode != GeneralSettingsHubEditMode.None)
        {
            return false;
        }

        if (ctx.State.SelectedIndex != MenuTheme)
        {
            return false;
        }

        return OpenThemePicker(ctx);
    }

    private static bool StartApplicationNameEdit(GeneralSettingsHubKeyContext ctx)
    {
        ctx.State.EditMode = GeneralSettingsHubEditMode.ApplicationName;
        ctx.State.RenameBuffer = ctx.UiSettings.TitleBarAppName?.Trim() ?? "";
        return true;
    }

    private static bool StartDefaultAssetFolderEdit(GeneralSettingsHubKeyContext ctx)
    {
        ctx.State.EditMode = GeneralSettingsHubEditMode.DefaultAssetFolder;
        ctx.State.RenameBuffer = ctx.UiSettings.DefaultAssetFolderPath?.Trim() ?? "";
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

    private static bool OpenThemePicker(GeneralSettingsHubKeyContext ctx)
    {
        ctx.UiThemeSelectionModal.Show(ctx.SetModalOpen, ctx.GetAnalysisSnapshot, ctx.SaveSettings);
        if (!ctx.DisplayState.FullScreen)
        {
            ctx.Orchestrator.RedrawWithFullHeader();
        }
        else
        {
            ctx.Orchestrator.Redraw();
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
        if (ctx.State.EditMode != GeneralSettingsHubEditMode.None)
        {
            return HandleTextEditKey(key, ctx);
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

    private static bool HandleTextEditKey(ConsoleKeyInfo key, GeneralSettingsHubKeyContext ctx)
    {
        if (key.Key == ConsoleKey.Enter)
        {
            switch (ctx.State.EditMode)
            {
                case GeneralSettingsHubEditMode.ApplicationName:
                    ctx.UiSettings.TitleBarAppName = string.IsNullOrWhiteSpace(ctx.State.RenameBuffer)
                        ? null
                        : ctx.State.RenameBuffer.Trim();
                    break;
                case GeneralSettingsHubEditMode.DefaultAssetFolder:
                    ctx.UiSettings.DefaultAssetFolderPath = string.IsNullOrWhiteSpace(ctx.State.RenameBuffer)
                        ? null
                        : ctx.State.RenameBuffer.Trim();
                    break;
                default:
                    return false;
            }

            ctx.State.EditMode = GeneralSettingsHubEditMode.None;
            ctx.State.RenameBuffer = "";
            ctx.SaveSettings();
            return true;
        }

        if (key.Key == ConsoleKey.Escape)
        {
            ctx.State.EditMode = GeneralSettingsHubEditMode.None;
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
