using System.Text;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Renders the title bar with hierarchical breadcrumb (app/mode/preset/layer) in cyberpunk/hacker style.</summary>
internal sealed class TitleBarRenderer : ITitleBarRenderer
{
    private static readonly TitleBarPalette s_defaultPalette = new();

    private readonly UiSettings _uiSettings;
    private readonly VisualizerSettings _visualizerSettings;
    private readonly IEnumerable<IVisualizer> _visualizers;

    public TitleBarRenderer(UiSettings uiSettings, VisualizerSettings visualizerSettings, IEnumerable<IVisualizer> visualizers)
    {
        _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
        _visualizerSettings = visualizerSettings ?? throw new ArgumentNullException(nameof(visualizerSettings));
        _visualizers = visualizers ?? throw new ArgumentNullException(nameof(visualizers));
    }

    /// <inheritdoc />
    public (string Line1, string Line2, string Line3) Render(int width)
    {
        width = Math.Max(10, width);
        var palette = _uiSettings.TitleBarPalette ?? s_defaultPalette;

        string breadcrumb = BuildBreadcrumb(palette);
        string truncated = StaticTextViewport.TruncateWithEllipsis(new AnsiText(breadcrumb), width - 2);
        int displayWidth = AnsiConsole.GetDisplayWidth(truncated);
        int contentWidth = width - 2;
        int leftPadding = Math.Max(0, (contentWidth - displayWidth) / 2);
        int rightPadding = contentWidth - displayWidth - leftPadding;

        string line1 = AnsiConsole.ColorCode(palette.Frame) + "╔" + new string('═', width - 2) + "╗" + AnsiConsole.ResetCode;
        string line2 = AnsiConsole.ColorCode(palette.Frame) + "║" + AnsiConsole.ResetCode +
            new string(' ', leftPadding) + truncated + new string(' ', rightPadding) +
            AnsiConsole.ColorCode(palette.Frame) + "║" + AnsiConsole.ResetCode;
        string line3 = AnsiConsole.ColorCode(palette.Frame) + "╚" + new string('═', width - 2) + "╝" + AnsiConsole.ResetCode;

        return (line1, line2, line3);
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
        var textLayers = _visualizers.FirstOrDefault(v =>
            string.Equals(v.TechnicalName, "textlayers", StringComparison.OrdinalIgnoreCase));
        string raw = textLayers?.GetActiveLayerDisplayName() ?? "none";
        int zIndex = textLayers?.GetActiveLayerZIndex() ?? -1;
        return (TextHelpers.Hackerize(raw), zIndex);
    }
}
