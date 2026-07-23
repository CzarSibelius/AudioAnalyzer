using AudioAnalyzer.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace AudioAnalyzer.Platform.macOS.NowPlaying;

/// <summary>
/// macOS now-playing provider backed by the <c>mediaremote-adapter</c> mechanism. A background child
/// process (Perl + helper framework) streams now-playing JSON; parsed payloads feed a lock-protected
/// cache that <see cref="GetNowPlaying"/> reads synchronously (no work on the render/header thread,
/// per ADR-0030). Mirrors the Windows GSMTC provider's background-cache model. See ADR-0094.
/// </summary>
public sealed class MacOsNowPlayingProvider : INowPlayingProvider, IDisposable
{
    private readonly string _scriptPath;
    private readonly string _frameworkPath;
    private readonly ILogger<MacOsNowPlayingProvider> _logger;
    private readonly object _lock = new();
    private NowPlayingInfo? _cachedInfo;
    private MediaRemoteAdapterProcess? _adapterProcess;

    /// <summary>Initializes a new instance of the <see cref="MacOsNowPlayingProvider"/> class.</summary>
    /// <param name="scriptPath">Absolute path to <c>mediaremote-adapter.pl</c>.</param>
    /// <param name="frameworkPath">Absolute path to <c>MediaRemoteAdapter.framework</c>.</param>
    /// <param name="logger">Logger for adapter process diagnostics.</param>
    public MacOsNowPlayingProvider(
        string scriptPath,
        string frameworkPath,
        ILogger<MacOsNowPlayingProvider> logger)
    {
        _scriptPath = scriptPath ?? throw new ArgumentNullException(nameof(scriptPath));
        _frameworkPath = frameworkPath ?? throw new ArgumentNullException(nameof(frameworkPath));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Starts the background adapter process; failures degrade to no data without throwing.</summary>
    public void Start()
    {
        var adapterProcess = new MediaRemoteAdapterProcess(_scriptPath, _frameworkPath, _logger, UpdateCache);
        _adapterProcess = adapterProcess;
        if (!adapterProcess.Start())
        {
            adapterProcess.Dispose();
            _adapterProcess = null;
        }
    }

    /// <inheritdoc />
    public NowPlayingInfo? GetNowPlaying()
    {
        lock (_lock)
        {
            return _cachedInfo;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _adapterProcess?.Dispose();
        _adapterProcess = null;
    }

    private void UpdateCache(NowPlayingInfo? info)
    {
        lock (_lock)
        {
            _cachedInfo = info;
        }
    }
}
