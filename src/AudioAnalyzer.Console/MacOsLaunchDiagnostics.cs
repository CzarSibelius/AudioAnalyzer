using System.Diagnostics;

namespace AudioAnalyzer.Console;

/// <summary>
/// macOS microphone consent is tied to bundle metadata with <c>NSMicrophoneUsageDescription</c>.
/// <c>dotnet run</c> uses the shared <c>dotnet</c> host; the post-build <c>AudioAnalyzer.app</c> wraps the real apphost for the system prompt.
/// </summary>
internal static class MacOsLaunchDiagnostics
{
    internal static void ReportTerminalRequiredAndExit()
    {
        const string message =
            "AudioAnalyzer is a terminal application and needs an interactive shell for keyboard input.\n" +
            "Run it from Terminal.app, for example:\n" +
            "  dotnet run --project src/AudioAnalyzer.Console/AudioAnalyzer.Console.csproj -f net10.0-macos26.0\n" +
            "For the macOS microphone prompt, use the built app bundle from Terminal (not Finder):\n" +
            "  ./src/AudioAnalyzer.Console/bin/Debug/net10.0-macos26.0/osx-arm64/AudioAnalyzer.app/Contents/MacOS/AudioAnalyzer.Console";

        try
        {
            System.Console.Error.WriteLine(message);
        }
        catch (Exception ex)
        {
            _ = ex; /* Console unavailable: best-effort alert below */
        }

        if (OperatingSystem.IsMacOS())
        {
            ShowMacOsAlert(message);
        }

        Environment.Exit(1);
    }

    internal static void WriteMicrophoneBundleHintIfNeeded()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        string? path = Environment.ProcessPath;
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        if (path.Contains(".app/Contents/MacOS/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string? dir = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(dir))
        {
            return;
        }

        string bundledExe = Path.Combine(dir, "AudioAnalyzer.app", "Contents", "MacOS", Path.GetFileName(path));
        if (!File.Exists(bundledExe))
        {
            return;
        }

        System.Console.WriteLine();
        System.Console.WriteLine("macOS: For the system microphone prompt, run this executable inside the built app bundle:");
        System.Console.WriteLine($"  \"{bundledExe}\"");
        System.Console.WriteLine("(Flat `dotnet run` / loose apphost often lacks NSMicrophoneUsageDescription — Core Audio may deny device binding.)");
        System.Console.WriteLine();
    }

    private static void ShowMacOsAlert(string message)
    {
        try
        {
            string escaped = message
                .Replace("\\", " ", StringComparison.Ordinal)
                .Replace("\"", "'", StringComparison.Ordinal)
                .Replace("\n", " ", StringComparison.Ordinal);
            if (escaped.Length > 500)
            {
                escaped = escaped[..500];
            }

            var psi = new ProcessStartInfo
            {
                FileName = "/usr/bin/osascript",
                Arguments = $"-e \"display alert \\\"AudioAnalyzer\\\" message \\\"{escaped}\\\" as warning\"",
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using Process? process = Process.Start(psi);
            process?.WaitForExit(5000);
        }
        catch (Exception ex)
        {
            _ = ex; /* Alert is best-effort when console output is unavailable */
        }
    }
}
