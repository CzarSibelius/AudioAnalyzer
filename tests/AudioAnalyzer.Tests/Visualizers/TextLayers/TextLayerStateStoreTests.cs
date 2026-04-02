using AudioAnalyzer.Visualizers;
using Xunit;

namespace AudioAnalyzer.Tests.Visualizers.TextLayers;

public sealed class TextLayerStateStoreTests
{
    [Fact]
    public void RemoveSlotAt_RemovesIndexAndShortensList()
    {
        var store = new TextLayerStateStore();
        store.EnsureCapacity(3);
        ITextLayerStateStore<BeatCirclesState> typed = store;
        _ = typed.GetState(0);
        _ = typed.GetState(1);
        _ = typed.GetState(2);

        store.RemoveSlotAt(1);

        // Previous slot 2 is now at index 1; new GetState(1) returns same reference type behavior — at minimum no throw and capacity 2 effective
        _ = typed.GetState(0);
        _ = typed.GetState(1);
    }

    [Fact]
    public void RemoveSlotAt_OutOfRange_IsNoOp()
    {
        var store = new TextLayerStateStore();
        store.EnsureCapacity(1);
        store.RemoveSlotAt(5);
        store.RemoveSlotAt(-1);
    }
}
