using System.Text;

namespace AudioAnalyzer.Application.Abstractions;

/// <summary>Static utilities for text transformation and display.</summary>
public static class TextHelpers
{
    /// <summary>
    /// Applies a hacker-style transformation: first character to lowercase, second to uppercase,
    /// and whitespace replaced with underscores. Used for title bar breadcrumb segments.
    /// </summary>
    /// <param name="value">The input string to transform.</param>
    /// <returns>The transformed string, e.g. "Preset" → "pReset", "Show" → "sHow".</returns>
    public static string Hackerize(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value ?? "";
        }

        var sb = new StringBuilder(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (char.IsWhiteSpace(c))
            {
                sb.Append('_');
            }
            else
            {
                sb.Append(c);
            }
        }

        string result = sb.ToString();
        if (result.Length >= 1 && char.IsLetter(result[0]))
        {
            result = char.ToLowerInvariant(result[0]) + result[1..];
        }
        if (result.Length >= 2 && char.IsLetter(result[1]))
        {
            result = result[..1] + char.ToUpperInvariant(result[1]) + result[2..];
        }

        return result;
    }
}
