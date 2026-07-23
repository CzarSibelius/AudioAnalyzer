using System.Diagnostics;
using AudioAnalyzer.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace AudioAnalyzer.Platform.macOS.NowPlaying;

/// <summary>
/// Spawns and supervises the <c>mediaremote-adapter</c> child process
/// (<c>/usr/bin/perl &lt;script&gt; &lt;framework&gt; stream --no-diff --no-artwork</c>), streaming its
/// stdout lines to a parsed-payload callback. Stderr is non-fatal (logged at debug); a process exit
/// is reported once and the process is not re-spawned (degrade to no data). <see cref="Dispose"/>
/// sends <c>SIGTERM</c> and joins so the adapter stops its run loop cleanly.
/// </summary>
public sealed class MediaRemoteAdapterProcess : IDisposable
{
    private static readonly Action<ILogger, string, Exception?> s_adapterStderr =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(7660, "MediaRemoteAdapterStderr"),
            "mediaremote-adapter stderr: {Line}");

    private static readonly Action<ILogger, int, Exception?> s_adapterExited =
        LoggerMessage.Define<int>(
            LogLevel.Warning,
            new EventId(7661, "MediaRemoteAdapterExited"),
            "mediaremote-adapter process exited with code {ExitCode}; now-playing will report no data");

    private static readonly Action<ILogger, Exception?> s_adapterStartFailed =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(7662, "MediaRemoteAdapterStartFailed"),
            "Failed to start the mediaremote-adapter process; now-playing will report no data");

    private readonly string _scriptPath;
    private readonly string _frameworkPath;
    private readonly ILogger _logger;
    private readonly Action<NowPlayingInfo?> _onPayload;
    private Process? _process;
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="MediaRemoteAdapterProcess"/> class.</summary>
    /// <param name="scriptPath">Absolute path to <c>mediaremote-adapter.pl</c>.</param>
    /// <param name="frameworkPath">Absolute path to <c>MediaRemoteAdapter.framework</c>.</param>
    /// <param name="logger">Logger for stderr and exit diagnostics.</param>
    /// <param name="onPayload">Callback invoked with the latest parsed now-playing info (or null).</param>
    public MediaRemoteAdapterProcess(
        string scriptPath,
        string frameworkPath,
        ILogger logger,
        Action<NowPlayingInfo?> onPayload)
    {
        _scriptPath = scriptPath ?? throw new ArgumentNullException(nameof(scriptPath));
        _frameworkPath = frameworkPath ?? throw new ArgumentNullException(nameof(frameworkPath));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _onPayload = onPayload ?? throw new ArgumentNullException(nameof(onPayload));
    }

    /// <summary>Starts the adapter process and begins reading stdout/stderr asynchronously.</summary>
    /// <returns>True when the process started; false when launch failed (caller degrades to no data).</returns>
    public bool Start()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = MacOsMediaRemoteAdapterPaths.PerlPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add(_scriptPath);
        startInfo.ArgumentList.Add(_frameworkPath);
        startInfo.ArgumentList.Add("stream");
        startInfo.ArgumentList.Add("--no-diff");
        startInfo.ArgumentList.Add("--no-artwork");

        var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true,
        };
        process.OutputDataReceived += OnOutputDataReceived;
        process.ErrorDataReceived += OnErrorDataReceived;
        process.Exited += OnExited;

        try
        {
            if (!process.Start())
            {
                s_adapterStartFailed(_logger, null);
                process.Dispose();
                return false;
            }
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            s_adapterStartFailed(_logger, ex);
            process.Dispose();
            return false;
        }

        _process = process;
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return true;
    }

    private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data is null)
        {
            return;
        }

        if (MediaRemoteAdapterPayloadParser.TryParse(e.Data, out NowPlayingInfo? info))
        {
            _onPayload(info);
        }
    }

    private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            s_adapterStderr(_logger, e.Data, null);
        }
    }

    private void OnExited(object? sender, EventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        int exitCode = sender is Process p ? p.ExitCode : -1;
        s_adapterExited(_logger, exitCode, null);
        _onPayload(null);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposed = true;

        Process? process = _process;
        _process = null;
        if (process is null)
        {
            return;
        }

        process.OutputDataReceived -= OnOutputDataReceived;
        process.ErrorDataReceived -= OnErrorDataReceived;
        process.Exited -= OnExited;

        try
        {
            if (!process.HasExited)
            {
                _ = MacOsNowPlayingNativeMethods.Kill(process.Id, MacOsNowPlayingNativeMethods.Sigterm);
                if (!process.WaitForExit(2000))
                {
                    process.Kill(entireProcessTree: true);
                }
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            /* Process already exited or could not be signalled: nothing to tear down. */
        }
        finally
        {
            process.Dispose();
        }
    }
}
