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

/// <summary>Marks a string property as having fixed choices; the S modal uses Cycle mode instead of TextEdit.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SettingChoicesAttribute(params string[] choices) : Attribute
{
    /// <summary>Available choices to cycle through.</summary>
    public string[] Choices { get; } = choices;
}

/// <summary>Specifies min, max, and step for numeric properties when cycling in the S modal.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SettingRangeAttribute(double min, double max, double step = 1) : Attribute
{
    /// <summary>Minimum value (inclusive).</summary>
    public double Min { get; } = min;

    /// <summary>Maximum value (inclusive).</summary>
    public double Max { get; } = max;

    /// <summary>Step size when cycling.</summary>
    public double Step { get; } = step;
}
