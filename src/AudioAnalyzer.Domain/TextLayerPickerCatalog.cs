namespace AudioAnalyzer.Domain;

/// <summary>Layer types offered by the Preset editor layer picker (L): every <see cref="TextLayerType"/> in stable display order.</summary>
public static class TextLayerPickerCatalog
{
    /// <summary>All enum values sorted by <see cref="TextLayerType"/> name (case-insensitive) for list navigation.</summary>
    public static IReadOnlyList<TextLayerType> OrderedTypes { get; } =
        Enum.GetValues<TextLayerType>()
            .OrderBy(t => t.ToString(), StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
