using System.IO.Abstractions.TestingHelpers;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Infrastructure;
using Xunit;

namespace AudioAnalyzer.Tests.Infrastructure;

/// <summary>Tests for <see cref="FileUiThemeRepository"/>.</summary>
public sealed class FileUiThemeRepositoryTests
{
    [Fact]
    public void Save_GetById_GetAll_RoundTrip()
    {
        var fs = new MockFileSystem();
        string dir = "C:/themes";
        var repo = new FileUiThemeRepository(fs, dir);
        var def = new UiThemeDefinition
        {
            Name = "Neon",
            FallbackPaletteId = "default",
            Ui = new UiThemeUiSection { Normal = new PaletteColorEntry { R = 1, G = 2, B = 3 } }
        };

        repo.Save("neon", def);

        UiThemeDefinition? loaded = repo.GetById("neon");
        Assert.NotNull(loaded);
        Assert.Equal("Neon", loaded!.Name);
        Assert.Equal("default", loaded.FallbackPaletteId);
        Assert.NotNull(loaded.Ui);
        IReadOnlyList<ThemeInfo> all = repo.GetAll();
        Assert.Single(all);
        Assert.Equal("neon", all[0].Id);
        Assert.Equal("Neon", all[0].Name);
        Assert.Equal("default", all[0].FallbackPaletteId);
    }

    [Fact]
    public void Create_GeneratesUniqueId()
    {
        var fs = new MockFileSystem();
        var repo = new FileUiThemeRepository(fs, "C:/t");
        var def = new UiThemeDefinition { Name = "A" };

        string id1 = repo.Create(def);
        string id2 = repo.Create(def);

        Assert.NotEqual(id1, id2);
        Assert.NotNull(repo.GetById(id1));
        Assert.NotNull(repo.GetById(id2));
    }
}
