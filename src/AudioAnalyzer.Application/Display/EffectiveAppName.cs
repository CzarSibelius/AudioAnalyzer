using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Display;

/// <summary>
/// Resolves the short application name shown in the title bar and General Settings hub (same rules as <see cref="TitleBarBreadcrumbFormatter"/>).
/// </summary>
public static class EffectiveAppName
{
    /// <summary>Returns <see cref="UiSettings.TitleBarAppName"/> when set; otherwise derives from <see cref="UiSettings.Title"/>.</summary>
    public static string FromUiSettings(UiSettings uiSettings)
    {
        ArgumentNullException.ThrowIfNull(uiSettings);
        if (!string.IsNullOrWhiteSpace(uiSettings.TitleBarAppName))
        {
            return uiSettings.TitleBarAppName.Trim();
        }

        return DeriveFromTitle(uiSettings.Title);
    }

    /// <summary>Derives a short display name from the full window title when <see cref="UiSettings.TitleBarAppName"/> is unset.</summary>
    public static string DeriveFromTitle(string title)
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
}
