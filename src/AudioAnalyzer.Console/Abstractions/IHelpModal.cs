namespace AudioAnalyzer.Console;

/// <summary>Help modal: displays keyboard controls and usage information.</summary>
internal interface IHelpModal
{
    /// <summary>Shows the help modal; any key closes it.</summary>
    /// <param name="onEnter">Called when the modal opens.</param>
    /// <param name="onClose">Called when the modal closes.</param>
    void Show(Action? onEnter, Action? onClose);
}
