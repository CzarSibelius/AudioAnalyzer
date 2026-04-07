using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Palette;
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

    private sealed class FakeUiThemeRepo : IUiThemeRepository
    {
        private readonly Dictionary<string, UiThemeDefinition> _byId = new(StringComparer.OrdinalIgnoreCase);

        public void Set(string id, UiThemeDefinition? def)
        {
            if (def == null)
            {
                _byId.Remove(id);
            }
            else
            {
                _byId[id] = def;
            }
        }

        public IReadOnlyList<ThemeInfo> GetAll() =>
            _byId.Select(kv =>
            {
                string trimmed = kv.Value.Name?.Trim() ?? "";
                string display = trimmed.Length > 0 ? trimmed : kv.Key;
                return new ThemeInfo(kv.Key, display, kv.Value.FallbackPaletteId);
            })
            .OrderBy(t => t.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        public UiThemeDefinition? GetById(string id) =>
            _byId.TryGetValue(id, out var d) ? d : null;

        public void Save(string id, UiThemeDefinition definition) => _byId[id] = definition;

        public string Create(UiThemeDefinition definition)
        {
            string id = "created-" + _byId.Count;
            Save(id, definition);
            return id;
        }
    }

    [Fact]
    public void GetEffectiveUiPalette_UsesInline_WhenThemeIdNull()
    {
        var ui = new UiPalette { Normal = PaletteColor.FromRgb(10, 20, 30) };
        var settings = new UiSettings { Palette = ui };
        var resolver = new UiThemeResolver(settings, new FakePaletteRepo(), new FakeUiThemeRepo());

        UiPalette result = resolver.GetEffectiveUiPalette();

        Assert.Equal(ui.Normal, result.Normal);
    }

    [Fact]
    public void GetEffectiveUiPalette_UsesThemeFallbackMapper_WhenThemeHasFallbackPalette()
    {
        var settings = new UiSettings
        {
            UiThemeId = "app",
            Palette = new UiPalette { Normal = PaletteColor.FromRgb(99, 99, 99) }
        };
        var paletteRepo = new FakePaletteRepo();
        paletteRepo.Set("pal", new PaletteDefinition
        {
            Name = "P",
            Colors =
            [
                new PaletteColorEntry { R = 1, G = 0, B = 0 },
                new PaletteColorEntry { R = 0, G = 2, B = 0 }
            ]
        });
        var themeRepo = new FakeUiThemeRepo();
        themeRepo.Set("app", new UiThemeDefinition { FallbackPaletteId = "pal" });
        var resolver = new UiThemeResolver(settings, paletteRepo, themeRepo);

        UiPalette result = resolver.GetEffectiveUiPalette();

        Assert.Equal(PaletteColor.FromRgb(1, 0, 0), result.Normal);
        Assert.Equal(PaletteColor.FromRgb(0, 2, 0), result.Highlighted);
    }

    [Fact]
    public void GetEffectiveUiPalette_OverlaysExplicitThemeUiSlots()
    {
        var settings = new UiSettings
        {
            UiThemeId = "app",
            Palette = new UiPalette { Normal = PaletteColor.FromRgb(1, 1, 1) }
        };
        var paletteRepo = new FakePaletteRepo();
        paletteRepo.Set("pal", new PaletteDefinition
        {
            Colors = [new PaletteColorEntry { R = 5, G = 5, B = 5 }]
        });
        var themeRepo = new FakeUiThemeRepo();
        themeRepo.Set("app", new UiThemeDefinition
        {
            FallbackPaletteId = "pal",
            Ui = new UiThemeUiSection
            {
                Normal = new PaletteColorEntry { R = 9, G = 8, B = 7 }
            }
        });
        var resolver = new UiThemeResolver(settings, paletteRepo, themeRepo);

        UiPalette result = resolver.GetEffectiveUiPalette();

        Assert.Equal(PaletteColor.FromRgb(9, 8, 7), result.Normal);
        Assert.Equal(PaletteColor.FromRgb(5, 5, 5), result.Highlighted);
    }

    [Fact]
    public void GetEffectiveUiPalette_FallsBack_WhenThemeMissing()
    {
        var settings = new UiSettings
        {
            UiThemeId = "missing",
            Palette = new UiPalette { Normal = PaletteColor.FromRgb(5, 5, 5) }
        };
        var resolver = new UiThemeResolver(settings, new FakePaletteRepo(), new FakeUiThemeRepo());

        UiPalette result = resolver.GetEffectiveUiPalette();

        Assert.Equal(PaletteColor.FromRgb(5, 5, 5), result.Normal);
    }

    [Fact]
    public void GetEffectiveTitleBarPalette_UsesInline_WhenThemeIdNull()
    {
        var tb = new TitleBarPalette { AppName = PaletteColor.FromRgb(7, 7, 7) };
        var settings = new UiSettings { TitleBarPalette = tb };
        var resolver = new UiThemeResolver(settings, new FakePaletteRepo(), new FakeUiThemeRepo());

        TitleBarPalette result = resolver.GetEffectiveTitleBarPalette();

        Assert.Equal(tb.AppName, result.AppName);
    }

    [Fact]
    public void GetEffectiveTitleBarPalette_UsesDefaults_WhenNoThemeAndNoInline()
    {
        var settings = new UiSettings();
        var resolver = new UiThemeResolver(settings, new FakePaletteRepo(), new FakeUiThemeRepo());

        TitleBarPalette result = resolver.GetEffectiveTitleBarPalette();

        Assert.Equal(new TitleBarPalette().AppName, result.AppName);
    }
}
