namespace AudioAnalyzer.Platform.macOS.Audio.CoreAudioTap;

/// <summary>Reports whether Core Audio process tap capture can run on this host.</summary>
public static class MacOsCoreAudioTapAvailability
{
    /// <summary>True on macOS 14.2+ (tap APIs exist); the device row may still require a built shim to capture.</summary>
    public static bool IsOperatingSystemSupported =>
        OperatingSystem.IsMacOS() && OperatingSystem.IsMacOSVersionAtLeast(14, 2);

    /// <summary>True when <c>libaudio_tap_shim.dylib</c> loaded and <c>audio_tap_is_supported</c> succeeds.</summary>
    public static bool IsCaptureReady
    {
        get
        {
            if (!IsOperatingSystemSupported)
            {
                return false;
            }

            if (!MacOsAudioTapShimNative.IsLibraryLoaded)
            {
                try
                {
                    _ = MacOsAudioTapShimNative.AudioTapIsSupported();
                }
                catch (DllNotFoundException)
                {
                    return false;
                }
            }

            return MacOsAudioTapShimNative.IsLibraryLoaded && MacOsAudioTapShimNative.AudioTapIsSupported() != 0;
        }
    }

    /// <summary>Alias for <see cref="IsCaptureReady"/> (device row is listed when the OS supports taps).</summary>
    public static bool IsAvailable => IsOperatingSystemSupported;
}
