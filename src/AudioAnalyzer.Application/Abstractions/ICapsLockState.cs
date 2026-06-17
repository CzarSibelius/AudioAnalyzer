namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Reports the keyboard Caps Lock state. Implemented per platform (Windows reads the console
/// Caps Lock API; other platforms report <c>false</c>) and injected so shared key-handling code
/// does not branch on the operating system.
/// </summary>
public interface ICapsLockState
{
    /// <summary>True when Caps Lock is on and the platform can report it; otherwise false.</summary>
    bool IsCapsLockOn { get; }
}
