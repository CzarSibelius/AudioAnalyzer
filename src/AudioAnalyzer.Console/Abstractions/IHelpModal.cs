using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Help modal: displays keyboard controls and usage information. Per ADR-0049 content is dynamic from handler bindings.</summary>
internal interface IHelpModal
{
    /// <summary>Shows the help modal; any key closes it. Content is ordered by current view when currentMode is provided.</summary>
    /// <param name="currentMode">Active application mode (Preset editor or Show play); used to prioritize section order. Defaults to Preset editor when null.</param>
    /// <param name="onEnter">Called when the modal opens.</param>
    /// <param name="onClose">Called when the modal closes.</param>
    void Show(ApplicationMode? currentMode = null, Action? onEnter = null, Action? onClose = null);
}
