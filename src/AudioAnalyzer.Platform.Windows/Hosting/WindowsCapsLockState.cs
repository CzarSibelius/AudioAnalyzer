using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Platform.Windows.Hosting;

/// <summary>Reports Caps Lock state via the Windows console API.</summary>
public sealed class WindowsCapsLockState : ICapsLockState
{
    /// <inheritdoc />
    public bool IsCapsLockOn => System.Console.CapsLock;
}
