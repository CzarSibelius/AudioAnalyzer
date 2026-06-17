namespace AudioAnalyzer.Console;

/// <summary>macOS launch checks for interactive console use.</summary>
internal static class MacOsLaunchDiagnostics
{
    internal static void ReportTerminalRequiredAndExit()
    {
        const string message =
            "AudioAnalyzer needs an interactive terminal for keyboard input.\n" +
            "From the repo root in Terminal.app, use:\n" +
            "  dotnet run --project src/AudioAnalyzer.Console/AudioAnalyzer.Console.csproj -f net10.0-macos26.0";

        try
        {
            System.Console.Error.WriteLine(message);
        }
        catch (Exception ex)
        {
            _ = ex; /* Console unavailable: exit after best-effort stderr */
        }

        Environment.Exit(1);
    }
}
