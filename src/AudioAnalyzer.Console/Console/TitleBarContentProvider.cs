using System.Text;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Builds the title bar breadcrumb (app/mode/preset/layer) in cyberpunk/hacker style for use as preformatted row content.</summary>
internal sealed class TitleBarContentProvider : ITitleBarContentProvider
{
    private static readonly TitleBarPalette s_defaultPalette = new();

    private readonly UiSettings _uiSettings;
    private readonly VisualizerSettings _visualizerSettings;
    private readonly IVisualizer _visualizer;

    public TitleBarContentProvider(UiSettings uiSettings, VisualizerSettings visualizerSettings, IVisualizer visualizer)
    {
        _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
        _visualizerSettings = visualizerSettings ?? throw new ArgumentNullException(nameof(visualizerSettings));
        _visualizer = visualizer ?? throw new ArgumentNullException(nameof(visualizer));
    }

    /// <inheritdoc />
    public IDisplayText GetTitleBarContent()
    {
        var palette = _uiSettings.TitleBarPalette ?? s_defaultPalette;
        string breadcrumb = BuildBreadcrumb(palette);
        return new AnsiText(breadcrumb);
    }

    private string BuildBreadcrumb(TitleBarPalette palette)
    {
        var sb = new StringBuilder();

        string appName = GetAppName();
        string mode = GetModeName();
        string preset = GetPresetName();
        var (layerName, layerZIndex) = GetLayerNameAndZIndex();

        AnsiConsole.AppendColored(sb, appName, palette.AppName);
        sb.Append(AnsiConsole.ColorCode(palette.Separator));
        sb.Append('/');
        sb.Append(AnsiConsole.ResetCode);

        AnsiConsole.AppendColored(sb, mode, palette.Mode);
        sb.Append(AnsiConsole.ColorCode(palette.Separator));
        sb.Append('/');
        sb.Append(AnsiConsole.ResetCode);

        AnsiConsole.AppendColored(sb, preset, palette.Preset);

        if (layerZIndex >= 0)
        {
            sb.Append(AnsiConsole.ColorCode(palette.Separator));
            sb.Append('[');
            sb.Append(layerZIndex);
            sb.Append("]:");
            sb.Append(AnsiConsole.ResetCode);
        }
        else
        {
            sb.Append(AnsiConsole.ColorCode(palette.Separator));
            sb.Append('/');
            sb.Append(AnsiConsole.ResetCode);
        }

        AnsiConsole.AppendColored(sb, layerName, palette.Layer);

        return sb.ToString();
    }

    private string GetAppName()
    {
        if (!string.IsNullOrWhiteSpace(_uiSettings.TitleBarAppName))
        {
            return _uiSettings.TitleBarAppName.Trim();
        }
        return DeriveAppNameFromTitle(_uiSettings.Title);
    }

    private static string DeriveAppNameFromTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "AudioAnalyzer";
        }
        var t = title.Trim();
        if (t.Contains("AUDIO", StringComparison.OrdinalIgnoreCase) && t.Contains("ANALYZER", StringComparison.OrdinalIgnoreCase))
        {
            return "aUdioNLZR";
        }
        var words = t.Split([' ', '-', '_'], StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return "app";
        }
        var first = words[0];
        if (first.Length > 0)
        {
            return char.ToLowerInvariant(first[0]) + (first.Length > 1 ? first[1..] : "");
        }
        return "app";
    }

    private string GetModeName()
    {
        string raw = _visualizerSettings.ApplicationMode == ApplicationMode.ShowPlay ? "Show" : "Preset";
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
