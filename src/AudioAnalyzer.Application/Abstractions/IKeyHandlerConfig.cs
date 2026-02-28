namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Per-context configuration for key handling. Implementations hold the actual bindings and handle logic;
/// <see cref="GenericKeyHandler{TContext}"/> delegates to an injected config. Enables open generic
/// registration of <see cref="IKeyHandler{TContext}"/>.
/// </summary>
/// <typeparam name="TContext">Component-specific context. Must implement <see cref="IKeyHandlerContext"/>.</typeparam>
public interface IKeyHandlerConfig<TContext> where TContext : IKeyHandlerContext
{
    /// <summary>Returns the key bindings this config supports. Used by dynamic help and other consumers.</summary>
    IReadOnlyList<KeyBinding> GetBindings();

    /// <summary>Handles the key. Returns true when the key was handled (caller-specific semantics apply).</summary>
    bool Handle(ConsoleKeyInfo key, TContext context);
}
