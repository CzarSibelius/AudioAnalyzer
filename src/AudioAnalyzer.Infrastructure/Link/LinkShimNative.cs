using System.Reflection;
using System.Runtime.InteropServices;

namespace AudioAnalyzer.Infrastructure.Link;

/// <summary>P/Invoke entry points for <c>link_shim.dll</c> (Ableton Link C wrapper).</summary>
internal static class LinkShimNative
{
    static LinkShimNative()
    {
        NativeLibrary.SetDllImportResolver(typeof(LinkShimNative).Assembly, ResolveLinkShim);
    }

    private static IntPtr ResolveLinkShim(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (!string.Equals(libraryName, "link_shim", StringComparison.OrdinalIgnoreCase))
        {
            return IntPtr.Zero;
        }

        string baseDir = AppContext.BaseDirectory;
        string candidate = Path.Combine(baseDir, "link_shim.dll");
        if (File.Exists(candidate) && NativeLibrary.TryLoad(candidate, out IntPtr h))
        {
            return h;
        }

        return IntPtr.Zero;
    }

    [DllImport("link_shim", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern IntPtr link_shim_create(double initial_bpm);

    [DllImport("link_shim", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void link_shim_destroy(IntPtr handle);

    [DllImport("link_shim", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void link_shim_set_enabled(IntPtr handle, int enabled);

    [DllImport("link_shim", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int link_shim_capture(IntPtr handle, double quantum, out double tempo, out int peers, out double beat);
}
