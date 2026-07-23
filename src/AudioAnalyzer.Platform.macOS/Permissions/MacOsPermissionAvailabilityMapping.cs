using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Platform.macOS.Permissions;

/// <summary>
/// Pure mappings from macOS permission-status integers to <see cref="FeatureAvailability"/> plus a
/// short detail hint. Kept free of native calls so the mapping can be unit tested directly.
/// </summary>
internal static class MacOsPermissionAvailabilityMapping
{
    /// <summary>
    /// Maps an <c>AVAuthorizationStatus</c> value (0 NotDetermined, 1 Restricted, 2 Denied,
    /// 3 Authorized; any other value treated as unknown) to availability + detail.
    /// </summary>
    /// <param name="status">Raw AVAuthorizationStatus integer from the shim.</param>
    /// <param name="permissionLabel">Human label (e.g. "Camera") for the detail hint.</param>
    public static (FeatureAvailability Availability, string Detail) FromAvAuthorizationStatus(
        int status,
        string permissionLabel) =>
        status switch
        {
            3 => (FeatureAvailability.Available, ""),
            0 => (FeatureAvailability.Unavailable, permissionLabel + " not yet requested"),
            2 => (FeatureAvailability.Unavailable, permissionLabel + " denied"),
            1 => (FeatureAvailability.Unavailable, permissionLabel + " restricted"),
            _ => (FeatureAvailability.Unavailable, permissionLabel + " status unavailable")
        };

    /// <summary>
    /// Maps the <c>audio_tap_permission_status</c> result (1 authorized, 0 not authorized,
    /// any other value unknown) to availability + detail.
    /// </summary>
    /// <param name="status">Raw preflight integer from the audio tap shim.</param>
    public static (FeatureAvailability Availability, string Detail) FromSystemAudioPreflight(int status) =>
        status switch
        {
            1 => (FeatureAvailability.Available, ""),
            0 => (FeatureAvailability.Unavailable, "System Audio Recording not granted"),
            _ => (FeatureAvailability.Unavailable, "System Audio Recording status unavailable")
        };
}
