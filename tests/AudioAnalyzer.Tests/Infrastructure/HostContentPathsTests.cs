using System.IO.Abstractions;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Infrastructure;
using Xunit;

namespace AudioAnalyzer.Tests.Infrastructure;

public sealed class HostContentPathsTests
{
    private sealed class FlatHostContentLocator : IHostContentLocator
    {
        public bool TryGetContentRoots(out string contentRoot, out string writableRoot)
        {
            contentRoot = string.Empty;
            writableRoot = string.Empty;
            return false;
        }
    }

    [Fact]
    public void Resolve_FlatLayout_UsesBaseDirectoryForSettingsPath()
    {
        var fs = new FileSystem();
        HostContentPaths paths = HostContentPaths.Resolve(fs, new FlatHostContentLocator());

        Assert.Equal(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), paths.SettingsFilePath);
        Assert.Equal(Path.Combine(AppContext.BaseDirectory, "presets"), paths.PresetsDirectory);
        Assert.True(fs.Directory.Exists(paths.PresetsDirectory));
    }

    [Fact]
    public void Resolve_WhenLocatorProvidesRoots_UsesThem()
    {
        var fs = new FileSystem();
        string root = Path.Combine(Path.GetTempPath(), "AudioAnalyzerHostContentPathsTests", Guid.NewGuid().ToString("N"));
        var locator = new FixedHostContentLocator(root, root);
        try
        {
            HostContentPaths paths = HostContentPaths.Resolve(fs, locator);

            Assert.Equal(Path.Combine(root, "appsettings.json"), paths.SettingsFilePath);
            Assert.Equal(Path.Combine(root, "presets"), paths.PresetsDirectory);
            Assert.True(fs.Directory.Exists(paths.PresetsDirectory));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private sealed class FixedHostContentLocator : IHostContentLocator
    {
        private readonly string _contentRoot;
        private readonly string _writableRoot;

        public FixedHostContentLocator(string contentRoot, string writableRoot)
        {
            _contentRoot = contentRoot;
            _writableRoot = writableRoot;
        }

        public bool TryGetContentRoots(out string contentRoot, out string writableRoot)
        {
            contentRoot = _contentRoot;
            writableRoot = _writableRoot;
            return true;
        }
    }
}
