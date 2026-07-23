using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Platform.macOS.Permissions;
using Xunit;

namespace AudioAnalyzer.Tests.Platform.macOS.Permissions;

/// <summary>Tests the macOS permission-status integer to <see cref="FeatureAvailability"/> mappings (ADR-0095).</summary>
public sealed class MacOsPermissionAvailabilityMappingTests
{
    [Theory]
    [InlineData(3, FeatureAvailability.Available)]
    [InlineData(0, FeatureAvailability.Unavailable)]
    [InlineData(1, FeatureAvailability.Unavailable)]
    [InlineData(2, FeatureAvailability.Unavailable)]
    [InlineData(-1, FeatureAvailability.Unavailable)]
    [InlineData(99, FeatureAvailability.Unavailable)]
    public void FromAvAuthorizationStatus_MapsAuthorizationStatus(int status, FeatureAvailability expected)
    {
        var (availability, detail) = MacOsPermissionAvailabilityMapping.FromAvAuthorizationStatus(status, "Camera");

        Assert.Equal(expected, availability);
        if (expected == FeatureAvailability.Available)
        {
            Assert.Equal("", detail);
        }
        else
        {
            Assert.Contains("Camera", detail, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData(1, FeatureAvailability.Available)]
    [InlineData(0, FeatureAvailability.Unavailable)]
    [InlineData(-1, FeatureAvailability.Unavailable)]
    public void FromSystemAudioPreflight_MapsPreflightResult(int status, FeatureAvailability expected)
    {
        var (availability, detail) = MacOsPermissionAvailabilityMapping.FromSystemAudioPreflight(status);

        Assert.Equal(expected, availability);
        if (expected == FeatureAvailability.Unavailable)
        {
            Assert.Contains("System Audio Recording", detail, StringComparison.Ordinal);
        }
    }
}
