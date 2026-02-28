namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Marker interface for types used as the context parameter to <see cref="IKeyHandler{TContext}.Handle"/>.
/// All key-handler context types implement this interface to support DI and shared key-handling utilities.
/// </summary>
public interface IKeyHandlerContext
{
}
