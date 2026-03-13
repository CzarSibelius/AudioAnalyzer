using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application;

/// <summary>
/// Renders a row of viewports into one line. Holds scroll state per slot; uses <see cref="IScrollingTextEngine"/>
/// for scrolling and applies label formatting and ANSI colors per cell.
/// </summary>
public sealed class LabeledRowRenderer : ILabeledRowRenderer, IUiComponentRenderer<LabeledRowComponent>
{
    private readonly IScrollingTextEngine _engine;
    private readonly List<SlotState> _slotStates = [];

    /// <summary>Creates a new row renderer that uses the given scrolling engine.</summary>
    public LabeledRowRenderer(IScrollingTextEngine engine)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
    }

    /// <inheritdoc />
    public ComponentRenderResult Render(LabeledRowComponent component, RenderContext context)
    {
        if (component.Viewports.Count == 0)
        {
            return ComponentRenderResult.Line(1, context.Width > 0 ? new string(' ', context.Width) : "");
        }

        string line = RenderRowWithComponentState(component, context);
        return ComponentRenderResult.Line(1, line);
    }

    private string RenderRowWithComponentState(LabeledRowComponent component, RenderContext context)
    {
        if (component.Viewports.Count == 0 || component.Widths.Count != component.Viewports.Count)
        {
            return context.Width > 0 ? new string(' ', context.Width) : "";
        }

        var palette = context.Palette ?? new UiPalette();
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < component.Viewports.Count; i++)
        {
            int cellWidth = i < component.Widths.Count ? Math.Max(0, component.Widths[i]) : 0;
            var vp = component.Viewports[i];
            var labelColor = vp.LabelColor ?? palette.Label;
            var textColor = vp.TextColor ?? palette.Normal;
            var slot = component.GetSlotState(i);
            string cell = RenderCellWithSlot(component.Viewports[i], cellWidth, context.ScrollSpeed, labelColor, textColor, slot);
            sb.Append(cell);
        }

        string line = sb.ToString();
        int displayWidth = AnsiConsole.GetDisplayWidth(line);
        if (displayWidth > context.Width)
        {
            return AnsiConsole.GetDisplaySubstring(line, 0, context.Width);
        }

        if (displayWidth < context.Width)
        {
            return AnsiConsole.PadToDisplayWidth(line, context.Width);
        }

        return line;
    }

    /// <inheritdoc />
    public string RenderRow(
        IReadOnlyList<Viewport> viewports,
        IReadOnlyList<int> widths,
        int totalWidth,
        UiPalette palette,
        double scrollSpeed,
        int startSlotIndex = 0)
    {
        if (viewports == null || viewports.Count == 0 || widths == null || widths.Count != viewports.Count)
        {
            return totalWidth > 0 ? new string(' ', totalWidth) : "";
        }

        EnsureSlotCount(startSlotIndex + viewports.Count);

        var paletteObj = palette ?? new UiPalette();

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < viewports.Count; i++)
        {
            int cellWidth = i < widths.Count ? Math.Max(0, widths[i]) : 0;
            var vp = viewports[i];
            var labelColor = vp.LabelColor ?? paletteObj.Label;
            var textColor = vp.TextColor ?? paletteObj.Normal;
            string cell = RenderCell(viewports[i], cellWidth, scrollSpeed, labelColor, textColor, startSlotIndex + i);
            sb.Append(cell);
        }

        string line = sb.ToString();
        int displayWidth = AnsiConsole.GetDisplayWidth(line);
        if (displayWidth > totalWidth)
        {
            return AnsiConsole.GetDisplaySubstring(line, 0, totalWidth);
        }

        if (displayWidth < totalWidth)
        {
            return AnsiConsole.PadToDisplayWidth(line, totalWidth);
        }

        return line;
    }

    private void EnsureSlotCount(int count)
    {
        while (_slotStates.Count < count)
        {
            _slotStates.Add(new SlotState());
        }
    }

    private static string FormatLabel(string label, string? hotkey)
    {
        var baseLabel = label ?? "";
        if (string.IsNullOrWhiteSpace(hotkey))
        {
            return string.IsNullOrEmpty(baseLabel) ? "" : baseLabel + ":";
        }
        return string.IsNullOrEmpty(baseLabel) ? "" : baseLabel + "(" + hotkey + "):";
    }

    private string RenderCellWithSlot(Viewport viewport, int cellWidth, double scrollSpeed,
        PaletteColor labelColor, PaletteColor textColor, LabeledRowSlotState slot)
    {
        if (cellWidth <= 0)
        {
            return "";
        }

        if (viewport.PreformattedAnsi)
        {
            IDisplayText value = viewport.GetValue();
            if (string.IsNullOrEmpty(value.Value))
            {
                return AnsiConsole.PadToDisplayWidth("", cellWidth);
            }
            if (value.GetDisplayWidth() > cellWidth)
            {
                if (value.Value != slot.LastText)
                {
                    slot.GetScrollStateRef().Reset();
                    slot.LastText = value.Value;
                }
                string part = _engine.GetVisibleSlice(value, cellWidth, ref slot.GetScrollStateRef(), scrollSpeed);
                slot.LastText = value.Value;
                return AnsiConsole.GetDisplayWidth(part) > cellWidth
                    ? AnsiConsole.GetDisplaySubstring(part, 0, cellWidth)
                    : AnsiConsole.PadToDisplayWidth(part, cellWidth);
            }
            string truncated = StaticTextViewport.TruncateWithEllipsis(value, cellWidth);
            return AnsiConsole.GetDisplayWidth(truncated) > cellWidth
                ? AnsiConsole.GetDisplaySubstring(truncated, 0, cellWidth)
                : AnsiConsole.PadToDisplayWidth(truncated, cellWidth);
        }

        string effectiveLabel = FormatLabel(viewport.Label, viewport.Hotkey);
        int labelDisplayWidth = string.IsNullOrEmpty(effectiveLabel)
            ? 0
            : DisplayWidth.GetDisplayWidth(effectiveLabel);
        int scrollWidth = Math.Max(0, cellWidth - labelDisplayWidth);

        IDisplayText text = viewport.GetValue();
        if (text.Value != slot.LastText)
        {
            slot.GetScrollStateRef().Reset();
            slot.LastText = text.Value;
        }

        string scrollPart;
        if (string.IsNullOrEmpty(text.Value))
        {
            scrollPart = new string(' ', scrollWidth);
        }
        else
        {
            ref ScrollingTextViewportState stateRef = ref slot.GetScrollStateRef();
            scrollPart = _engine.GetVisibleSlice(text, scrollWidth, ref stateRef, scrollSpeed);
        }

        slot.LastText = text.Value;

        string labelSegment = AnsiConsole.ColorCode(labelColor) + effectiveLabel + AnsiConsole.ResetCode;
        string scrollSegment = AnsiConsole.ColorCode(textColor) + scrollPart + AnsiConsole.ResetCode;
        string result = labelSegment + scrollSegment;
        int displayWidth = AnsiConsole.GetDisplayWidth(result);
        return displayWidth > cellWidth
            ? AnsiConsole.GetDisplaySubstring(result, 0, cellWidth)
            : AnsiConsole.PadToDisplayWidth(result, cellWidth);
    }

    private string RenderCell(Viewport viewport, int cellWidth, double scrollSpeed,
        PaletteColor labelColor, PaletteColor textColor, int slotIndex)
    {
        if (cellWidth <= 0)
        {
            return "";
        }

        SlotState slot = _slotStates[slotIndex];
        if (viewport.PreformattedAnsi)
        {
            IDisplayText value = viewport.GetValue();
            if (string.IsNullOrEmpty(value.Value))
            {
                return AnsiConsole.PadToDisplayWidth("", cellWidth);
            }
            if (value.GetDisplayWidth() > cellWidth)
            {
                if (value.Value != slot.LastText)
                {
                    slot.ScrollState.Reset();
                    slot.LastText = value.Value;
                }
                string part = _engine.GetVisibleSlice(value, cellWidth, ref slot.ScrollState, scrollSpeed);
                slot.LastText = value.Value;
                return AnsiConsole.GetDisplayWidth(part) > cellWidth
                    ? AnsiConsole.GetDisplaySubstring(part, 0, cellWidth)
                    : AnsiConsole.PadToDisplayWidth(part, cellWidth);
            }
            string truncated = StaticTextViewport.TruncateWithEllipsis(value, cellWidth);
            return AnsiConsole.GetDisplayWidth(truncated) > cellWidth
                ? AnsiConsole.GetDisplaySubstring(truncated, 0, cellWidth)
                : AnsiConsole.PadToDisplayWidth(truncated, cellWidth);
        }

        string effectiveLabel = FormatLabel(viewport.Label, viewport.Hotkey);
        int labelDisplayWidth = string.IsNullOrEmpty(effectiveLabel)
            ? 0
            : DisplayWidth.GetDisplayWidth(effectiveLabel);
        int scrollWidth = Math.Max(0, cellWidth - labelDisplayWidth);

        IDisplayText text = viewport.GetValue();
        if (text.Value != slot.LastText)
        {
            slot.ScrollState.Reset();
            slot.LastText = text.Value;
        }

        string scrollPart;
        if (string.IsNullOrEmpty(text.Value))
        {
            scrollPart = new string(' ', scrollWidth);
        }
        else
        {
            ref ScrollingTextViewportState stateRef = ref slot.ScrollState;
            scrollPart = _engine.GetVisibleSlice(text, scrollWidth, ref stateRef, scrollSpeed);
        }

        slot.LastText = text.Value;

        string labelSegment = AnsiConsole.ColorCode(labelColor) + effectiveLabel + AnsiConsole.ResetCode;
        string scrollSegment = AnsiConsole.ColorCode(textColor) + scrollPart + AnsiConsole.ResetCode;
        string result = labelSegment + scrollSegment;
        int displayWidth = AnsiConsole.GetDisplayWidth(result);
        return displayWidth > cellWidth
            ? AnsiConsole.GetDisplaySubstring(result, 0, cellWidth)
            : AnsiConsole.PadToDisplayWidth(result, cellWidth);
    }

    private sealed class SlotState
    {
        public ScrollingTextViewportState ScrollState;
        public string? LastText;
    }
}
