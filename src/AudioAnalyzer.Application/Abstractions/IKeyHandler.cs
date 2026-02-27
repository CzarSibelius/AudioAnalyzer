namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Common contract for key handlers that take a context type. UI components (main loop, modals, visualizers)
/// use a renderer plus an implementation of this interface for consistent key handling.
/// </summary>
/// <typeparam name="TContext">Component-specific context (state and operations) passed each key.</typeparam>
public interface IKeyHandler<in TContext>
{
    /// <summary>Returns the key bindings this handler supports. Used by dynamic help and other consumers. Per ADR-0048.</summary>
    /// <returns>List of key binding entries (key display, description, optional section). Must stay in sync with <see cref="Handle"/>.</returns>
    IReadOnlyList<KeyBinding> GetBindings();

    /// <summary>Handles the key. Returns true when the key was handled (caller-specific semantics apply).</summary>
    /// <param name="key">The key that was pressed.</param>
    /// <param name="context">Component-specific context. May be mutated by the handler.</param>
    /// <returns>True if the key was handled; false if unknown.</returns>
    bool Handle(ConsoleKeyInfo key, TContext context);
}
