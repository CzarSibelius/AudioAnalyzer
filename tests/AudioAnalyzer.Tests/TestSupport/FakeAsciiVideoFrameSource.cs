using System.Threading;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Tests.TestSupport;

/// <summary>Deterministic BGRA frames for tests; no camera hardware.</summary>
public sealed class FakeAsciiVideoFrameSource : IAsciiVideoFrameSource
{
    private const int FrameWidth = 16;
    private const int FrameHeight = 16;
    private long _sequence;
    private readonly byte[] _templatePixels;

    /// <summary>Initializes a new instance with a solid orange-brown template frame.</summary>
    public FakeAsciiVideoFrameSource()
    {
        _templatePixels = new byte[FrameWidth * FrameHeight * 4];
        for (int i = 0; i < _templatePixels.Length; i += 4)
        {
            _templatePixels[i] = 200;
            _templatePixels[i + 1] = 80;
            _templatePixels[i + 2] = 40;
            _templatePixels[i + 3] = 255;
        }
    }

    /// <inheritdoc />
    public void PrepareForFrame(AsciiVideoCaptureRequest? request)
    {
        if (request?.SourceKind == AsciiVideoSourceKind.Webcam)
        {
            Interlocked.Increment(ref _sequence);
            return;
        }

        Interlocked.Exchange(ref _sequence, 0);
    }

    /// <inheritdoc />
    public bool TryGetLatestFrame(out AsciiVideoFrameSnapshot? snapshot)
    {
        long seq = Interlocked.Read(ref _sequence);
        if (seq == 0)
        {
            snapshot = null;
            return false;
        }

        var copy = GC.AllocateUninitializedArray<byte>(_templatePixels.Length);
        Buffer.BlockCopy(_templatePixels, 0, copy, 0, copy.Length);
        snapshot = new AsciiVideoFrameSnapshot(FrameWidth, FrameHeight, seq, copy);
        return true;
    }

    /// <inheritdoc />
    public bool IsWebcamStarting => false;

    /// <inheritdoc />
    public bool IsWebcamSessionActive => Interlocked.Read(ref _sequence) > 0;

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
