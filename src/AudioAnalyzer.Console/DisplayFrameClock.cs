using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Console;

/// <summary>Publishes frame delta for header scrolling when no snapshot is on <see cref="RenderContext"/>.</summary>
internal sealed class DisplayFrameClock : IDisplayFrameClock
{
    private double _seconds = 1.0 / 60.0;

    /// <inheritdoc />
    public double FrameDeltaSeconds => _seconds;

    /// <inheritdoc />
    public void SetFrameDeltaSeconds(double seconds) =>
        _seconds = seconds > 0 ? seconds : 1.0 / 60.0;
}
