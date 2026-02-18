using AudioAnalyzer.Application.Abstractions;
using Xunit;

namespace AudioAnalyzer.Tests;

/// <summary>Tests for ScrollingTextViewport grapheme-cluster handling (emoji, surrogate pairs).</summary>
public sealed class ScrollingTextViewportTests
{
    [Fact]
    public void Emoji_NotSplit_WhenScrolling()
    {
        var text = new PlainText("Hello 😀 World");
        var state = new ScrollingTextViewportState();

        for (int step = 0; step < 20; step++)
        {
            string result = ScrollingTextViewport.Render(text, 8, ref state, 1.0);

            Assert.DoesNotContain('\ufffd', result);

            for (int i = 0; i < result.Length - 1; i++)
            {
                if (char.IsHighSurrogate(result[i]))
                {
                    Assert.True(char.IsLowSurrogate(result[i + 1]), "High surrogate must be followed by low surrogate");
                }
            }
        }
    }

    [Fact]
    public void Emoji_GetVisibleSubstring_ReturnsWholeGrapheme()
    {
        var text = new PlainText("A😀B");
        Assert.Equal(3, text.GetVisibleLength());

        string sub = text.GetVisibleSubstring(1, 1);
        Assert.Equal("😀", sub);
    }

    [Fact]
    public void PlainText_GetVisibleLength_CountsGraphemes()
    {
        Assert.Equal(5, new PlainText("Hello").GetVisibleLength());
        Assert.Equal(3, new PlainText("A😀B").GetVisibleLength());
    }

    [Fact]
    public void FormatLabel_WithHotkey_ReturnsLabelWithHotkeyInParens()
    {
        Assert.Equal("Preset (V): ", ScrollingTextViewport.FormatLabel("Preset", "V"));
        Assert.Equal("Device (D): ", ScrollingTextViewport.FormatLabel("Device", "D"));
        Assert.Equal("Mode (Tab): ", ScrollingTextViewport.FormatLabel("Mode", "Tab"));
    }

    [Fact]
    public void FormatLabel_WithoutHotkey_ReturnsLabelWithColonOnly()
    {
        Assert.Equal("Now: ", ScrollingTextViewport.FormatLabel("Now", null));
        Assert.Equal("Device: ", ScrollingTextViewport.FormatLabel("Device", ""));
    }
}
