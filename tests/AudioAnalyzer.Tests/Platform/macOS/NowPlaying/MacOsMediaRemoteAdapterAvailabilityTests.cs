using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Platform.macOS.NowPlaying;
using Xunit;

namespace AudioAnalyzer.Tests.Platform.macOS.NowPlaying;

public sealed class MacOsMediaRemoteAdapterAvailabilityTests
{
    private const string ContentRoot = "/Apps/AudioAnalyzer.app/Contents/Resources";

    private static string ScriptPath =>
        MacOsMediaRemoteAdapterPaths.GetScriptPath(ContentRoot);

    private static string FrameworkPath =>
        MacOsMediaRemoteAdapterPaths.GetFrameworkPath(ContentRoot);

    [Fact]
    public void Paths_ResolveUnderAdapterDirectory()
    {
        Assert.Equal(
            Path.Combine(ContentRoot, "mediaremote-adapter", "mediaremote-adapter.pl"),
            ScriptPath);
        Assert.Equal(
            Path.Combine(ContentRoot, "mediaremote-adapter", "MediaRemoteAdapter.framework"),
            FrameworkPath);
    }

    [Fact]
    public void TryResolvePaths_AllPresent_ReturnsResolvedPaths()
    {
        var availability = new MacOsMediaRemoteAdapterAvailability(
            new FakeContentLocator(ContentRoot),
            fileExists: p => p == MacOsMediaRemoteAdapterPaths.PerlPath || p == ScriptPath,
            directoryExists: p => p == FrameworkPath);

        Assert.True(availability.IsAvailable);
        Assert.True(availability.TryResolvePaths(out string script, out string framework));
        Assert.Equal(ScriptPath, script);
        Assert.Equal(FrameworkPath, framework);
    }

    [Fact]
    public void TryResolvePaths_NoContentRoot_ReturnsFalse()
    {
        var availability = new MacOsMediaRemoteAdapterAvailability(
            new FakeContentLocator(contentRoot: null),
            fileExists: _ => true,
            directoryExists: _ => true);

        Assert.False(availability.IsAvailable);
        Assert.False(availability.TryResolvePaths(out _, out _));
    }

    [Fact]
    public void TryResolvePaths_MissingPerl_ReturnsFalse()
    {
        var availability = new MacOsMediaRemoteAdapterAvailability(
            new FakeContentLocator(ContentRoot),
            fileExists: p => p == ScriptPath,
            directoryExists: p => p == FrameworkPath);

        Assert.False(availability.TryResolvePaths(out _, out _));
    }

    [Fact]
    public void TryResolvePaths_MissingFramework_ReturnsFalse()
    {
        var availability = new MacOsMediaRemoteAdapterAvailability(
            new FakeContentLocator(ContentRoot),
            fileExists: p => p == MacOsMediaRemoteAdapterPaths.PerlPath || p == ScriptPath,
            directoryExists: _ => false);

        Assert.False(availability.TryResolvePaths(out _, out _));
    }

    [Fact]
    public void TryResolvePaths_MissingScript_ReturnsFalse()
    {
        var availability = new MacOsMediaRemoteAdapterAvailability(
            new FakeContentLocator(ContentRoot),
            fileExists: p => p == MacOsMediaRemoteAdapterPaths.PerlPath,
            directoryExists: p => p == FrameworkPath);

        Assert.False(availability.TryResolvePaths(out _, out _));
    }

    private sealed class FakeContentLocator : IHostContentLocator
    {
        private readonly string? _contentRoot;

        public FakeContentLocator(string? contentRoot) => _contentRoot = contentRoot;

        public bool TryGetContentRoots(out string contentRoot, out string writableRoot)
        {
            contentRoot = _contentRoot ?? string.Empty;
            writableRoot = _contentRoot ?? string.Empty;
            return _contentRoot is not null;
        }
    }
}
