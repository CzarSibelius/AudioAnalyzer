namespace AudioAnalyzer.Console;

/// <summary>Safe probes and reads for interactive console key input.</summary>
internal static class InteractiveConsoleInput
{
    private static int _isSupportedState = -1;

    /// <summary>Whether <see cref="System.Console.KeyAvailable"/> can be used without throwing.</summary>
    internal static bool IsSupported
    {
        get
        {
            if (_isSupportedState >= 0)
            {
                return _isSupportedState == 1;
            }

            bool supported = ProbeSupported();
            _isSupportedState = supported ? 1 : 0;
            return supported;
        }
    }

    /// <summary>Returns whether a keypress is waiting, or false when console input is unavailable.</summary>
    internal static bool KeyAvailable => IsSupported && System.Console.KeyAvailable;

    /// <summary>Reads a key when <see cref="KeyAvailable"/> is true.</summary>
    internal static ConsoleKeyInfo ReadKey(bool intercept) => System.Console.ReadKey(intercept);

    private static bool ProbeSupported()
    {
        try
        {
            _ = System.Console.KeyAvailable;
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
