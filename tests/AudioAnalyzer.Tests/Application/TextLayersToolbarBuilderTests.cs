using System.Linq;
using System.Text.RegularExpressions;
using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Domain;
using Xunit;

namespace AudioAnalyzer.Tests.Application;

/// <summary>Tests for <see cref="TextLayersToolbarBuilder"/> preset-editor layer digits.</summary>
public sealed class TextLayersToolbarBuilderTests
{
    private sealed class FakePaletteRepo : IPaletteRepository
    {
        private readonly Dictionary<string, PaletteDefinition?> _byId = new(StringComparer.OrdinalIgnoreCase);

        public void Set(string id, PaletteDefinition? def) => _byId[id] = def;

        public IReadOnlyList<PaletteInfo> GetAll() => [];

        public PaletteDefinition? GetById(string id) =>
            _byId.TryGetValue(id, out var d) ? d : null;
    }

    private sealed class FakeUiThemeRepository : IUiThemeRepository
    {
        public IReadOnlyList<ThemeInfo> GetAll() => [];

        public UiThemeDefinition? GetById(string id) => null;

        public void Save(string id, UiThemeDefinition definition)
        {
        }

        public string Create(UiThemeDefinition definition) => "theme-test";
    }

    private static string StripAnsi(string s) =>
        Regex.Replace(s, @"\x1b\[[\d;]*m", string.Empty);

    private static TextLayersToolbarBuilder CreateBuilder(out UiPalette effectivePalette)
    {
        var inline = new UiPalette
        {
            Normal = PaletteColor.FromRgb(10, 10, 10),
            Highlighted = PaletteColor.FromRgb(20, 20, 20),
            Dimmed = PaletteColor.FromRgb(30, 30, 30),
            Label = PaletteColor.FromRgb(40, 40, 40)
        };
        var uiSettings = new UiSettings { Palette = inline };
        var resolver = new UiThemeResolver(uiSettings, new FakePaletteRepo(), new FakeUiThemeRepository());
        effectivePalette = resolver.GetEffectiveUiPalette();
        return new TextLayersToolbarBuilder(resolver);
    }

    private static TextLayersToolbarContext CreatePresetContext(
        IReadOnlyList<TextLayerSettings> sortedLayers,
        int paletteCycleLayerIndex,
        IPaletteRepository paletteRepo)
    {
        var settings = new TextLayersVisualizerSettings { PaletteId = "default", Layers = sortedLayers.ToList() };
        return new TextLayersToolbarContext
        {
            Snapshot = new AnalysisSnapshot { TerminalWidth = 120, BeatCount = 0 },
            SortedLayers = sortedLayers,
            Settings = settings,
            PaletteCycleLayerIndex = paletteCycleLayerIndex,
            PaletteRepo = paletteRepo,
            UiSettings = new UiSettings(),
            ActiveLayerContextualRows = [],
            ApplicationMode = ApplicationMode.PresetEditor
        };
    }

    [Fact]
    public void BuildSuffix_TwoLayers_StripsToLayers12Only()
    {
        var repo = new FakePaletteRepo();
        repo.Set("default", new PaletteDefinition
        {
            Name = "Default",
            Colors = [new PaletteColorEntry { R = 1, G = 2, B = 3 }]
        });
        var builder = CreateBuilder(out _);
        var layers = new List<TextLayerSettings>
        {
            new() { ZOrder = 0, Enabled = true },
            new() { ZOrder = 1, Enabled = true }
        };
        var ctx = CreatePresetContext(layers, paletteCycleLayerIndex: 0, repo);
        string? suffix = builder.BuildSuffix(ctx);
        Assert.NotNull(suffix);
        string plain = StripAnsi(suffix);
        Match m = Regex.Match(plain, @"Layers:(\d+)");
        Assert.True(m.Success);
        Assert.Equal("12", m.Groups[1].Value);
    }

    [Fact]
    public void BuildSuffix_SelectedLayerUsesHighlightedColor_First()
    {
        var repo = new FakePaletteRepo();
        repo.Set("default", new PaletteDefinition { Name = "D", Colors = [new PaletteColorEntry { R = 5, G = 5, B = 5 }] });
        var builder = CreateBuilder(out UiPalette p);
        var layers = new List<TextLayerSettings>
        {
            new() { ZOrder = 0, Enabled = true },
            new() { ZOrder = 1, Enabled = true }
        };
        var ctx = CreatePresetContext(layers, paletteCycleLayerIndex: 0, repo);
        string? suffix = builder.BuildSuffix(ctx);
        Assert.NotNull(suffix);
        string hi = AnsiConsole.ColorCode(p.Highlighted);
        string marker = hi + "1" + AnsiConsole.ResetCode;
        Assert.Contains(marker, suffix);
    }

    [Fact]
    public void BuildSuffix_SelectedLayerUsesHighlightedColor_Second()
    {
        var repo = new FakePaletteRepo();
        repo.Set("default", new PaletteDefinition { Name = "D", Colors = [new PaletteColorEntry { R = 5, G = 5, B = 5 }] });
        var builder = CreateBuilder(out UiPalette p);
        var layers = new List<TextLayerSettings>
        {
            new() { ZOrder = 0, Enabled = true },
            new() { ZOrder = 1, Enabled = true }
        };
        var ctx = CreatePresetContext(layers, paletteCycleLayerIndex: 1, repo);
        string? suffix = builder.BuildSuffix(ctx);
        Assert.NotNull(suffix);
        string hi = AnsiConsole.ColorCode(p.Highlighted);
        string marker = hi + "2" + AnsiConsole.ResetCode;
        Assert.Contains(marker, suffix);
    }

    [Fact]
    public void BuildSuffix_DisabledNonSelectedLayerUsesDimmedColor()
    {
        var repo = new FakePaletteRepo();
        repo.Set("default", new PaletteDefinition { Name = "D", Colors = [new PaletteColorEntry { R = 5, G = 5, B = 5 }] });
        var builder = CreateBuilder(out UiPalette p);
        var layers = new List<TextLayerSettings>
        {
            new() { ZOrder = 0, Enabled = false },
            new() { ZOrder = 1, Enabled = true }
        };
        var ctx = CreatePresetContext(layers, paletteCycleLayerIndex: 1, repo);
        string? suffix = builder.BuildSuffix(ctx);
        Assert.NotNull(suffix);
        string dim = AnsiConsole.ColorCode(p.Dimmed);
        string marker = dim + "1" + AnsiConsole.ResetCode;
        Assert.Contains(marker, suffix);
    }

    [Fact]
    public void BuildViewports_LayersCellMatchesSuffixLayerDigits()
    {
        var repo = new FakePaletteRepo();
        repo.Set("default", new PaletteDefinition { Name = "Default", Colors = [new PaletteColorEntry { R = 1, G = 1, B = 1 }] });
        var builder = CreateBuilder(out _);
        var layers = new List<TextLayerSettings>
        {
            new() { ZOrder = 0, Enabled = true },
            new() { ZOrder = 1, Enabled = true }
        };
        var ctx = CreatePresetContext(layers, paletteCycleLayerIndex: 0, repo);
        var viewports = builder.BuildViewports(ctx);
        var layersDesc = viewports.First(d => d.Label == "Layers");
        var ansi = ((AnsiText)layersDesc.GetValue()).Value;
        string plain = StripAnsi(ansi);
        Match m = Regex.Match(plain, @"Layers:(\d+)");
        Assert.True(m.Success);
        Assert.Equal("12", m.Groups[1].Value);
    }
}
