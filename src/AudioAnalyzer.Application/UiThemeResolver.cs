using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application;

/// <inheritdoc />
public sealed class UiThemeResolver : IUiThemeResolver
{
    private readonly UiSettings _uiSettings;
    private readonly IPaletteRepository _paletteRepo;
    private readonly IUiThemeRepository _themeRepo;

    /// <summary>Initializes a new instance of the <see cref="UiThemeResolver"/> class.</summary>
    public UiThemeResolver(UiSettings uiSettings, IPaletteRepository paletteRepo, IUiThemeRepository themeRepo)
    {
        _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
        _paletteRepo = paletteRepo ?? throw new ArgumentNullException(nameof(paletteRepo));
        _themeRepo = themeRepo ?? throw new ArgumentNullException(nameof(themeRepo));
    }

    /// <inheritdoc />
    public UiPalette GetEffectiveUiPalette()
    {
        if (!TryResolveFromTheme(out UiPalette? ui, out _))
        {
            return _uiSettings.Palette ?? new UiPalette();
        }

        return ui!;
    }

    /// <inheritdoc />
    public TitleBarPalette GetEffectiveTitleBarPalette()
    {
        if (!TryResolveFromTheme(out _, out TitleBarPalette? titleBar))
        {
            return _uiSettings.TitleBarPalette ?? new TitleBarPalette();
        }

        return titleBar!;
    }

    private bool TryResolveFromTheme(out UiPalette? ui, out TitleBarPalette? titleBar)
    {
        ui = null;
        titleBar = null;
        string? id = _uiSettings.UiThemeId;
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        var theme = _themeRepo.GetById(id.Trim());
        if (theme == null)
        {
            return false;
        }

        (UiPalette baseUi, TitleBarPalette baseTb) = UiThemeMerger.ResolveBase(
            theme.FallbackPaletteId,
            _uiSettings,
            _paletteRepo);
        (ui, titleBar) = UiThemeMerger.MergeOverlay(theme, baseUi, baseTb);
        return true;
    }
}
