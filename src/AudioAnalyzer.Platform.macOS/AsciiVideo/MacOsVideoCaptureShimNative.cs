using System.Runtime.InteropServices;

namespace AudioAnalyzer.Platform.macOS.AsciiVideo;

/// <summary>P/Invoke for <c>libvideo_capture_shim.dylib</c> (AVFoundation webcam capture).</summary>
internal static class MacOsVideoCaptureShimNative
{
    private const string LibraryName = "video_capture_shim";

    static MacOsVideoCaptureShimNative()
    {
        MacOsNativeLibraryResolver.AddProbe(ResolveLibrary);
    }

    /// <summary>True once the dylib has been located and loaded by the resolver.</summary>
    internal static bool IsLibraryLoaded { get; private set; }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void VideoCaptureFrameCallback(
        IntPtr userData,
        IntPtr bgraBase,
        int width,
        int height,
        int bytesPerRow);

    [StructLayout(LayoutKind.Sequential)]
    internal struct VideoCaptureConfig
    {
        public int DeviceIndex;
        public int MaxWidth;
        public int MaxHeight;
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
        yield return Path.Combine(baseDir, "libvideo_capture_shim.dylib");

        // .app hosts: managed files are under Contents/MonoBundle; the shim is copied beside the apphost in Contents/MacOS.
        yield return Path.GetFullPath(Path.Combine(baseDir, "..", "MacOS", "libvideo_capture_shim.dylib"));

        string? processDir = Path.GetDirectoryName(Environment.ProcessPath);
        if (!string.IsNullOrEmpty(processDir))
        {
            yield return Path.Combine(processDir, "libvideo_capture_shim.dylib");
        }

        yield return Path.Combine(baseDir, "runtimes", "osx-arm64", "native", "libvideo_capture_shim.dylib");
        yield return Path.Combine(baseDir, "runtimes", "osx-x64", "native", "libvideo_capture_shim.dylib");
    }

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "video_capture_is_supported")]
    internal static extern int VideoCaptureIsSupported();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "video_capture_device_count")]
    internal static extern int VideoCaptureDeviceCount();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "video_capture_device_name")]
    internal static extern int VideoCaptureDeviceName(int index, IntPtr nameOut, UIntPtr nameOutSize);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "video_capture_start")]
    internal static extern int VideoCaptureStart(
        ref VideoCaptureConfig config,
        VideoCaptureFrameCallback callback,
        IntPtr userData,
        IntPtr errorOut,
        UIntPtr errorOutSize);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "video_capture_stop")]
    internal static extern void VideoCaptureStop();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "video_capture_is_running")]
    internal static extern int VideoCaptureIsRunning();
}
