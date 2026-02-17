using System.Runtime.Versioning;
using AudioAnalyzer.Application.Abstractions;
using Windows.Media.Control;

namespace AudioAnalyzer.Platform.Windows.NowPlaying;

/// <summary>Now-playing provider using Windows GSMTC (Global System Media Transport Controls).</summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsNowPlayingProvider : INowPlayingProvider, IDisposable
{
    private readonly object _lock = new();
    private string? _cachedText;
    private CancellationTokenSource? _cts;
    private Task? _pollTask;
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _currentSession;

    /// <summary>Starts the provider and begins listening for media sessions.</summary>
    public void Start()
    {
        _cts = new CancellationTokenSource();
        _pollTask = Task.Run(() => RunAsync(_cts.Token));
    }

    /// <inheritdoc />
    public string? GetNowPlayingText()
    {
        lock (_lock)
        {
            return _cachedText;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _cts?.Cancel();
        try
        {
            _pollTask?.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
            /* Task cancelled: expected on shutdown */
        }
        _cts?.Dispose();
        UnsubscribeSession();
        _manager = null;
        _currentSession = null;
    }

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync().AsTask(ct);
            _manager.CurrentSessionChanged += OnCurrentSessionChanged;

            await RefreshSessionAndPropertiesAsync(ct);
            await PollLoop(ct);
        }
        catch (OperationCanceledException)
        {
            /* Expected on shutdown */
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"NowPlaying: GSMTC failed: {ex.Message}");
        }
    }

    private void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
    {
        UnsubscribeSession();
        _currentSession = sender.GetCurrentSession();
        SubscribeSession();
        _ = RefreshPropertiesAsync();
    }

    private void SubscribeSession()
    {
        if (_currentSession != null)
        {
            _currentSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
        }
    }

    private void UnsubscribeSession()
    {
        if (_currentSession != null)
        {
            try
            {
                _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NowPlaying: Unsubscribe failed: {ex.Message}");
            }
            _currentSession = null;
        }
    }

    private void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession session, MediaPropertiesChangedEventArgs args)
    {
        _ = RefreshPropertiesAsync();
    }

    private async Task RefreshSessionAndPropertiesAsync(CancellationToken ct)
    {
        try
        {
            _currentSession = _manager?.GetCurrentSession();
            SubscribeSession();
            await RefreshPropertiesAsync(ct);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"NowPlaying: Refresh failed: {ex.Message}");
        }
    }

    private Task RefreshPropertiesAsync(CancellationToken ct = default)
    {
        return RefreshPropertiesAsyncCore(ct);
    }

    private async Task RefreshPropertiesAsyncCore(CancellationToken ct)
    {
        var session = _currentSession;
        if (session == null)
        {
            UpdateCache(null);
            return;
        }

        try
        {
            var props = await session.TryGetMediaPropertiesAsync().AsTask(ct);
            string? text = FormatProperties(props);
            UpdateCache(text);
        }
        catch (OperationCanceledException)
        {
            /* Ignore */
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"NowPlaying: GetMediaProperties failed: {ex.Message}");
            UpdateCache(null);
        }
    }

    private static string? FormatProperties(GlobalSystemMediaTransportControlsSessionMediaProperties? props)
    {
        if (props == null)
        {
            return null;
        }

        string? title = props.Title?.Trim();
        string? artist = props.Artist?.Trim();

        if (string.IsNullOrEmpty(title))
        {
            return string.IsNullOrEmpty(artist) ? null : artist;
        }

        if (string.IsNullOrEmpty(artist))
        {
            return title;
        }

        return $"{artist} - {title}";
    }

    private void UpdateCache(string? text)
    {
        lock (_lock)
        {
            _cachedText = text;
        }
    }

    private async Task PollLoop(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1.5));
        while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
        {
            await RefreshSessionAndPropertiesAsync(ct);
        }
    }
}
