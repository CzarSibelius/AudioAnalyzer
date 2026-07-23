namespace AudioAnalyzer.Application.Abstractions;

/// <summary>Whether a reported feature capability is functional on the current host.</summary>
public enum FeatureAvailability
{
    /// <summary>The feature is present and functional.</summary>
    Available,

    /// <summary>The feature is relevant on this host but not currently functional (missing shim, ungranted permission, etc.).</summary>
    Unavailable,

    /// <summary>The feature is not relevant on this host or operating system.</summary>
    NotApplicable
}
