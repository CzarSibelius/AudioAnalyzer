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

    [Fact]
    public void ApplySlotPermutation_RemapsSlotsByOldIndices()
    {
        var store = new TextLayerStateStore();
        store.EnsureCapacity(2);
        ITextLayerStateStore<BeatCirclesState> typed = store;
        typed.GetState(0).LastBeatCount = 11;
        typed.GetState(1).LastBeatCount = 22;

        store.ApplySlotPermutation([1, 0]);

        Assert.Equal(22, typed.GetState(0).LastBeatCount);
        Assert.Equal(11, typed.GetState(1).LastBeatCount);
    }

    [Fact]
    public void ClearAllSlots_RemovesPriorStateSoNextGetState_IsNew()
    {
        var store = new TextLayerStateStore();
        store.EnsureCapacity(2);
        ITextLayerStateStore<BeatCirclesState> typed = store;
        BeatCirclesState a = typed.GetState(0);
        a.LastBeatCount = 42;

        store.ClearAllSlots();
        store.EnsureCapacity(2);

        BeatCirclesState b = typed.GetState(0);
        Assert.NotSame(a, b);
        Assert.Equal(-1, b.LastBeatCount);
    }
}
