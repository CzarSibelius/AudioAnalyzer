using System.Runtime.InteropServices;
using System.Text;

namespace AudioAnalyzer.Platform.macOS.Audio.CoreAudio;

internal static class MacOsCfInterop
{
    internal const uint kCFStringEncodingUTF8 = 0x08000100;

    private const string CoreFoundation =
        "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    [DllImport(CoreFoundation)]
    private static extern IntPtr CFStringCreateWithCString(IntPtr allocator, IntPtr cString, uint encoding);

    [DllImport(CoreFoundation)]
    internal static extern void CFRelease(IntPtr cf);

    [DllImport(CoreFoundation)]
    private static extern nuint CFStringGetLength(IntPtr theString);

    [DllImport(CoreFoundation)]
    private static extern nuint CFStringGetMaximumSizeForEncoding(nuint length, uint encoding);

    [DllImport(CoreFoundation)]
    private static extern byte CFStringGetCString(IntPtr theString, IntPtr buffer, nuint bufferSize, uint encoding);

    internal static IntPtr CreateCfStringUtf8(string value)
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(value);
        byte[] z = new byte[utf8.Length + 1];
        utf8.AsSpan().CopyTo(z);

        IntPtr nativeUtf8 = Marshal.AllocHGlobal(z.Length);
        try
        {
            Marshal.Copy(z, 0, nativeUtf8, z.Length);
            return CFStringCreateWithCString(IntPtr.Zero, nativeUtf8, kCFStringEncodingUTF8);
        }
        finally
        {
            Marshal.FreeHGlobal(nativeUtf8);
        }
    }

    internal static string CfStringToUtf8(IntPtr cfString)
    {
        if (cfString == IntPtr.Zero)
        {
            return string.Empty;
        }

        nuint len = CFStringGetLength(cfString);
        nuint maxSize = CFStringGetMaximumSizeForEncoding(len, kCFStringEncodingUTF8);
        if (maxSize == 0)
        {
            return string.Empty;
        }

        IntPtr buffer = Marshal.AllocHGlobal((int)(maxSize + 1));
        try
        {
            if (CFStringGetCString(cfString, buffer, maxSize + 1, kCFStringEncodingUTF8) == 0)
            {
                return string.Empty;
            }

            return Marshal.PtrToStringUTF8(buffer) ?? string.Empty;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }
}
