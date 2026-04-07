using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Resolves effective <see cref="UiPalette"/> and <see cref="TitleBarPalette"/> from
/// <see cref="UiSettings.UiThemeId"/> (<c>themes/</c> JSON) or inline settings.
/// </summary>
public interface IUiThemeResolver
{
    /// <summary>Returns UI chrome colors (header, toolbar labels, modals).</summary>
    UiPalette GetEffectiveUiPalette();

    /// <summary>Returns title bar breadcrumb colors; never null.</summary>
    TitleBarPalette GetEffectiveTitleBarPalette();
}
