using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Viewports;
using AudioAnalyzer.Domain;
using Xunit;

namespace AudioAnalyzer.Tests.Application.Viewports;

/// <summary>Regression tests for diff-based flush: unchanged rows must not rebuild ANSI or call the writer (ADR-0030).</summary>
public sealed class ViewportCellBufferFlushToTests
{
    private sealed class CountingConsoleWriter : IConsoleWriter
    {
        public int WriteLineCount { get; private set; }

        public void WriteLine(int row, string line) => WriteLineCount++;
    }

    [Fact]
    public void FlushTo_SecondIdenticalFrame_DoesNotCallWriterAgain()
    {
        var buffer = new ViewportCellBuffer();
        buffer.EnsureSize(40, 5);
        buffer.Clear(PaletteColor.FromConsoleColor(ConsoleColor.Black));
        var writer = new CountingConsoleWriter();

        buffer.FlushTo(writer, 0);
        Assert.Equal(5, writer.WriteLineCount);

        buffer.FlushTo(writer, 0);
        Assert.Equal(5, writer.WriteLineCount);
    }

    [Fact]
    public void FlushTo_RepeatedIdenticalFrames_DoesNotCallWriterAgain()
    {
        var buffer = new ViewportCellBuffer();
        buffer.EnsureSize(80, 24);
        buffer.Clear(PaletteColor.FromConsoleColor(ConsoleColor.DarkBlue));
        var writer = new CountingConsoleWriter();
        buffer.FlushTo(writer, 0);
        int writesAfterFirstFlush = writer.WriteLineCount;
        Assert.Equal(24, writesAfterFirstFlush);

        const int iterations = 200;
        for (int i = 0; i < iterations; i++)
        {
            buffer.FlushTo(writer, 0);
        }

        Assert.Equal(writesAfterFirstFlush, writer.WriteLineCount);
    }

    [Fact]
    public void FlushTo_AfterSingleCellChange_WritesOnlyOnceMore()
    {
        var buffer = new ViewportCellBuffer();
        buffer.EnsureSize(10, 4);
        buffer.Clear(PaletteColor.FromConsoleColor(ConsoleColor.Black));
        var writer = new CountingConsoleWriter();
        buffer.FlushTo(writer, 0);
        Assert.Equal(4, writer.WriteLineCount);

        buffer.Set(0, 0, 'X', PaletteColor.FromConsoleColor(ConsoleColor.White));
        buffer.FlushTo(writer, 0);
        Assert.Equal(5, writer.WriteLineCount);

        buffer.FlushTo(writer, 0);
        Assert.Equal(5, writer.WriteLineCount);
    }
}
