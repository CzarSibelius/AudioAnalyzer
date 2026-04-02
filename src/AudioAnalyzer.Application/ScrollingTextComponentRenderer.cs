using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application;

/// <summary>
/// Renders a single <see cref="ScrollingTextComponent"/> to one line (label + scrolling or preformatted text).
/// Returns <see cref="ComponentRenderResult.Line"/> so a parent (e.g. horizontal row) can collect content without writing.
/// </summary>
public sealed class ScrollingTextComponentRenderer : IUiComponentRenderer<ScrollingTextComponent>
{
    private readonly IScrollingTextEngine _engine;

    /// <summary>Creates a new renderer that uses the given scrolling engine.</summary>
    public ScrollingTextComponentRenderer(IScrollingTextEngine engine)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
    }

    /// <inheritdoc />
    public ComponentRenderResult Render(ScrollingTextComponent component, RenderContext context)
    {
        int cellWidth = context.Width;
        if (cellWidth <= 0)
        {
            return ComponentRenderResult.Line(1, "");
        }

        if (component.GetValue == null)
        {
            return ComponentRenderResult.Line(1, new string(' ', cellWidth));
        }

        var palette = context.Palette ?? new UiPalette();
        var labelColor = component.LabelColor ?? palette.Label;
        var textColor = component.TextColor ?? palette.Normal;

        string cell = component.PreformattedAnsi
            ? RenderPreformattedCell(component, cellWidth, context.ScrollSpeed)
            : RenderLabeledCell(component, cellWidth, context.ScrollSpeed, labelColor, textColor);

        return ComponentRenderResult.Line(1, cell);
    }

    private string RenderPreformattedCell(ScrollingTextComponent component, int cellWidth, double scrollSpeed)
    {
        IDisplayText value = component.GetValue!();
        if (string.IsNullOrEmpty(value.Value))
        {
            return AnsiConsole.PadToDisplayWidth("", cellWidth);
        }
        if (value.GetDisplayWidth() > cellWidth)
        {
            if (value.Value != component.LastText)
            {
                component.GetScrollStateRef().Reset();
                component.LastText = value.Value;
            }
            string part = _engine.GetVisibleSlice(value, cellWidth, ref component.GetScrollStateRef(), scrollSpeed);
            component.LastText = value.Value;
            return AnsiConsole.GetDisplayWidth(part) > cellWidth
                ? AnsiConsole.GetDisplaySubstring(part, 0, cellWidth)
                : AnsiConsole.PadToDisplayWidth(part, cellWidth);
        }
        string truncated = StaticTextViewport.TruncateWithEllipsis(value, cellWidth);
        return AnsiConsole.GetDisplayWidth(truncated) > cellWidth
            ? AnsiConsole.GetDisplaySubstring(truncated, 0, cellWidth)
            : AnsiConsole.PadToDisplayWidth(truncated, cellWidth);
    }

    private string RenderLabeledCell(ScrollingTextComponent component, int cellWidth, double scrollSpeed,
        PaletteColor labelColor, PaletteColor textColor)
    {
        string effectiveLabel = LabelFormatting.FormatLabel(component.Label);
        int labelDisplayWidth = string.IsNullOrEmpty(effectiveLabel)
            ? 0
            : DisplayWidth.GetDisplayWidth(effectiveLabel);
        int scrollWidth = Math.Max(0, cellWidth - labelDisplayWidth);

        IDisplayText text = component.GetValue!();
        if (text.Value != component.LastText)
        {
            component.GetScrollStateRef().Reset();
            component.LastText = text.Value;
        }

        string scrollPart;
        if (string.IsNullOrEmpty(text.Value))
        {
            scrollPart = new string(' ', scrollWidth);
        }
        else
        {
            ref ScrollingTextViewportState stateRef = ref component.GetScrollStateRef();
            scrollPart = _engine.GetVisibleSlice(text, scrollWidth, ref stateRef, scrollSpeed);
        }

        component.LastText = text.Value;

        string labelSegment = AnsiConsole.ColorCode(labelColor) + effectiveLabel + AnsiConsole.ResetCode;
        string scrollSegment = AnsiConsole.ColorCode(textColor) + scrollPart + AnsiConsole.ResetCode;
        string result = labelSegment + scrollSegment;
        int displayWidth = AnsiConsole.GetDisplayWidth(result);
        return displayWidth > cellWidth
            ? AnsiConsole.GetDisplaySubstring(result, 0, cellWidth)
            : AnsiConsole.PadToDisplayWidth(result, cellWidth);
    }
}
