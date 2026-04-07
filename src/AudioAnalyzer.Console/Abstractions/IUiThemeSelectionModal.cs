using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Console;

/// <summary>Selects a UI theme palette (shared with TextLayers) or custom inline colors.</summary>
internal interface IUiThemeSelectionModal
{
    /// <summary>
    /// Shows the theme list. (Custom) clears <c>UiThemeId</c>; otherwise sets it to the chosen theme file id.
    /// Calls <paramref name="saveSettings"/> after a successful selection.
    /// </summary>
    void Show(Action<bool> setModalOpen, Func<AnalysisSnapshot> getSnapshot, Action saveSettings);
}
