using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Domain;
using Xunit;

namespace AudioAnalyzer.Tests.Application.Display;

/// <summary>Tests for ScrollingTextViewport grapheme-cluster and display-width handling (emoji, surrogate pairs).</summary>
public sealed class ScrollingTextViewportTests
{
    private const double D60 = 1.0 / 60.0;

    [Fact]
    public void DisplayWidth_Emoji_CountsAsTwoColumns()
    {
        Assert.Equal(2, DisplayWidth.GetGraphemeWidth("🔊", 0));
        Assert.Equal(2, DisplayWidth.GetGraphemeWidth("😀", 0));
        Assert.Equal(6, DisplayWidth.GetDisplayWidth("😀1234"));
        Assert.Equal(5, DisplayWidth.GetDisplayWidth("01234"));
    }

    [Fact]
    public void DisplayWidth_SnapToGraphemeStart_NeverLandsMidEmoji()
    {
        // "😀12" = emoji cols 0-1, "1" col 2, "2" col 3
        Assert.Equal(0, DisplayWidth.SnapToGraphemeStart("😀12", 0));
        Assert.Equal(0, DisplayWidth.SnapToGraphemeStart("😀12", 1));
        Assert.Equal(2, DisplayWidth.SnapToGraphemeStart("😀12", 2));
        Assert.Equal(3, DisplayWidth.SnapToGraphemeStart("😀12", 3));
    }

    private static ScrollingTextViewport CreateViewport()
    {
        var engine = new ScrollingTextEngine();
        return new ScrollingTextViewport(engine);
    }

    [Fact]
    public void DoubleFrameDelta_DoublesScrollAdvancePerTick()
    {
        var text = new PlainText("0123456789ABCDEF");
        int width = 4;
        var v1 = CreateViewport();
        _ = v1.Render(text, width, 1.0, D60);
        string afterTwoSmall = v1.Render(text, width, 1.0, D60);

        var v2 = CreateViewport();
        string afterOneDouble = v2.Render(text, width, 1.0, 2.0 * D60);

        Assert.Equal(afterTwoSmall, afterOneDouble);
    }

    [Fact]
    public void Emoji_NotSplit_WhenScrolling()
    {
        var viewport = CreateViewport();
        var text = new PlainText("Hello 😀 World");

        for (int step = 0; step < 20; step++)
        {
            string result = viewport.Render(text, 8, 1.0, D60);

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
        _ = viewport.Render(text, width, 1.0, D60);
        string result = viewport.Render(text, width, 1.0, D60);

        int displayWidth = AnsiConsole.GetDisplayWidth(result);
        Assert.Equal(width, displayWidth);
    }

    [Fact]
    public void EmojiFirst_AtScrollLeft_OutputHasConsistentWidth()
    {
        var viewport = CreateViewport();
        int width = 5;
        var textWithEmojiFirst = new PlainText("😀1234");   // 6 graphemes: emoji + 5 digits
        var textWithoutEmojiFirst = new PlainText("01234"); // 6 graphemes (no emoji at start)

        // Drive to scroll-left (offset 0): first Render goes right, second goes back left
        string resultEmojiFirst = viewport.Render(textWithEmojiFirst, width, 1.0, D60);
        resultEmojiFirst = viewport.Render(textWithEmojiFirst, width, 1.0, D60);

        // Use new viewport for second text (content change resets state)
        var viewport2 = CreateViewport();
        string resultNoEmoji = viewport2.Render(textWithoutEmojiFirst, width, 1.0, D60);
        resultNoEmoji = viewport2.Render(textWithoutEmojiFirst, width, 1.0, D60);

        int lenEmoji = AnsiConsole.GetDisplayWidth(resultEmojiFirst);
        int lenNoEmoji = AnsiConsole.GetDisplayWidth(resultNoEmoji);

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
        _ = viewport.RenderWithLabel("Device", deviceName, totalWidth, 1.0, D60, palette.Label, palette.Normal);
        string result = viewport.RenderWithLabel("Device", deviceName, totalWidth, 1.0, D60, palette.Label, palette.Normal);

        int displayWidth = AnsiConsole.GetDisplayWidth(result);
        Assert.Equal(totalWidth, displayWidth);
    }

    [Fact]
    public void EmojiFirst_RenderWithLabel_AtScrollLeft_OutputHasConsistentWidth()
    {
        int totalWidth = 5;
        var palette = new UiPalette();
        var deviceWithEmojiFirst = new PlainText("😀Microphone Device Name");
        var deviceWithoutEmojiFirst = new PlainText("Microphone Device Name");

        var viewport1 = CreateViewport();
        _ = viewport1.RenderWithLabel("Device", deviceWithEmojiFirst, totalWidth, 1.0, D60, palette.Label, palette.Normal);
        string resultEmoji = viewport1.RenderWithLabel("Device", deviceWithEmojiFirst, totalWidth, 1.0, D60, palette.Label, palette.Normal);

        var viewport2 = CreateViewport();
        _ = viewport2.RenderWithLabel("Device", deviceWithoutEmojiFirst, totalWidth, 1.0, D60, palette.Label, palette.Normal);
        string resultNoEmoji = viewport2.RenderWithLabel("Device", deviceWithoutEmojiFirst, totalWidth, 1.0, D60, palette.Label, palette.Normal);

        int lenEmoji = AnsiConsole.GetDisplayWidth(resultEmoji);
        int lenNoEmoji = AnsiConsole.GetDisplayWidth(resultNoEmoji);

        Assert.Equal(totalWidth, lenEmoji);
        Assert.Equal(totalWidth, lenNoEmoji);
        Assert.Equal(lenNoEmoji, lenEmoji);
    }

    [Fact]
    public void FormatLabel_ReturnsLabelWithColonOnly()
    {
        var viewport = CreateViewport();
        Assert.Equal("Now:", viewport.FormatLabel("Now"));
        Assert.Equal("Device:", viewport.FormatLabel("Device"));
        Assert.Equal("", viewport.FormatLabel(""));
        Assert.Equal("", viewport.FormatLabel(null));
    }
}
