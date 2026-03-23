using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Console;

/// <summary>Selects a UI theme palette (shared with TextLayers) or custom inline colors.</summary>
internal interface IUiThemeSelectionModal
{
    /// <summary>
    /// Shows the palette list. (Custom) clears <c>UiThemePaletteId</c>; otherwise sets it to the chosen id.
    /// Calls <paramref name="saveSettings"/> after a successful selection.
    /// </summary>
    void Show(Action<bool> setModalOpen, Func<AnalysisSnapshot> getSnapshot, Action saveSettings);
}
