using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Application.Palette;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application;

/// <inheritdoc />
public sealed class UiThemeResolver : IUiThemeResolver
{
    private readonly UiSettings _uiSettings;
    private readonly IPaletteRepository _paletteRepo;

    /// <summary>Initializes a new instance of the <see cref="UiThemeResolver"/> class.</summary>
    public UiThemeResolver(UiSettings uiSettings, IPaletteRepository paletteRepo)
    {
        _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
        _paletteRepo = paletteRepo ?? throw new ArgumentNullException(nameof(paletteRepo));
    }

    /// <inheritdoc />
    public UiPalette GetEffectiveUiPalette()
    {
        if (!TryGetThemeMappedPalettes(out UiPalette? ui, out _))
        {
            return _uiSettings.Palette ?? new UiPalette();
        }

        return ui!;
    }

    /// <inheritdoc />
    public TitleBarPalette GetEffectiveTitleBarPalette()
    {
        if (!TryGetThemeMappedPalettes(out _, out TitleBarPalette? titleBar))
        {
            return _uiSettings.TitleBarPalette ?? new TitleBarPalette();
        }

        return titleBar!;
    }

    private bool TryGetThemeMappedPalettes(out UiPalette? ui, out TitleBarPalette? titleBar)
    {
        ui = null;
        titleBar = null;
        string? id = _uiSettings.UiThemePaletteId;
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        var def = _paletteRepo.GetById(id.Trim());
        var colors = ColorPaletteParser.Parse(def);
        if (colors is not { Count: > 0 })
        {
            return false;
        }

        (UiPalette mappedUi, TitleBarPalette mappedTb) = UiThemePaletteMapper.Map(colors);
        ui = mappedUi;
        titleBar = mappedTb;
        return true;
    }
}
