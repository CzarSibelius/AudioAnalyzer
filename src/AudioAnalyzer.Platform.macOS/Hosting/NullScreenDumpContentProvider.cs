using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Platform.macOS.Hosting;

/// <summary>
/// macOS screen dump provider: no console-buffer read API equivalent to Windows, so capture is
/// unavailable and returns null (graceful degradation per ADR-0046).
/// </summary>
public sealed class NullScreenDumpContentProvider : IScreenDumpContentProvider
{
    /// <inheritdoc />
    public string? ReadVisibleConsoleContent() => null;
}
