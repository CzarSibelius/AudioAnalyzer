using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;

namespace AudioAnalyzer.Console;

/// <summary>Supplies the title bar breadcrumb via <see cref="ITitleBarBreadcrumbFormatter"/> (ADR-0036, ADR-0060).</summary>
internal sealed class TitleBarContentProvider : ITitleBarContentProvider
{
    private readonly ITitleBarBreadcrumbFormatter _formatter;

    public TitleBarContentProvider(ITitleBarBreadcrumbFormatter formatter)
    {
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
    }

    /// <inheritdoc />
    public IDisplayText GetTitleBarContent()
    {
        return new AnsiText(_formatter.BuildAnsiLine());
    }
}
