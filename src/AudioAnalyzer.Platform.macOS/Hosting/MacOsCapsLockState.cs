using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Platform.macOS.Hosting;

/// <summary>macOS terminals expose no Caps Lock query; always reports false.</summary>
public sealed class MacOsCapsLockState : ICapsLockState
{
    /// <inheritdoc />
    public bool IsCapsLockOn => false;
}
