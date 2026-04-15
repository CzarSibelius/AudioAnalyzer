using System;
using System.Text;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Viewports;

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
    /// <summary>Last flushed cell content per row (parallel to the cell buffer); used to skip ANSI assembly and string allocation when a row is unchanged.</summary>
    private char[] _lastFlushedChars = [];
    private PaletteColor[] _lastFlushedColors = [];
    private readonly StringBuilder _sb = new();
    private readonly List<ClipRect> _clipStack = [];

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

        _clipStack.Clear();
        _width = width;
        _height = height;
        int size = width * height;
        _chars = new char[size];
        _colors = new PaletteColor[size];
        _lastFlushedChars = new char[size];
        _lastFlushedColors = new PaletteColor[size];
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

    /// <summary>Removes all clip regions. Call at the start of each frame before compositing layers.</summary>
    public void ClearClipStack()
    {
        _clipStack.Clear();
    }

    /// <summary>Intersects the active clip with the given rectangle (cell coordinates). Balanced with <see cref="PopClip"/>.</summary>
    public void PushClip(int left, int top, int width, int height)
    {
        if (_width <= 0 || _height <= 0)
        {
            return;
        }

        var next = ClipRect.FromBox(left, top, width, height, _width, _height);
        if (_clipStack.Count == 0)
        {
            _clipStack.Add(next);
        }
        else
        {
            _clipStack.Add(_clipStack[^1].Intersect(next));
        }
    }

    /// <summary>Pops the clip pushed last. Safe if the stack is empty.</summary>
    public void PopClip()
    {
        if (_clipStack.Count > 0)
        {
            _clipStack.RemoveAt(_clipStack.Count - 1);
        }
    }

    /// <summary>Sets a cell at (x, y). Coordinates are zero-based. No-op if out of bounds or outside the active clip (if any).</summary>
    public void Set(int x, int y, char c, PaletteColor color)
    {
        if (x < 0 || x >= _width || y < 0 || y >= _height)
        {
            return;
        }

        if (_clipStack.Count > 0)
        {
            var c0 = _clipStack[^1];
            if (c0.IsEmpty || x < c0.MinX || x >= c0.MaxX || y < c0.MinY || y >= c0.MaxY)
            {
                return;
            }
        }

        int i = y * _width + x;
        _chars[i] = c;
        _colors[i] = color;
    }

    /// <summary>Gets the character and color at (x, y). Returns (' ', default) if out of bounds.</summary>
    public (char C, PaletteColor Color) Get(int x, int y)
    {
        if (x < 0 || x >= _width || y < 0 || y >= _height)
        {
            return (' ', default);
        }

        int i = y * _width + x;
        return (_chars[i], _colors[i]);
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

        if (_lastFlushedChars.Length != _width * _height || _lastFlushedColors.Length != _width * _height)
        {
            int size = _width * _height;
            _lastFlushedChars = new char[size];
            _lastFlushedColors = new PaletteColor[size];
        }

        int capacity = _width * 32;
        if (_sb.Capacity < capacity)
        {
            _sb.Capacity = capacity;
        }

        ReadOnlySpan<char> curChars = _chars;
        ReadOnlySpan<PaletteColor> curColors = _colors;
        Span<char> prevChars = _lastFlushedChars;
        Span<PaletteColor> prevColors = _lastFlushedColors;
        for (int y = 0; y < _height; y++)
        {
            int rowStart = y * _width;
            ReadOnlySpan<char> rowC = curChars.Slice(rowStart, _width);
            ReadOnlySpan<PaletteColor> rowCol = curColors.Slice(rowStart, _width);
            if (rowC.SequenceEqual(prevChars.Slice(rowStart, _width))
                && rowCol.SequenceEqual(prevColors.Slice(rowStart, _width)))
            {
                continue;
            }

            _sb.Clear();
            for (int x = 0; x < _width; x++)
            {
                int i = rowStart + x;
                AnsiConsole.AppendColored(_sb, _chars[i], _colors[i]);
            }

            string line = _sb.ToString();
            writer.WriteLine(startRow + y, line);
            rowC.CopyTo(prevChars.Slice(rowStart, _width));
            rowCol.CopyTo(prevColors.Slice(rowStart, _width));
        }
    }

    private readonly struct ClipRect
    {
        public int MinX { get; }
        public int MinY { get; }
        public int MaxX { get; }
        public int MaxY { get; }
        public bool IsEmpty => MinX >= MaxX || MinY >= MaxY;

        private ClipRect(int minX, int minY, int maxX, int maxY)
        {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }

        public static ClipRect FromBox(int left, int top, int width, int height, int bufW, int bufH)
        {
            if (width < 1 || height < 1 || bufW < 1 || bufH < 1)
            {
                return new ClipRect(0, 0, 0, 0);
            }

            int minX = Math.Clamp(left, 0, bufW - 1);
            int minY = Math.Clamp(top, 0, bufH - 1);
            int maxX = Math.Clamp(left + width, minX + 1, bufW);
            int maxY = Math.Clamp(top + height, minY + 1, bufH);
            return new ClipRect(minX, minY, maxX, maxY);
        }

        public ClipRect Intersect(ClipRect other)
        {
            if (IsEmpty || other.IsEmpty)
            {
                return new ClipRect(0, 0, 0, 0);
            }

            int minX = Math.Max(MinX, other.MinX);
            int minY = Math.Max(MinY, other.MinY);
            int maxX = Math.Min(MaxX, other.MaxX);
            int maxY = Math.Min(MaxY, other.MaxY);
            return new ClipRect(minX, minY, maxX, maxY);
        }
    }
}
