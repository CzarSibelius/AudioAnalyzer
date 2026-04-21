namespace AudioAnalyzer.Console.KeyHandling;

/// <summary>
/// Detects Shift+V vs plain V for Preset editor preset cycling. Some Windows terminals omit <see cref="ConsoleModifiers.Shift"/> in
/// <see cref="ConsoleKeyInfo.Modifiers"/> for Shift+letter; we infer Shift+V when <see cref="ConsoleKeyInfo.KeyChar"/> is
/// uppercase 'V' while CapsLock is off.
/// </summary>
internal static class ConsoleShiftLetterV
{
    /// <summary>True when the key should be treated as Shift+V (previous preset), not plain V.</summary>
    public static bool IsShiftVChord(ConsoleKeyInfo key)
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

        return key.KeyChar == 'V' && !global::System.Console.CapsLock;
    }

    /// <summary>True for plain V (next preset in Preset editor); false when <see cref="IsShiftVChord"/> is true.</summary>
    public static bool IsPlainPresetVChord(ConsoleKeyInfo key)
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
