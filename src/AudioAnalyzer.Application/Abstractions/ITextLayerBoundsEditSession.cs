using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Abstractions;

/// <summary>Keyboard WYSIWYG session for editing a layer's <see cref="TextLayerSettings.RenderBounds"/> on the live visualizer.</summary>
public interface ITextLayerBoundsEditSession
{
    /// <summary>True while the user is moving/resizing the region.</summary>
    bool IsActive { get; }

    /// <summary>Sorted layer index being edited, or null when inactive.</summary>
    int? EditingSortedLayerIndex { get; }

    /// <summary>Updated each frame by the text-layers visualizer with the current viewport size in cells.</summary>
    void SetLastViewport(int width, int height);

    /// <summary>Starts editing the given sorted layer. Mutates <paramref name="textLayers"/> layers in place.</summary>
    void BeginEdit(int sortedLayerIndex, TextLayersVisualizerSettings textLayers);

    /// <summary>Bindings for help while <see cref="IsActive"/>.</summary>
    IReadOnlyList<KeyBinding> GetHelpBindings();

    /// <summary>Handles keys while active. Returns true if the key was consumed (caller should redraw and persist settings).</summary>
    bool HandleKey(ConsoleKeyInfo key);
}
