using System.Runtime.InteropServices;

namespace AudioAnalyzer.Platform.macOS.NowPlaying;

/// <summary>libc interop for sending a graceful termination signal to the adapter child process.</summary>
internal static class MacOsNowPlayingNativeMethods
{
    /// <summary>POSIX <c>SIGTERM</c> signal number; the adapter installs a handler that stops its run loop.</summary>
    internal const int Sigterm = 15;

    /// <summary>Sends <paramref name="sig"/> to process <paramref name="pid"/> (libc <c>kill(2)</c>).</summary>
    [DllImport("libc", EntryPoint = "kill", SetLastError = true)]
    internal static extern int Kill(int pid, int sig);
}
