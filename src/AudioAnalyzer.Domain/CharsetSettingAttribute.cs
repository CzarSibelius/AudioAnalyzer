namespace AudioAnalyzer.Domain;

/// <summary>
/// Marks a string property as a <c>charsets/*.json</c> id for the S modal (ADR-0080). The console layer maps this to charset-picker edit mode.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class CharsetSettingAttribute : Attribute
{
}
