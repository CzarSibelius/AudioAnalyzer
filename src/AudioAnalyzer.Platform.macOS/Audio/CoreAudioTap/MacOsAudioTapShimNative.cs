using System.Runtime.InteropServices;

namespace AudioAnalyzer.Platform.macOS.Audio.CoreAudioTap;

/// <summary>P/Invoke for <c>libaudio_tap_shim.dylib</c> (Core Audio process taps).</summary>
internal static class MacOsAudioTapShimNative
{
    private const string LibraryName = "audio_tap_shim";

    static MacOsAudioTapShimNative()
    {
        MacOsNativeLibraryResolver.AddProbe(ResolveLibrary);
    }

    internal static bool IsLibraryLoaded { get; private set; }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void AudioTapPcmCallback(
        IntPtr userData,
        IntPtr buffer,
        uint byteCount,
        IntPtr format);

    [StructLayout(LayoutKind.Sequential)]
    internal struct AudioTapConfig
    {
        public int CaptureAllProcesses;
        public IntPtr ProcessIds;
        public int ProcessIdCount;
        public int Mono;
        public int SampleRate;
        public IntPtr DeviceUid;
        public int StreamIndex;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct AudioTapFormat
    {
        public double SampleRate;
        public uint Channels;
        public uint BitsPerSample;
        public uint IsFloat;
    }

    private static IntPtr ResolveLibrary(string libraryName)
    {
        if (!string.Equals(libraryName, LibraryName, StringComparison.OrdinalIgnoreCase))
        {
            return IntPtr.Zero;
        }

        foreach (string candidate in GetLibrarySearchPaths())
        {
            if (File.Exists(candidate) && NativeLibrary.TryLoad(candidate, out IntPtr handle))
            {
                IsLibraryLoaded = true;
                return handle;
            }
        }

        return IntPtr.Zero;
    }

    private static IEnumerable<string> GetLibrarySearchPaths()
    {
        string baseDir = AppContext.BaseDirectory;
        yield return Path.Combine(baseDir, "libaudio_tap_shim.dylib");

        // .app hosts: managed files are under Contents/MonoBundle; the shim is copied beside the apphost in Contents/MacOS.
        yield return Path.GetFullPath(Path.Combine(baseDir, "..", "MacOS", "libaudio_tap_shim.dylib"));

        string? processDir = Path.GetDirectoryName(Environment.ProcessPath);
        if (!string.IsNullOrEmpty(processDir))
        {
            yield return Path.Combine(processDir, "libaudio_tap_shim.dylib");
        }

        yield return Path.Combine(baseDir, "runtimes", "osx-arm64", "native", "libaudio_tap_shim.dylib");
        yield return Path.Combine(baseDir, "runtimes", "osx-x64", "native", "libaudio_tap_shim.dylib");
    }

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "audio_tap_is_supported")]
    internal static extern int AudioTapIsSupported();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "audio_tap_start")]
    internal static extern int AudioTapStart(
        ref AudioTapConfig config,
        AudioTapPcmCallback callback,
        IntPtr userData,
        IntPtr errorOut,
        UIntPtr errorOutSize);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "audio_tap_stop")]
    internal static extern void AudioTapStop();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "audio_tap_is_running")]
    internal static extern int AudioTapIsRunning();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "audio_tap_permission_status")]
    internal static extern int AudioTapPermissionStatus();
}
