namespace AudioAnalyzer.Console;

/// <summary>
/// Delegates header line count to the active <see cref="IApplicationMode"/>.
/// </summary>
internal sealed class ApplicationModeHeaderProvider : IApplicationModeHeaderProvider
{
    private readonly IApplicationModeFactory _modeFactory;

    public ApplicationModeHeaderProvider(IApplicationModeFactory modeFactory)
    {
        _modeFactory = modeFactory ?? throw new ArgumentNullException(nameof(modeFactory));
    }

    /// <inheritdoc />
    public int HeaderLineCount => _modeFactory.GetActiveApplicationMode().HeaderLineCount;
}
