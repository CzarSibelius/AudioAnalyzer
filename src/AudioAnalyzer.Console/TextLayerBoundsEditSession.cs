using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Per-layer render bounds keyboard editing on the live visualizer (arrows move, Shift+arrows resize).</summary>
internal sealed class TextLayerBoundsEditSession : ITextLayerBoundsEditSession
{
    private const string Section = "Layer render bounds (visual edit)";
    private TextLayerSettings? _layer;
    private TextLayerRenderBounds? _backup;
    private int _sortedIndex;
    private int _vw = 80;
    private int _vh = 24;

    /// <inheritdoc />
    public bool IsActive => _layer != null;

    /// <inheritdoc />
    public int? EditingSortedLayerIndex => IsActive ? _sortedIndex : null;

    /// <inheritdoc />
    public void SetLastViewport(int width, int height)
    {
        if (width > 0) { _vw = width; }
        if (height > 0) { _vh = height; }
    }

    /// <inheritdoc />
    public void BeginEdit(int sortedLayerIndex, TextLayersVisualizerSettings textLayers)
    {
        var layers = textLayers.Layers ?? [];
        var sorted = layers.OrderBy(l => l.ZOrder).ToList();
        if (sortedLayerIndex < 0 || sortedLayerIndex >= sorted.Count)
        {
            return;
        }

        _layer = sorted[sortedLayerIndex];
        _sortedIndex = sortedLayerIndex;
        _backup = _layer.RenderBounds?.DeepCopy();

        if (_layer.RenderBounds == null)
        {
            _layer.RenderBounds = new TextLayerRenderBounds { X = 0, Y = 0, Width = 1, Height = 1 };
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<KeyBinding> GetHelpBindings()
    {
        var list = new List<KeyBinding>
        {
            new("↑/↓/←/→", "Move region", Section),
            new("Shift+↑/↓/←/→", "Resize region (bottom-right)", Section),
            new("Enter", "Apply and exit", Section),
            new("Esc", "Cancel and exit", Section),
        };
        return list;
    }

    /// <inheritdoc />
    public bool HandleKey(ConsoleKeyInfo key)
    {
        if (!IsActive || _layer?.RenderBounds is not { } b)
        {
            return false;
        }

        if (key.Key == ConsoleKey.Enter)
        {
            Commit();
            return true;
        }

        if (key.Key == ConsoleKey.Escape)
        {
            Cancel();
            return true;
        }

        bool shift = key.Modifiers.HasFlag(ConsoleModifiers.Shift);
        int dx = 0;
        int dy = 0;
        int dw = 0;
        int dh = 0;
        switch (key.Key)
        {
            case ConsoleKey.LeftArrow:
                if (shift) { dw = -1; }
                else { dx = -1; }
                break;
            case ConsoleKey.RightArrow:
                if (shift) { dw = 1; }
                else { dx = 1; }
                break;
            case ConsoleKey.UpArrow:
                if (shift) { dh = -1; }
                else { dy = -1; }
                break;
            case ConsoleKey.DownArrow:
                if (shift) { dh = 1; }
                else { dy = 1; }
                break;
            default:
                return false;
        }

        double invW = 1.0 / _vw;
        double invH = 1.0 / _vh;
        double minW = invW;
        double minH = invH;

        if (shift)
        {
            if (dw != 0)
            {
                double nw = b.Width + dw * invW;
                nw = Math.Max(minW, nw);
                b.Width = Math.Min(nw, 1.0 - b.X);
            }

            if (dh != 0)
            {
                double nh = b.Height + dh * invH;
                nh = Math.Max(minH, nh);
                b.Height = Math.Min(nh, 1.0 - b.Y);
            }
        }
        else
        {
            if (dx != 0)
            {
                double nx = b.X + dx * invW;
                b.X = Math.Clamp(nx, 0, 1.0 - b.Width);
            }

            if (dy != 0)
            {
                double ny = b.Y + dy * invH;
                b.Y = Math.Clamp(ny, 0, 1.0 - b.Height);
            }
        }

        return true;
    }

    private void Commit()
    {
        _layer = null;
        _backup = null;
    }

    private void Cancel()
    {
        if (_layer == null)
        {
            return;
        }

        if (_backup == null)
        {
            _layer.RenderBounds = null;
        }
        else
        {
            _layer.RenderBounds = _backup.DeepCopy();
        }

        _layer = null;
        _backup = null;
    }
}
