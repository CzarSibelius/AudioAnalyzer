namespace AudioAnalyzer.Domain;

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
