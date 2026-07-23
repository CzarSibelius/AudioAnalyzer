namespace AudioAnalyzer.Console;

/// <summary>Reusable yes/no confirmation modal (ADR-0093). Y/Enter confirm, N/Esc cancel; used for quit and other destructive actions.</summary>
internal interface IConfirmationModal
{
    /// <summary>Shows the modal and blocks until the user confirms or cancels.</summary>
    /// <param name="title">Short action verb (e.g. <c>quit</c>); used for the breadcrumb suffix and the confirm choice hint.</param>
    /// <param name="prompt">Question shown to the operator (e.g. <c>Quit AudioAnalyzer?</c>).</param>
    /// <param name="setModalOpen">Sets the modal-open render guard (true on enter, false on close).</param>
    /// <returns>True when confirmed (Y/Enter); false when cancelled (N/Esc).</returns>
    bool Show(string title, string prompt, Action<bool> setModalOpen);
}
