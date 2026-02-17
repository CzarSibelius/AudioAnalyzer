namespace AudioAnalyzer.Domain;

/// <summary>Optional override for setting Id and Label in the S modal. When omitted, property name is used for both.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SettingAttribute(string? id = null, string? label = null) : Attribute
{
    /// <summary>Display id for the setting. Null = use property name.</summary>
    public string? Id { get; } = id;

    /// <summary>Display label. Null = derive from property name (e.g. ShowVolumeBar → "Show volume bar").</summary>
    public string? Label { get; } = label;
}
