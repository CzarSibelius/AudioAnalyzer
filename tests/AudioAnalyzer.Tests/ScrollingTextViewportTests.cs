using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using Xunit;

namespace AudioAnalyzer.Tests;

/// <summary>Tests for ScrollingTextViewport grapheme-cluster handling (emoji, surrogate pairs).</summary>
public sealed class ScrollingTextViewportTests
{
    private static IScrollingTextViewport CreateViewport()
    {
        var engine = new ScrollingTextEngine();
        return new ScrollingTextViewport(engine);
    }

    [Fact]
    public void Emoji_NotSplit_WhenScrolling()
    {
        var viewport = CreateViewport();
        var text = new PlainText("Hello 😀 World");

        for (int step = 0; step < 20; step++)
        {
            string result = viewport.Render(text, 8, 1.0);

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
    public void EmojiFirst_AtScrollLeft_ResultLengthEqualsRequestedWidth()
    {
        var viewport = CreateViewport();
        var text = new PlainText("😀Hello");
        int width = 5;

        // Two Renders to reach scroll-left (offset 0)
        _ = viewport.Render(text, width, 1.0);
        string result = viewport.Render(text, width, 1.0);

        int visibleLen = new PlainText(result).GetVisibleLength();
        Assert.Equal(width, visibleLen);
    }

    [Fact]
    public void EmojiFirst_AtScrollLeft_OutputHasConsistentWidth()
    {
        var viewport = CreateViewport();
        int width = 5;
        var textWithEmojiFirst = new PlainText("😀1234");   // 6 graphemes: emoji + 5 digits
        var textWithoutEmojiFirst = new PlainText("01234"); // 6 graphemes (no emoji at start)

        // Drive to scroll-left (offset 0): first Render goes right, second goes back left
        string resultEmojiFirst = viewport.Render(textWithEmojiFirst, width, 1.0);
        resultEmojiFirst = viewport.Render(textWithEmojiFirst, width, 1.0);

        // Use new viewport for second text (content change resets state)
        var viewport2 = CreateViewport();
        string resultNoEmoji = viewport2.Render(textWithoutEmojiFirst, width, 1.0);
        resultNoEmoji = viewport2.Render(textWithoutEmojiFirst, width, 1.0);

        int lenEmoji = new PlainText(resultEmojiFirst).GetVisibleLength();
        int lenNoEmoji = new PlainText(resultNoEmoji).GetVisibleLength();

        Assert.Equal(width, lenEmoji);
        Assert.Equal(width, lenNoEmoji);
        Assert.Equal(lenNoEmoji, lenEmoji);
    }

    [Fact]
    public void EmojiFirst_RenderWithLabel_AtScrollLeft_ResultLengthEqualsRequestedWidth()
    {
        var viewport = CreateViewport();
        var deviceName = new PlainText("😀Very Long Microphone Name");
        int totalWidth = 30;
        var palette = new UiPalette();

        // Drive to scroll-left: first Render goes right, second goes back left
        _ = viewport.RenderWithLabel("Device", deviceName, totalWidth, 1.0, palette.Label, palette.Normal, "D");
        string result = viewport.RenderWithLabel("Device", deviceName, totalWidth, 1.0, palette.Label, palette.Normal, "D");

        int visibleLen = AnsiConsole.GetVisibleLength(result);
        Assert.Equal(totalWidth, visibleLen);
    }

    [Fact]
    public void EmojiFirst_RenderWithLabel_AtScrollLeft_OutputHasConsistentWidth()
    {
        int totalWidth = 30;
        var palette = new UiPalette();
        var deviceWithEmojiFirst = new PlainText("😀Microphone Device Name");
        var deviceWithoutEmojiFirst = new PlainText("Microphone Device Name");

        var viewport1 = CreateViewport();
        _ = viewport1.RenderWithLabel("Device", deviceWithEmojiFirst, totalWidth, 1.0, palette.Label, palette.Normal, "D");
        string resultEmoji = viewport1.RenderWithLabel("Device", deviceWithEmojiFirst, totalWidth, 1.0, palette.Label, palette.Normal, "D");

        var viewport2 = CreateViewport();
        _ = viewport2.RenderWithLabel("Device", deviceWithoutEmojiFirst, totalWidth, 1.0, palette.Label, palette.Normal, "D");
        string resultNoEmoji = viewport2.RenderWithLabel("Device", deviceWithoutEmojiFirst, totalWidth, 1.0, palette.Label, palette.Normal, "D");

        int lenEmoji = AnsiConsole.GetVisibleLength(resultEmoji);
        int lenNoEmoji = AnsiConsole.GetVisibleLength(resultNoEmoji);

        Assert.Equal(totalWidth, lenEmoji);
        Assert.Equal(totalWidth, lenNoEmoji);
        Assert.Equal(lenNoEmoji, lenEmoji);
    }

    [Fact]
    public void FormatLabel_WithHotkey_ReturnsLabelWithHotkeyInParens()
    {
        var viewport = CreateViewport();
        Assert.Equal("Preset (V): ", viewport.FormatLabel("Preset", "V"));
        Assert.Equal("Device (D): ", viewport.FormatLabel("Device", "D"));
        Assert.Equal("Mode (Tab): ", viewport.FormatLabel("Mode", "Tab"));
    }

    [Fact]
    public void FormatLabel_WithoutHotkey_ReturnsLabelWithColonOnly()
    {
        var viewport = CreateViewport();
        Assert.Equal("Now: ", viewport.FormatLabel("Now", null));
        Assert.Equal("Device: ", viewport.FormatLabel("Device", ""));
    }
}
