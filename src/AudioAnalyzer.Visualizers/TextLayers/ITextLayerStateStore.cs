namespace AudioAnalyzer.Visualizers;

/// <summary>Provides capacity and clear operations for the shared per-slot state store. Used by TextLayersVisualizer for EnsureCapacity and clearing state when layer type changes.</summary>
public interface ITextLayerStateStore
{
    /// <summary>Ensures at least <paramref name="layerCount"/> slots exist. Call at the start of each render pass.</summary>
    void EnsureCapacity(int layerCount);

    /// <summary>Clears the state at the given layer index so the next draw will create state for the layer type at that slot.</summary>
    void ClearState(int layerIndex);

    /// <summary>Removes the slot at <paramref name="sortedLayerIndex"/> and shifts higher indices down (after a layer is removed from the sorted list).</summary>
    void RemoveSlotAt(int sortedLayerIndex);

    /// <summary>
    /// Reorders in-memory slots when the sorted draw order changes without add/remove (e.g. ZOrder swap).
    /// <paramref name="oldIndexByNewSlot"/> has length <c>n</c>; new slot <c>j</c> receives the state that was at old index <c>oldIndexByNewSlot[j]</c>.
    /// </summary>
    void ApplySlotPermutation(IReadOnlyList<int> oldIndexByNewSlot);

    /// <summary>Clears all per-slot state (e.g. Ctrl+R full layer reset). Next draw recreates state per layer type.</summary>
    void ClearAllSlots();
}
