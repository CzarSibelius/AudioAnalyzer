namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// State for a scrolling text viewport. Holds the current scroll offset and direction for ping-pong scrolling.
/// </summary>
public struct ScrollingTextViewportState
{
    /// <summary>Current scroll offset (characters from the start of the text).</summary>
    public double Offset { get; set; }

    /// <summary>Scroll direction: 1 = right (increasing offset), -1 = left (decreasing offset).</summary>
    public int Direction { get; set; }

    /// <summary>Resets state to start from the beginning, scrolling right.</summary>
    public void Reset()
    {
        Offset = 0;
        Direction = 1;
    }
}
