using AudioAnalyzer.Application.Abstractions;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace AudioAnalyzer.Platform.Windows.Audio;

/// <summary>WASAPI-backed <see cref="IAudioInput"/> wrapper for Windows capture and loopback.</summary>
public sealed class WindowsWaveInAudioInput : IAudioInput
{
    private IWaveIn? _capture;
    private bool _disposed;
    private readonly object _lock = new();

    /// <summary>Creates an input that forwards NAudio buffer events to <see cref="IAudioInput"/> subscribers.</summary>
    public WindowsWaveInAudioInput(IWaveIn capture)
    {
        _capture = capture;
        _capture.DataAvailable += OnDataAvailable;
    }

    /// <inheritdoc />
    public event EventHandler<AudioDataAvailableEventArgs>? DataAvailable;

    /// <inheritdoc />
    public void Start()
    {
        lock (_lock)
        {
            _capture?.StartRecording();
        }
    }

    /// <inheritdoc />
    public void StopCapture()
    {
        lock (_lock)
        {
            _capture?.StopRecording();
        }
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (_capture == null || e.BytesRecorded == 0)
        {
            return;
        }

        var format = _capture switch
        {
            WasapiLoopbackCapture loopback => loopback.WaveFormat,
            WasapiCapture wasapi => wasapi.WaveFormat,
            _ => null
        };
        if (format == null)
        {
            return;
        }

        var audioFormat = new AudioFormat
        {
            SampleRate = format.SampleRate,
            BitsPerSample = format.BitsPerSample,
            Channels = format.Channels
        };
        DataAvailable?.Invoke(this, new AudioDataAvailableEventArgs
        {
            Buffer = e.Buffer ?? Array.Empty<byte>(),
            BytesRecorded = e.BytesRecorded,
            Format = audioFormat
        });
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _capture?.StopRecording();
            _capture?.Dispose();
            _capture = null;
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
