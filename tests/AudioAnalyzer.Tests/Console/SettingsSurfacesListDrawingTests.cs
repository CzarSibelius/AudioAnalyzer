using AudioAnalyzer.Console;
using Xunit;

namespace AudioAnalyzer.Tests.Console;

public sealed class SettingsSurfacesListDrawingTests
{
    [Theory]
    [InlineData(0, 5, 10, 0)]
    [InlineData(4, 5, 10, 0)]
    [InlineData(9, 10, 10, 0)]
    [InlineData(5, 10, 3, 3)]
    [InlineData(9, 10, 3, 7)]
    public void ComputeListScrollOffset_KeepsSelectionVisible(
        int selectedIndex,
        int totalCount,
        int visibleCount,
        int expectedOffset)
    {
        int offset = SettingsSurfacesListDrawing.ComputeListScrollOffset(selectedIndex, totalCount, visibleCount);
        Assert.Equal(expectedOffset, offset);
        Assert.InRange(selectedIndex, offset, offset + visibleCount - 1);
    }
}
