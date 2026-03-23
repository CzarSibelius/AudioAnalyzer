using AudioAnalyzer.Application.Viewports;
using AudioAnalyzer.Domain;
using Xunit;

namespace AudioAnalyzer.Tests.Application.Viewports;

public sealed class ViewportCellBufferClipTests
{
    [Fact]
    public void Set_OutsideClip_IsNoOp()
    {
        var buffer = new ViewportCellBuffer();
        buffer.EnsureSize(10, 5);
        var bg = PaletteColor.FromConsoleColor(ConsoleColor.Black);
        var fg = PaletteColor.FromConsoleColor(ConsoleColor.White);
        buffer.Clear(bg);
        buffer.PushClip(2, 1, 4, 3);
        buffer.Set(0, 0, 'X', fg);
        buffer.Set(3, 2, 'Y', fg);
        buffer.PopClip();

        var (c0, _) = buffer.Get(0, 0);
        Assert.Equal(' ', c0);
        var (cy, _) = buffer.Get(3, 2);
        Assert.Equal('Y', cy);
    }

    [Fact]
    public void PushClip_Intersect_NarrowsRegion()
    {
        var buffer = new ViewportCellBuffer();
        buffer.EnsureSize(20, 10);
        var bg = PaletteColor.FromConsoleColor(ConsoleColor.Black);
        var fg = PaletteColor.FromConsoleColor(ConsoleColor.White);
        buffer.Clear(bg);
        buffer.PushClip(0, 0, 20, 10);
        buffer.PushClip(10, 0, 10, 10);
        buffer.Set(5, 5, 'A', fg);
        buffer.Set(15, 5, 'B', fg);
        buffer.PopClip();
        buffer.PopClip();

        Assert.Equal(' ', buffer.Get(5, 5).C);
        Assert.Equal('B', buffer.Get(15, 5).C);
    }
}
