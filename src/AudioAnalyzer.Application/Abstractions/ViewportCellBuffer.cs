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
    private string?[] _lastWrittenByRow = [];
    private readonly StringBuilder _sb = new();

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
        _lastWrittenByRow = new string?[height];
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

    /// <summary>Flushes the buffer to the writer starting at the given row. Only writes rows that changed (diff-based).</summary>
    /// <param name="writer">The console writer abstraction (implemented by the Console project).</param>
    /// <param name="startRow">The console row where the first buffer line is written.</param>
    public void FlushTo(IConsoleWriter writer, int startRow)
    {
        if (_width <= 0 || _height <= 0)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(writer);

        if (_lastWrittenByRow.Length != _height)
        {
            _lastWrittenByRow = new string?[_height];
        }

        int capacity = _width * 32;
        if (_sb.Capacity < capacity)
        {
            _sb.Capacity = capacity;
        }

        for (int y = 0; y < _height; y++)
        {
            _sb.Clear();
            for (int x = 0; x < _width; x++)
            {
                int i = y * _width + x;
                AnsiConsole.AppendColored(_sb, _chars[i], _colors[i]);
            }

            string line = _sb.ToString();
            if (line == _lastWrittenByRow[y])
            {
                continue;
            }

            _lastWrittenByRow[y] = line;
            writer.WriteLine(startRow + y, line);
        }
    }
}
