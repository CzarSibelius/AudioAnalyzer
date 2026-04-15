using System.Globalization;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using KeyHandling = AudioAnalyzer.Console.KeyHandling;

namespace AudioAnalyzer.Console;

/// <summary>Keyboard handling for the General Settings hub: menu navigation, device picker, BPM source, app settings, and text edits.</summary>
internal sealed class GeneralSettingsHubKeyHandlerConfig : IKeyHandlerConfig<GeneralSettingsHubKeyContext>
{
    private const string Section = "General settings hub";

    private static IReadOnlyList<KeyHandling.KeyBindingEntry<GeneralSettingsHubKeyContext>> GetEntries() =>
    [
        new KeyHandling.KeyBindingEntry<GeneralSettingsHubKeyContext>(
            Matches: static k => k.Key is ConsoleKey.Add or ConsoleKey.OemPlus or ConsoleKey.Subtract or ConsoleKey.OemMinus,
            Action: static (k, ctx) => TryStepMaxAudioHistorySeconds(ctx, k),
            Key: "+ / -",
            Description: "Adjust max audio history (seconds) when that row is selected",
            Section,
            ApplicableMode: ApplicationMode.Settings),
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

    private static bool TryStepMaxAudioHistorySeconds(GeneralSettingsHubKeyContext ctx, ConsoleKeyInfo key)
    {
        if (ctx.State.EditMode != GeneralSettingsHubEditMode.None
            || ctx.State.SelectedIndex != GeneralSettingsHubMenuRows.MaxAudioHistorySeconds)
        {
            return false;
        }

        bool increase = key.Key is ConsoleKey.Add or ConsoleKey.OemPlus;
        bool decrease = key.Key is ConsoleKey.Subtract or ConsoleKey.OemMinus;
        if (!increase && !decrease)
        {
            return false;
        }

        double delta = increase ? 5.0 : -5.0;
        double next = ClampHistorySeconds(ctx.AppSettings.MaxAudioHistorySeconds + delta);
        ctx.AppSettings.MaxAudioHistorySeconds = next;
        ctx.WaveformHistoryConfigurator.ApplyMaxHistorySeconds(next, null);
        ctx.SaveSettings();
        RedrawGeneralHub(ctx);
        return true;
    }

    private static double ClampHistorySeconds(double seconds)
    {
        if (double.IsNaN(seconds) || double.IsInfinity(seconds))
        {
            return 60.0;
        }

        return Math.Clamp(seconds, 5.0, 180.0);
    }

    private static void RedrawGeneralHub(GeneralSettingsHubKeyContext ctx)
    {
        if (!ctx.DisplayState.FullScreen)
        {
            ctx.Orchestrator.RedrawWithFullHeader();
        }
        else
        {
            ctx.Orchestrator.Redraw();
        }
    }

    private static bool MoveSelection(GeneralSettingsHubKeyContext ctx, int delta)
    {
        if (ctx.State.EditMode != GeneralSettingsHubEditMode.None)
        {
            return false;
        }

        ctx.State.SelectedIndex = (ctx.State.SelectedIndex + delta + GeneralSettingsHubMenuRows.Count) % GeneralSettingsHubMenuRows.Count;
        return true;
    }

    private static bool OnEnter(GeneralSettingsHubKeyContext ctx)
    {
        return ctx.State.SelectedIndex switch
        {
            GeneralSettingsHubMenuRows.Audio => OpenDevicePicker(ctx),
            GeneralSettingsHubMenuRows.BpmSource => CycleBpmSource(ctx),
            GeneralSettingsHubMenuRows.ApplicationName => StartApplicationNameEdit(ctx),
            GeneralSettingsHubMenuRows.MaxAudioHistorySeconds => StartMaxAudioHistorySecondsEdit(ctx),
            GeneralSettingsHubMenuRows.DefaultAssetFolder => StartDefaultAssetFolderEdit(ctx),
            GeneralSettingsHubMenuRows.UiTheme => OpenThemePicker(ctx),
            GeneralSettingsHubMenuRows.ShowRenderFps => ToggleShowRenderFps(ctx),
            GeneralSettingsHubMenuRows.ShowLayerRenderTime => ToggleShowLayerRenderTime(ctx),
            _ => false
        };
    }

    private static bool ToggleShowRenderFps(GeneralSettingsHubKeyContext ctx)
    {
        ctx.UiSettings.ShowRenderFps = !ctx.UiSettings.ShowRenderFps;
        ctx.SaveSettings();
        return true;
    }

    private static bool ToggleShowLayerRenderTime(GeneralSettingsHubKeyContext ctx)
    {
        ctx.UiSettings.ShowLayerRenderTime = !ctx.UiSettings.ShowLayerRenderTime;
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

        if (ctx.State.SelectedIndex != GeneralSettingsHubMenuRows.UiTheme)
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

    private static bool StartMaxAudioHistorySecondsEdit(GeneralSettingsHubKeyContext ctx)
    {
        ctx.State.EditMode = GeneralSettingsHubEditMode.MaxAudioHistorySeconds;
        ctx.State.RenameBuffer = ctx.AppSettings.MaxAudioHistorySeconds.ToString("F0", CultureInfo.InvariantCulture);
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
        ctx.UiThemeSelectionModal.Show(ctx.SetModalOpen, ctx.GetAudioAnalysisSnapshot, ctx.SaveSettings);
        RedrawGeneralHub(ctx);
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
                case GeneralSettingsHubEditMode.MaxAudioHistorySeconds:
                    if (!double.TryParse(
                            ctx.State.RenameBuffer.Trim(),
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out double parsed))
                    {
                        return false;
                    }

                    double clamped = ClampHistorySeconds(parsed);
                    ctx.AppSettings.MaxAudioHistorySeconds = clamped;
                    ctx.WaveformHistoryConfigurator.ApplyMaxHistorySeconds(clamped, null);
                    break;
                default:
                    return false;
            }

            ctx.State.EditMode = GeneralSettingsHubEditMode.None;
            ctx.State.RenameBuffer = "";
            ctx.SaveSettings();
            RedrawGeneralHub(ctx);
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
