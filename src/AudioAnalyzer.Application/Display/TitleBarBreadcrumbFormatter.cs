using System.Linq;
using System.Text;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Display;

/// <summary>Builds the universal title breadcrumb (ANSI) per ADR-0036 and ADR-0060.</summary>
public sealed class TitleBarBreadcrumbFormatter : ITitleBarBreadcrumbFormatter
{
    private static readonly TitleBarPalette s_defaultPalette = new();

    private readonly UiSettings _uiSettings;
    private readonly VisualizerSettings _visualizerSettings;
    private readonly IVisualizer _visualizer;
    private readonly ITitleBarNavigationContext _navigation;

    public TitleBarBreadcrumbFormatter(
        UiSettings uiSettings,
        VisualizerSettings visualizerSettings,
        IVisualizer visualizer,
        ITitleBarNavigationContext navigation)
    {
        _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
        _visualizerSettings = visualizerSettings ?? throw new ArgumentNullException(nameof(visualizerSettings));
        _visualizer = visualizer ?? throw new ArgumentNullException(nameof(visualizer));
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
    }

    /// <inheritdoc />
    public string BuildAnsiLine()
    {
        var palette = _uiSettings.TitleBarPalette ?? s_defaultPalette;
        return _navigation.View switch
        {
            TitleBarViewKind.Main => BuildMainView(palette),
            TitleBarViewKind.PresetSettingsModal => BuildPresetModal(palette),
            TitleBarViewKind.ShowEditModal => BuildPresetScopedSuffix(palette, "showedit"),
            TitleBarViewKind.HelpModal => BuildPresetScopedSuffix(palette, "help"),
            TitleBarViewKind.DeviceAudioInputModal => BuildDeviceAudioInput(palette),
            TitleBarViewKind.SettingsHub => BuildSettingsHub(palette),
            _ => BuildMainView(palette)
        };
    }

    private string BuildMainView(TitleBarPalette palette)
    {
        if (_visualizerSettings.ApplicationMode == ApplicationMode.Settings)
        {
            return BuildSettingsHubHome(palette);
        }

        var sb = new StringBuilder();
        string appName = EffectiveAppName.FromUiSettings(_uiSettings);
        string mode = GetModeName();
        string preset = GetPresetName();
        var (layerName, layerZIndex) = GetLayerNameAndZIndex();

        AnsiConsole.AppendColored(sb, appName, palette.AppName);
        AppendSlash(sb, palette);
        AnsiConsole.AppendColored(sb, mode, palette.Mode);
        AppendSlash(sb, palette);
        AnsiConsole.AppendColored(sb, preset, palette.Preset);

        if (layerZIndex >= 0)
        {
            sb.Append(AnsiConsole.ColorCode(palette.Separator));
            sb.Append('[');
            sb.Append(layerZIndex + 1);
            sb.Append("]:");
            sb.Append(AnsiConsole.ResetCode);
        }
        else
        {
            AppendSlash(sb, palette);
        }

        AnsiConsole.AppendColored(sb, layerName, palette.Layer);
        return sb.ToString();
    }

    private string BuildPresetModal(TitleBarPalette palette)
    {
        var sb = new StringBuilder();
        AppendAppModePreset(sb, palette);

        if (_navigation.PresetSettingsLayerOneBased is >= 1
            && !string.IsNullOrEmpty(_navigation.PresetSettingsLayerTypeRaw))
        {
            sb.Append(AnsiConsole.ColorCode(palette.Separator));
            sb.Append('[');
            sb.Append(_navigation.PresetSettingsLayerOneBased.Value);
            sb.Append("]:");
            sb.Append(AnsiConsole.ResetCode);
            AnsiConsole.AppendColored(sb, TextHelpers.Hackerize(_navigation.PresetSettingsLayerTypeRaw!), palette.Layer);
        }

        if (!string.IsNullOrEmpty(_navigation.PresetSettingsFocusedSettingId))
        {
            AppendSlash(sb, palette);
            AnsiConsole.AppendColored(sb, TextHelpers.Hackerize(_navigation.PresetSettingsFocusedSettingId!), palette.Layer);
        }

        if (_navigation.PresetSettingsPalettePickerActive)
        {
            AppendSlash(sb, palette);
            AnsiConsole.AppendColored(sb, TextHelpers.Hackerize("editor"), palette.Layer);
        }

        return sb.ToString();
    }

    private string BuildPresetScopedSuffix(TitleBarPalette palette, string rawSuffix)
    {
        var sb = new StringBuilder();
        AppendAppModePreset(sb, palette);
        AppendSlash(sb, palette);
        AnsiConsole.AppendColored(sb, TextHelpers.Hackerize(rawSuffix), palette.Layer);
        return sb.ToString();
    }

    private string BuildDeviceAudioInput(TitleBarPalette palette)
    {
        var sb = new StringBuilder();
        string appName = EffectiveAppName.FromUiSettings(_uiSettings);
        AnsiConsole.AppendColored(sb, appName, palette.AppName);
        AppendSlash(sb, palette);
        AnsiConsole.AppendColored(sb, TextHelpers.Hackerize("settings"), palette.Mode);
        AppendSlash(sb, palette);
        AnsiConsole.AppendColored(sb, TextHelpers.Hackerize("audioinput"), palette.Preset);
        return sb.ToString();
    }

    private string BuildSettingsHub(TitleBarPalette palette) => BuildSettingsHubHome(palette);

    private string BuildSettingsHubHome(TitleBarPalette palette)
    {
        var sb = new StringBuilder();
        string appName = EffectiveAppName.FromUiSettings(_uiSettings);
        AnsiConsole.AppendColored(sb, appName, palette.AppName);
        AppendSlash(sb, palette);
        AnsiConsole.AppendColored(sb, TextHelpers.Hackerize("settings"), palette.Mode);
        return sb.ToString();
    }

    private void AppendAppModePreset(StringBuilder sb, TitleBarPalette palette)
    {
        string appName = EffectiveAppName.FromUiSettings(_uiSettings);
        string mode = GetModeName();
        string preset = GetPresetName();
        AnsiConsole.AppendColored(sb, appName, palette.AppName);
        AppendSlash(sb, palette);
        AnsiConsole.AppendColored(sb, mode, palette.Mode);
        AppendSlash(sb, palette);
        AnsiConsole.AppendColored(sb, preset, palette.Preset);
    }

    private static void AppendSlash(StringBuilder sb, TitleBarPalette palette)
    {
        sb.Append(AnsiConsole.ColorCode(palette.Separator));
        sb.Append('/');
        sb.Append(AnsiConsole.ResetCode);
    }

    private string GetModeName()
    {
        string raw = _visualizerSettings.ApplicationMode switch
        {
            ApplicationMode.ShowPlay => "Show",
            ApplicationMode.Settings => "Settings",
            _ => "Preset"
        };
        return TextHelpers.Hackerize(raw);
    }

    private string GetPresetName()
    {
        if (_visualizerSettings.Presets is not { Count: > 0 })
        {
            return TextHelpers.Hackerize("default");
        }
        var active = _visualizerSettings.Presets.FirstOrDefault(p =>
            string.Equals(p.Id, _visualizerSettings.ActivePresetId, StringComparison.OrdinalIgnoreCase))
            ?? _visualizerSettings.Presets[0];
        string sanitized = string.IsNullOrWhiteSpace(active.Name) ? "Preset 1" : SanitizePresetName(active.Name.Trim());
        return TextHelpers.Hackerize(sanitized);
    }

    private static string SanitizePresetName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "default";
        }
        var sb = new StringBuilder();
        foreach (char c in name)
        {
            if (char.IsLetterOrDigit(c) || c == '_' || c == '-')
            {
                sb.Append(c);
            }
            else if (char.IsWhiteSpace(c))
            {
                sb.Append('_');
            }
        }
        return sb.Length > 0 ? sb.ToString() : "default";
    }

    private (string Name, int ZIndex) GetLayerNameAndZIndex()
    {
        string raw = _visualizer.GetActiveLayerDisplayName() ?? "none";
        int zIndex = _visualizer.GetActiveLayerZIndex();
        return (TextHelpers.Hackerize(raw), zIndex);
    }
}
