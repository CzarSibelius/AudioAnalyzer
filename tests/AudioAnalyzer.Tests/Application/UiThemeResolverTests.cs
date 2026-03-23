using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using Xunit;

namespace AudioAnalyzer.Tests.Application;

/// <summary>Tests for <see cref="UiThemeResolver"/>.</summary>
public sealed class UiThemeResolverTests
{
    private sealed class FakePaletteRepo : IPaletteRepository
    {
        private readonly Dictionary<string, PaletteDefinition?> _byId = new(StringComparer.OrdinalIgnoreCase);

        public void Set(string id, PaletteDefinition? def) => _byId[id] = def;

        public IReadOnlyList<PaletteInfo> GetAll() => [];

        public PaletteDefinition? GetById(string id) =>
            _byId.TryGetValue(id, out var d) ? d : null;
    }

    [Fact]
    public void GetEffectiveUiPalette_UsesInline_WhenThemeIdNull()
    {
        var ui = new UiPalette { Normal = PaletteColor.FromRgb(10, 20, 30) };
        var settings = new UiSettings { Palette = ui };
        var resolver = new UiThemeResolver(settings, new FakePaletteRepo());

        UiPalette result = resolver.GetEffectiveUiPalette();

        Assert.Equal(ui.Normal, result.Normal);
    }

    [Fact]
    public void GetEffectiveUiPalette_UsesTheme_WhenRepoReturnsColors()
    {
        var settings = new UiSettings
        {
            UiThemePaletteId = "mytheme",
            Palette = new UiPalette { Normal = PaletteColor.FromRgb(99, 99, 99) }
        };
        var repo = new FakePaletteRepo();
        repo.Set("mytheme", new PaletteDefinition
        {
            Name = "T",
            Colors =
            [
                new PaletteColorEntry { R = 1, G = 0, B = 0 },
                new PaletteColorEntry { R = 0, G = 2, B = 0 }
            ]
        });
        var resolver = new UiThemeResolver(settings, repo);

        UiPalette result = resolver.GetEffectiveUiPalette();

        Assert.Equal(PaletteColor.FromRgb(1, 0, 0), result.Normal);
        Assert.Equal(PaletteColor.FromRgb(0, 2, 0), result.Highlighted);
    }

    [Fact]
    public void GetEffectiveUiPalette_FallsBack_WhenPaletteMissing()
    {
        var settings = new UiSettings
        {
            UiThemePaletteId = "missing",
            Palette = new UiPalette { Normal = PaletteColor.FromRgb(5, 5, 5) }
        };
        var repo = new FakePaletteRepo();
        repo.Set("missing", null);
        var resolver = new UiThemeResolver(settings, repo);

        UiPalette result = resolver.GetEffectiveUiPalette();

        Assert.Equal(PaletteColor.FromRgb(5, 5, 5), result.Normal);
    }

    [Fact]
    public void GetEffectiveTitleBarPalette_UsesInline_WhenThemeIdNull()
    {
        var tb = new TitleBarPalette { AppName = PaletteColor.FromRgb(7, 7, 7) };
        var settings = new UiSettings { TitleBarPalette = tb };
        var resolver = new UiThemeResolver(settings, new FakePaletteRepo());

        TitleBarPalette result = resolver.GetEffectiveTitleBarPalette();

        Assert.Equal(tb.AppName, result.AppName);
    }

    [Fact]
    public void GetEffectiveTitleBarPalette_UsesDefaults_WhenNoThemeAndNoInline()
    {
        var settings = new UiSettings();
        var resolver = new UiThemeResolver(settings, new FakePaletteRepo());

        TitleBarPalette result = resolver.GetEffectiveTitleBarPalette();

        Assert.Equal(new TitleBarPalette().AppName, result.AppName);
    }
}
