namespace AudioAnalyzer.Platform.macOS.AsciiVideo;

/// <summary>Reports whether AVFoundation webcam capture can run on this host.</summary>
public static class MacOsCameraCaptureAvailability
{
    /// <summary>True on macOS (AVFoundation video capture APIs are present); capture also needs the built shim.</summary>
    public static bool IsOperatingSystemSupported => OperatingSystem.IsMacOS();

    /// <summary>True when <c>libvideo_capture_shim.dylib</c> loaded and <c>video_capture_is_supported</c> succeeds.</summary>
    public static bool IsCaptureReady
    {
        get
        {
            if (!IsOperatingSystemSupported)
            {
                return false;
            }

            if (!MacOsVideoCaptureShimNative.IsLibraryLoaded)
            {
                try
                {
                    _ = MacOsVideoCaptureShimNative.VideoCaptureIsSupported();
                }
                catch (DllNotFoundException)
                {
                    return false;
                }
            }

            return MacOsVideoCaptureShimNative.IsLibraryLoaded && MacOsVideoCaptureShimNative.VideoCaptureIsSupported() != 0;
        }
    }
}
