using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Renders the currently playing media (Artist - Title) from the system. Falls back to TextSnippets when no session.</summary>
public sealed class NowPlayingLayer : ITextLayerRenderer
{
    private readonly INowPlayingProvider _nowPlayingProvider;

    public TextLayerType LayerType => TextLayerType.NowPlaying;

    public NowPlayingLayer(INowPlayingProvider nowPlayingProvider)
    {
        _nowPlayingProvider = nowPlayingProvider;
    }

    public (double Offset, int SnippetIndex) Draw(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        TextLayerDrawContext ctx)
    {
        int w = ctx.Width;
        int h = ctx.Height;
        string? nowPlaying = _nowPlayingProvider.GetNowPlayingText()?.Trim();
        var snippets = layer.TextSnippets?.Where(s => !string.IsNullOrEmpty(s)).ToList() ?? new List<string>();
        string text = !string.IsNullOrEmpty(nowPlaying)
            ? nowPlaying
            : snippets.Count > 0
                ? snippets[0]
                : "—";
        if (text.Length == 0)
        {
            text = " ";
        }

        double speed = layer.SpeedMultiplier * ctx.SpeedBurst * 0.8;
        if (layer.BeatReaction == TextLayerBeatReaction.SpeedBurst && ctx.Snapshot.BeatFlashActive)
        {
            speed *= 2.5;
        }

        state.Offset += speed;
        if (layer.BeatReaction == TextLayerBeatReaction.Flash && ctx.Snapshot.BeatFlashActive)
        {
            state.Offset += 1.0;
        }

        var settings = layer.GetCustom<NowPlayingSettings>();
        string position = settings?.VerticalPosition?.Trim() ?? "Center";
        int drawY = position.ToLowerInvariant() switch
        {
            "top" => 0,
            "bottom" => Math.Max(0, h - 1),
            _ => h / 2
        };
        bool pulse = layer.BeatReaction == TextLayerBeatReaction.Pulse && ctx.Snapshot.BeatFlashActive;
        var color = ctx.Palette[Math.Max(0, layer.ColorIndex % ctx.Palette.Count)];
        if (pulse)
        {
            color = ctx.Palette[(layer.ColorIndex + 1) % ctx.Palette.Count];
        }

        if (text.Length <= w)
        {
            int startX = Math.Max(0, (w - text.Length) / 2);
            for (int i = 0; i < text.Length; i++)
            {
                int x = startX + i;
                if (x >= 0 && x < w)
                {
                    char c = text[i];
                    if (pulse && c != ' ')
                    {
                        c = char.IsLower(c) ? char.ToUpperInvariant(c) : (c == ' ' ? ' ' : '█');
                    }
                    ctx.Buffer.Set(x, drawY, c, color);
                }
            }
        }
        else
        {
            int scrollOffset = (int)Math.Floor(state.Offset) % (text.Length + w);
            if (scrollOffset < 0)
            {
                scrollOffset = (scrollOffset % (text.Length + w) + (text.Length + w)) % (text.Length + w);
            }
            for (int x = 0; x < w; x++)
            {
                int srcIndex = (scrollOffset + x) % (text.Length + w);
                char c = ' ';
                if (srcIndex < text.Length)
                {
                    c = text[srcIndex];
                    if (pulse && c != ' ')
                    {
                        c = char.IsLower(c) ? char.ToUpperInvariant(c) : (c == ' ' ? ' ' : '█');
                    }
                }
                ctx.Buffer.Set(x, drawY, c, color);
            }
        }

        return state;
    }
}
