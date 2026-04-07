using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Application.Palette;
using AudioAnalyzer.Domain;
using Xunit;

namespace AudioAnalyzer.Tests.Application.Display;

/// <summary>Tests for <see cref="UiThemeMerger"/>.</summary>
public sealed class UiThemeMergerTests
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
    public void ResolveBase_UsesMapper_WhenFallbackResolves()
    {
        var repo = new FakePaletteRepo();
        repo.Set("x", new PaletteDefinition
        {
            Colors =
            [
                new PaletteColorEntry { R = 10, G = 0, B = 0 },
                new PaletteColorEntry { R = 0, G = 20, B = 0 }
            ]
        });
        var settings = new UiSettings { Palette = new UiPalette { Normal = PaletteColor.FromRgb(1, 1, 1) } };

        (UiPalette ui, TitleBarPalette tb) = UiThemeMerger.ResolveBase("x", settings, repo);

        Assert.Equal(PaletteColor.FromRgb(10, 0, 0), ui.Normal);
        Assert.Equal(PaletteColor.FromRgb(0, 20, 0), ui.Highlighted);
        _ = tb;
    }

    [Fact]
    public void ResolveBase_UsesInline_WhenFallbackMissing()
    {
        var settings = new UiSettings
        {
            Palette = new UiPalette { Normal = PaletteColor.FromRgb(3, 3, 3) },
            TitleBarPalette = new TitleBarPalette { AppName = PaletteColor.FromRgb(4, 4, 4) }
        };

        (UiPalette ui, TitleBarPalette tb) = UiThemeMerger.ResolveBase("nope", settings, new FakePaletteRepo());

        Assert.Equal(PaletteColor.FromRgb(3, 3, 3), ui.Normal);
        Assert.Equal(PaletteColor.FromRgb(4, 4, 4), tb.AppName);
    }

    [Fact]
    public void MergeOverlay_AppliesExplicitSlots()
    {
        var baseUi = new UiPalette { Normal = PaletteColor.FromRgb(1, 1, 1), Highlighted = PaletteColor.FromRgb(2, 2, 2) };
        var baseTb = new TitleBarPalette();
        var theme = new UiThemeDefinition
        {
            Ui = new UiThemeUiSection { Normal = new PaletteColorEntry { R = 7, G = 8, B = 9 } },
            TitleBar = new UiThemeTitleBarSection { AppName = new PaletteColorEntry { R = 1, G = 2, B = 3 } }
        };

        (UiPalette ui, TitleBarPalette tb) = UiThemeMerger.MergeOverlay(theme, baseUi, baseTb);

        Assert.Equal(PaletteColor.FromRgb(7, 8, 9), ui.Normal);
        Assert.Equal(PaletteColor.FromRgb(2, 2, 2), ui.Highlighted);
        Assert.Equal(PaletteColor.FromRgb(1, 2, 3), tb.AppName);
    }
}
