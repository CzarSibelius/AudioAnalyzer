namespace AudioAnalyzer.Application.Abstractions;

/// <summary>Structured info for the currently playing media. Callers can display Title, Artist, Album or use <see cref="ToDisplayString"/> for a default "Artist - Title" line.</summary>
/// <param name="Title">Track title, or null/empty when unknown.</param>
/// <param name="Artist">Artist name, or null/empty when unknown.</param>
/// <param name="Album">Album name, or null/empty when unknown.</param>
public sealed record NowPlayingInfo(string? Title, string? Artist, string? Album = null)
{
    /// <summary>Returns a single-line display string: "Artist - Title", or just Title/Artist when the other is missing, or empty when both missing.</summary>
    public string ToDisplayString()
    {
        string? title = Title?.Trim();
        string? artist = Artist?.Trim();

        if (string.IsNullOrEmpty(title))
        {
            return string.IsNullOrEmpty(artist) ? "" : artist;
        }

        if (string.IsNullOrEmpty(artist))
        {
            return title;
        }

        return $"{artist} - {title}";
    }
}
