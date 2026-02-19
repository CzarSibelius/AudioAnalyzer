using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Application;

/// <summary>Advances scroll state and computes the visible slice of text. Uses grapheme clusters so emoji are never split.</summary>
public sealed class ScrollingTextEngine : IScrollingTextEngine
{
    /// <inheritdoc />
    public string GetVisibleSlice<T>(T text, int width, ref ScrollingTextViewportState state, double speedPerFrame)
        where T : IDisplayText
    {
        if (width <= 0)
        {
            return "";
        }

        if (string.IsNullOrEmpty(text.Value))
        {
            return new string(' ', width);
        }

        int visibleLength = text.GetVisibleLength();
        if (visibleLength <= width)
        {
            state.Reset();
            return text.PadToWidth(width);
        }

        int maxOffset = visibleLength - width;

        state.Offset += speedPerFrame * state.Direction;

        if (state.Offset >= maxOffset)
        {
            state.Offset = maxOffset;
            state.Direction = -1;
        }
        else if (state.Offset <= 0)
        {
            state.Offset = 0;
            state.Direction = 1;
        }

        int start = (int)Math.Clamp(Math.Floor(state.Offset), 0, maxOffset);
        return text.GetVisibleSubstring(start, width);
    }
}
