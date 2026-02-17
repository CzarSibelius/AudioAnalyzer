namespace AudioAnalyzer.Domain;

/// <summary>Marks a string property as having fixed choices; the S modal uses Cycle mode instead of TextEdit.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SettingChoicesAttribute(params string[] choices) : Attribute
{
    /// <summary>Available choices to cycle through.</summary>
    public string[] Choices { get; } = choices;
}
