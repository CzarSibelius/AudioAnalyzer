namespace AudioAnalyzer.Application.Abstractions;

/// <summary>Creates stateful scrolling viewport instances. One viewport per scroll region (e.g. device, now-playing).</summary>
public interface IScrollingTextViewportFactory
{
    /// <summary>Creates a new viewport with its own scroll state.</summary>
    IScrollingTextViewport CreateViewport();
}
