using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Console.KeyHandling;

/// <summary>
/// Detects Shift+V vs plain V for Preset editor preset cycling. Some Windows terminals omit <see cref="ConsoleModifiers.Shift"/> in
/// <see cref="ConsoleKeyInfo.Modifiers"/> for Shift+letter; we infer Shift+V when <see cref="ConsoleKeyInfo.KeyChar"/> is
/// uppercase 'V' while CapsLock is off. The CapsLock state is supplied by an injected <see cref="ICapsLockState"/> so this
/// type does not branch on the operating system.
/// </summary>
internal sealed class ConsoleShiftLetterV
{
    private readonly ICapsLockState _capsLockState;

    /// <summary>Initializes a new instance of the <see cref="ConsoleShiftLetterV"/> class.</summary>
    public ConsoleShiftLetterV(ICapsLockState capsLockState)
    {
        _capsLockState = capsLockState ?? throw new ArgumentNullException(nameof(capsLockState));
    }

    /// <summary>True when the key should be treated as Shift+V (previous preset), not plain V.</summary>
    public bool IsShiftVChord(ConsoleKeyInfo key)
    {
        if (key.Key != ConsoleKey.V)
        {
            return false;
        }

        if ((key.Modifiers & (ConsoleModifiers.Control | ConsoleModifiers.Alt)) != 0)
        {
            return false;
        }

        if (key.Modifiers.HasFlag(ConsoleModifiers.Shift))
        {
            return true;
        }

        // Caps Lock is only reported on Windows; elsewhere ICapsLockState reports false and we
        // rely on explicit Shift in the modifiers only.
        return key.KeyChar == 'V' && !_capsLockState.IsCapsLockOn;
    }

    /// <summary>True for plain V (next preset in Preset editor); false when <see cref="IsShiftVChord"/> is true.</summary>
    public bool IsPlainPresetVChord(ConsoleKeyInfo key)
    {
        if (key.Key != ConsoleKey.V)
        {
            return false;
        }

        if ((key.Modifiers & (ConsoleModifiers.Control | ConsoleModifiers.Alt)) != 0)
        {
            return false;
        }

        return !IsShiftVChord(key);
    }
}
