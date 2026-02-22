using System.Globalization;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>
/// Beat-enabled layer that draws a selected text snippet in a diagonal cascade:
/// one line per beat, each new line offset one character left so aligned columns form a diagonal.
/// Aligned characters use a configurable accent color; after one line per character the cycle loops.
/// </summary>
public sealed class MaschineLayer : ITextLayerRenderer
{
    public TextLayerType LayerType => TextLayerType.Maschine;

    public (double Offset, int SnippetIndex) Draw(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        TextLayerDrawContext ctx)
    {
        int w = ctx.Width;
        int h = ctx.Height;

        if (w < 2 || h < 1)
        {
            return state;
        }

        var snippets = layer.TextSnippets?.Where(s => !string.IsNullOrEmpty(s)).ToList() ?? new List<string>();
        if (snippets.Count == 0)
        {
            return state;
        }

        var maschineState = ctx.MaschineStateForLayer;
        string text = snippets[maschineState.SnippetIndex % snippets.Count];
        if (text.Length == 0)
        {
            return state;
        }

        int textDisplayWidth = DisplayWidth.GetDisplayWidth(text);
        if (textDisplayWidth == 0)
        {
            return state;
        }

        int cycleLength = textDisplayWidth;
        bool onBeat = ctx.Snapshot.BeatCount > maschineState.LastBeatCount;
        if (onBeat)
        {
            maschineState.LastBeatCount = ctx.Snapshot.BeatCount;
            maschineState.Phase = (maschineState.Phase + 1) % Math.Max(1, cycleLength);
            if (maschineState.Phase == 0)
            {
                maschineState.SnippetIndex = (maschineState.SnippetIndex + 1) % Math.Max(1, snippets.Count);
            }
        }

        int phase = maschineState.Phase;
        int numLines = phase + 1;

        var palette = ctx.Palette;
        if (palette is null || palette.Count == 0)
        {
            return state;
        }

        var settings = layer.GetCustom<MaschineSettings>() ?? new MaschineSettings();
        var normalColor = palette[Math.Max(0, layer.ColorIndex % palette.Count)];
        var accentColor = palette[Math.Max(0, settings.AccentColorIndex % palette.Count)];

        // Aligned diagonal (first char of line 0, second of line 1, ...) stays at viewport center.
        int baseCol = w / 2;
        int accentColumn = settings.AccentColumnMode == MaschineAccentColumnMode.Fixed
            ? baseCol
            : baseCol + (phase % Math.Max(1, textDisplayWidth));

        // First line fixed halfway up from vertical center (so full cascade would be centered); new lines appear below without moving existing ones.
        int startY = Math.Max(0, (h - cycleLength) / 2);

        for (int row = 0; row < numLines && startY + row < h; row++)
        {
            int lineStartCol = baseCol - row;
            int colOffset = 0;
            int i = 0;
            int y = startY + row;

            while (i < text.Length)
            {
                int elemLen = StringInfo.GetNextTextElementLength(text.AsSpan(i));
                int gw = DisplayWidth.GetGraphemeWidth(text, i);
                int x = lineStartCol + colOffset;

                if (x >= 0 && x < w)
                {
                    char c = elemLen > 0 ? text[i] : ' ';
                    var color = (x == accentColumn) ? accentColor : normalColor;
                    ctx.Buffer.Set(x, y, c, color);
                }

                colOffset += gw;
                i += elemLen;
            }
        }

        return state;
    }
}
