using System.Reflection;
using System.Runtime.InteropServices;

namespace AudioAnalyzer.Platform.macOS;

/// <summary>
/// Coordinates a single per-assembly <see cref="NativeLibrary.SetDllImportResolver"/> registration.
/// .NET allows only one resolver per assembly, so each native shim contributes its own probe here.
/// </summary>
internal static class MacOsNativeLibraryResolver
{
    private static readonly object s_gate = new();
    private static readonly List<Func<string, IntPtr>> s_probes = new();
    private static bool s_registered;

    /// <summary>Registers a probe that returns a loaded library handle for its name, or <see cref="IntPtr.Zero"/>.</summary>
    internal static void AddProbe(Func<string, IntPtr> probe)
    {
        ArgumentNullException.ThrowIfNull(probe);
        lock (s_gate)
        {
            s_probes.Add(probe);
            if (!s_registered)
            {
                NativeLibrary.SetDllImportResolver(typeof(MacOsNativeLibraryResolver).Assembly, Resolve);
                s_registered = true;
            }
        }
    }

    private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        Func<string, IntPtr>[] probes;
        lock (s_gate)
        {
            probes = s_probes.ToArray();
        }

        foreach (Func<string, IntPtr> probe in probes)
        {
            IntPtr handle = probe(libraryName);
            if (handle != IntPtr.Zero)
            {
                return handle;
            }
        }

        return IntPtr.Zero;
    }
}
