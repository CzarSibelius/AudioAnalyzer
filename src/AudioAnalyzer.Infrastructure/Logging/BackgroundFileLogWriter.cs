using System.IO.Abstractions;
using System.Text;
using System.Threading.Channels;

namespace AudioAnalyzer.Infrastructure.Logging;

/// <summary>Queues log lines and writes them on a background thread (ADR-0076 performance).</summary>
internal sealed class BackgroundFileLogWriter : IDisposable
{
    private readonly Channel<string> _channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false,
        AllowSynchronousContinuations = false
    });

    private readonly StreamWriter _streamWriter;
    private readonly Task _processTask;

    /// <summary>Creates the writer, ensuring the log directory exists.</summary>
    public BackgroundFileLogWriter(IFileSystem fileSystem, string absolutePath)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        ArgumentException.ThrowIfNullOrEmpty(absolutePath);

        string? dir = Path.GetDirectoryName(absolutePath);
        if (!string.IsNullOrEmpty(dir))
        {
            fileSystem.Directory.CreateDirectory(dir);
        }

        Stream stream = fileSystem.File.Open(absolutePath, FileMode.Append, FileAccess.Write, FileShare.Read);
        _streamWriter = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = false };
        _processTask = Task.Run(ProcessLoopAsync);
    }

    /// <summary>Enqueues a line for writing. Does not block the caller on disk I/O.</summary>
    public bool TryEnqueue(string line)
    {
        return _channel.Writer.TryWrite(line);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _channel.Writer.TryComplete();
        try
        {
            _processTask.GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            /* Drain failed: avoid throwing from Dispose */
            System.Diagnostics.Debug.WriteLine($"Log writer drain failed: {ex.Message}");
        }

        _streamWriter.Dispose();
    }

    private async Task ProcessLoopAsync()
    {
        try
        {
            await foreach (string item in _channel.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                await _streamWriter.WriteAsync(item).ConfigureAwait(false);
                await _streamWriter.FlushAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Log writer loop failed: {ex.Message}");
        }
    }
}
