namespace AudioAnalyzer.Domain;

/// <summary>Marks a custom layer-settings property as persisted in JSON but omitted from the S modal list.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ExcludeFromSettingsModalAttribute : Attribute;
