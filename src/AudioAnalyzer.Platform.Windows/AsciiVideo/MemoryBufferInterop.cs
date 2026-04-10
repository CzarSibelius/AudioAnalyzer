using System.Runtime.InteropServices;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace AudioAnalyzer.Platform.Windows.AsciiVideo;

/// <summary>WinRT interop to read <see cref="BitmapBuffer"/> memory as bytes.</summary>
internal static unsafe class MemoryBufferInterop
{
    /// <summary>IID for <c>IMemoryBufferByteAccess</c> (same as <see cref="IMemoryBufferByteAccess"/>).</summary>
    private static readonly Guid s_memoryBufferByteAccessIid = new("5B0D3235-4DBA-4D44-866E-E3ADCA1BB992");

    /// <summary>
    /// Copies BGRA8 pixels from <paramref name="bitmap"/> into <paramref name="destination"/>.
    /// Uses direct buffer memory when <see cref="IMemoryBufferByteAccess"/> is available; otherwise falls back to <see cref="SoftwareBitmap.CopyToBuffer"/> (some webcam drivers fail the COM interop path).
    /// </summary>
    public static void CopyBgra8FromSoftwareBitmap(SoftwareBitmap bitmap, int width, int height, byte[] destination)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        ArgumentNullException.ThrowIfNull(destination);
        int need = width * height * 4;
        if (destination.Length < need)
        {
            throw new ArgumentException("Destination too small.", nameof(destination));
        }

        try
        {
            using BitmapBuffer buffer = bitmap.LockBuffer(BitmapBufferAccessMode.Read);
            CopyBgra8(buffer, width, height, destination);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MemoryBufferInterop: IMemoryBufferByteAccess copy failed, using CopyToBuffer. {ex.Message}");
            CopyBgra8UsingCopyToBuffer(bitmap, destination);
        }
    }

    /// <summary>Copies BGRA8 pixels from a locked bitmap buffer into <paramref name="destination"/> (tight row packing, width*4 per row).</summary>
    public static void CopyBgra8(BitmapBuffer buffer, int width, int height, byte[] destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (destination.Length < width * height * 4)
        {
            throw new ArgumentException("Destination too small.", nameof(destination));
        }

        BitmapPlaneDescription desc = buffer.GetPlaneDescription(0);
        object reference = buffer.CreateReference();
        IntPtr pUnkRef = IntPtr.Zero;
        IntPtr pByteAccess = IntPtr.Zero;
        try
        {
            pUnkRef = Marshal.GetIUnknownForObject(reference);
            Guid iid = s_memoryBufferByteAccessIid;
            int hr = Marshal.QueryInterface(pUnkRef, in iid, out pByteAccess);
            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            var byteAccess = (IMemoryBufferByteAccess)Marshal.GetObjectForIUnknown(pByteAccess);
            byteAccess.GetBuffer(out byte* dataInBytes, out uint _);
            fixed (byte* destPtr = destination)
            {
                int rowBytes = width * 4;
                for (int y = 0; y < height; y++)
                {
                    System.Buffer.MemoryCopy(
                        dataInBytes + (y * desc.Stride),
                        destPtr + (y * rowBytes),
                        rowBytes,
                        rowBytes);
                }
            }
        }
        finally
        {
            if (pByteAccess != IntPtr.Zero)
            {
                Marshal.Release(pByteAccess);
            }

            if (pUnkRef != IntPtr.Zero)
            {
                Marshal.Release(pUnkRef);
            }

            if (reference is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    private static void CopyBgra8UsingCopyToBuffer(SoftwareBitmap bitmap, byte[] destination)
    {
        uint len = (uint)destination.Length;
        var winBuffer = new global::Windows.Storage.Streams.Buffer(len);
        bitmap.CopyToBuffer(winBuffer);
        using DataReader reader = DataReader.FromBuffer(winBuffer);
        reader.ReadBytes(destination);
    }

    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-866E-E3ADCA1BB992")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }
}
