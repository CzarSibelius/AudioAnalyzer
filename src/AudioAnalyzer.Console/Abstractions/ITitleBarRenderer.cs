namespace AudioAnalyzer.Console;

/// <summary>Renders the application title bar with hierarchical breadcrumb (app/mode/preset/layer) and configurable styling.</summary>
internal interface ITitleBarRenderer
{
    /// <summary>Renders the title bar frame and content. Returns three lines for the header box.</summary>
    /// <param name="width">Console width to fit output within.</param>
    /// <returns>Line1 (top border), Line2 (title content), Line3 (bottom border).</returns>
    (string Line1, string Line2, string Line3) Render(int width);
}
