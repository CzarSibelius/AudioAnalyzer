using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Console;

/// <summary>Mutable context for <see cref="ConfirmationKeyHandlerConfig"/>: records the yes/no decision (ADR-0093).</summary>
internal sealed class ConfirmationKeyContext : IKeyHandlerContext
{
    /// <summary>Null until the user decides; true when confirmed (Y/Enter), false when cancelled (N/Esc).</summary>
    public bool? Result { get; set; }
}
