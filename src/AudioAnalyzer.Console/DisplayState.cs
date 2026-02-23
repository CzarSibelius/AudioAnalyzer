namespace AudioAnalyzer.Console;

/// <summary>Default implementation of display state. Holds full-screen and notifies on change.</summary>
internal sealed class DisplayState : IDisplayState
{
    private bool _fullScreen;

    /// <inheritdoc />
    public bool FullScreen
    {
        get => _fullScreen;
        set
        {
            if (_fullScreen == value)
            {
                return;
            }
            _fullScreen = value;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <inheritdoc />
    public event EventHandler? Changed;
}
