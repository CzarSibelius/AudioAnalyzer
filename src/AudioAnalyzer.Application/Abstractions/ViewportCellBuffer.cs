using System.Text;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// A 2D cell buffer for compositing layered visualizer output. Each cell holds a character and color.
/// Draw layers in order (ascending ZOrder); then write the buffer to the console in one pass.
/// </summary>
public sealed class ViewportCellBuffer
{
    private int _width;
    private int _height;
    private char[] _chars = [];
    private PaletteColor[] _colors = [];

    /// <summary>Width of the buffer (columns).</summary>
    public int Width => _width;

    /// <summary>Height of the buffer (rows).</summary>
    public int Height => _height;

    /// <summary>Ensures the buffer has at least the given dimensions; reallocates if needed.</summary>
    public void EnsureSize(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        if (_width == width && _height == height)
        {
            return;
        }

        _width = width;
        _height = height;
        int size = width * height;
        _chars = new char[size];
        _colors = new PaletteColor[size];
    }

    /// <summary>Fills the buffer with space and the given default color.</summary>
    public void Clear(PaletteColor defaultColor)
    {
        int size = _width * _height;
        for (int i = 0; i < size; i++)
        {
            _chars[i] = ' ';
            _colors[i] = defaultColor;
        }
    }

    /// <summary>Sets a cell at (x, y). Coordinates are zero-based. No-op if out of bounds.</summary>
    public void Set(int x, int y, char c, PaletteColor color)
    {
        if (x < 0 || x >= _width || y < 0 || y >= _height)
        {
            return;
        }

        int i = y * _width + x;
        _chars[i] = c;
        _colors[i] = color;
    }

    /// <summary>Writes the buffer to the console starting at the given row. Builds one ANSI line per row.</summary>
    public void WriteToConsole(int startRow)
    {
        if (_width <= 0 || _height <= 0)
        {
            return;
        }

        var sb = new StringBuilder(_width * 32);
        for (int y = 0; y < _height; y++)
        {
            sb.Clear();
            for (int x = 0; x < _width; x++)
            {
                int i = y * _width + x;
                AnsiConsole.AppendColored(sb, _chars[i], _colors[i]);
            }

            try
            {
                Console.SetCursorPosition(0, startRow + y);
                Console.Write(sb.ToString());
            }
            catch
            {
                // Ignore console errors (e.g. resize)
            }
        }
    }
}
