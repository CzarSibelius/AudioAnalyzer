using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Console;

/// <summary>
/// Generic key handler that delegates to an injected config. Enables open generic registration
/// of <see cref="IKeyHandler{TContext}"/>; per-context behavior lives in <see cref="IKeyHandlerConfig{TContext}"/>.
/// </summary>
/// <typeparam name="TContext">Component-specific context. Must implement <see cref="IKeyHandlerContext"/>.</typeparam>
internal sealed class GenericKeyHandler<TContext> : IKeyHandler<TContext> where TContext : IKeyHandlerContext
{
    private readonly IKeyHandlerConfig<TContext> _config;

    public GenericKeyHandler(IKeyHandlerConfig<TContext> config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <inheritdoc />
    public IReadOnlyList<KeyBinding> GetBindings() => _config.GetBindings();

    /// <inheritdoc />
    public bool Handle(ConsoleKeyInfo key, TContext context) => _config.Handle(key, context);
}
