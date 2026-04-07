namespace AudioAnalyzer.Application.Abstractions;

/// <summary>Metadata for a UI theme file under <c>themes/</c> (id = filename without extension).</summary>
public sealed record ThemeInfo(string Id, string Name, string? FallbackPaletteId);
