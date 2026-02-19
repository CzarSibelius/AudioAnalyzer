using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Application;

/// <summary>Creates new <see cref="IScrollingTextViewport"/> instances, each with its own scroll state.</summary>
public sealed class ScrollingTextViewportFactory : IScrollingTextViewportFactory
{
    private readonly IScrollingTextEngine _engine;

    /// <summary>Creates the factory with the given scroll engine.</summary>
    public ScrollingTextViewportFactory(IScrollingTextEngine engine)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
    }

    /// <inheritdoc />
    public IScrollingTextViewport CreateViewport()
    {
        return new ScrollingTextViewport(_engine);
    }
}
