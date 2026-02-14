using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Infrastructure;

/// <summary>
/// Synthetic audio input for demo mode. Produces a steady-BPM stream (sine waves + periodic kick)
/// so visualizers can be tested without real audio. The stream is not audible; it feeds the analysis pipeline.
/// </summary>
public sealed class SyntheticAudioInput : IAudioInput
{
    private const int SampleRate = 44100;
    private const int BitsPerSample = 16;
    private const int Channels = 2;
    private const int FramesPerChunk = 1024;
    private const int ChunkIntervalMs = 23;
    private const double TwoPi = 2 * Math.PI;

    private readonly int _bpm;
    private readonly byte[] _buffer;
    private readonly AudioFormat _format;
    private readonly object _lock = new();
    private CancellationTokenSource? _cts;
    private Task? _runTask;
    private bool _disposed;
    private double _elapsedMs;
    private double _phase80;
    private double _phase250;
    private double _phase1k;
    private double _phase4k;
    private double _lfoPhase;

    public event EventHandler<AudioDataAvailableEventArgs>? DataAvailable;

    /// <summary>Creates a synthetic audio input with the specified BPM (beats per minute).</summary>
    /// <param name="bpm">Beats per minute for the periodic kick; typically 60–180.</param>
    public SyntheticAudioInput(int bpm)
    {
        _bpm = Math.Clamp(bpm, 60, 180);
        _buffer = new byte[FramesPerChunk * Channels * (BitsPerSample / 8)];
        _format = new AudioFormat
        {
            SampleRate = SampleRate,
            BitsPerSample = BitsPerSample,
            Channels = Channels
        };
    }

    /// <inheritdoc />
    public void Start()
    {
        lock (_lock)
        {
            if (_disposed || _cts != null)
            {
                return;
            }

            _cts = new CancellationTokenSource();
            _elapsedMs = 0;
            _runTask = Task.Run(() => RunAsync(_cts.Token));
        }
    }

    /// <inheritdoc />
    public void StopCapture()
    {
        lock (_lock)
        {
            _cts?.Cancel();
        }
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

            _cts?.Cancel();
            try
            {
                _runTask?.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
            }

            _cts?.Dispose();
            _cts = null;
            _runTask = null;
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(ChunkIntervalMs));
        try
        {
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                lock (_lock)
                {
                    if (_disposed)
                    {
                        break;
                    }

                    GenerateChunk();
                }

                DataAvailable?.Invoke(this, new AudioDataAvailableEventArgs
                {
                    Buffer = _buffer,
                    BytesRecorded = _buffer.Length,
                    Format = _format
                });
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when stopped
        }
        finally
        {
            timer.Dispose();
        }
    }

    private void GenerateChunk()
    {
        double beatIntervalMs = 60000.0 / _bpm;
        double kickDurationMs = 30;
        double chunkMs = FramesPerChunk * 1000.0 / SampleRate;

        // LFO for amplitude modulation (~0.5 Hz)
        _lfoPhase += TwoPi * 0.5 * chunkMs / 1000;
        double lfo = 0.6 + 0.4 * Math.Sin(_lfoPhase);

        for (int i = 0; i < FramesPerChunk; i++)
        {
            double t = (_elapsedMs * 0.001) + (i * 1.0 / SampleRate);
            double posInCycleMs = (t * 1000) % beatIntervalMs;
            bool inKick = posInCycleMs < kickDurationMs;

            // Base spectrum: mix of sines at different frequencies
            _phase80 += TwoPi * 80 / SampleRate;
            _phase250 += TwoPi * 250 / SampleRate;
            _phase1k += TwoPi * 1000 / SampleRate;
            _phase4k += TwoPi * 4000 / SampleRate;

            float sample = (float)(
                (0.08 * Math.Sin(_phase80) + 0.06 * Math.Sin(_phase250) + 0.05 * Math.Sin(_phase1k) + 0.04 * Math.Sin(_phase4k)) * lfo);

            if (inKick)
            {
                double kickT = posInCycleMs / 1000;
                float kick = (float)(0.4 * Math.Exp(-kickT * 25) * Math.Sin(TwoPi * 60 * t));
                sample += kick;
            }

            sample = Math.Clamp(sample, -1f, 1f);
            short s16 = (short)(sample * 32767);
            int offset = i * 4;
            _buffer[offset] = (byte)(s16 & 0xFF);
            _buffer[offset + 1] = (byte)((s16 >> 8) & 0xFF);
            _buffer[offset + 2] = (byte)(s16 & 0xFF);
            _buffer[offset + 3] = (byte)((s16 >> 8) & 0xFF);
        }

        _elapsedMs += chunkMs;
    }
}
