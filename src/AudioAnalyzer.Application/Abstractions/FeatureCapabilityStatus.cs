namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Immutable status of one shim/permission-dependent (or core) feature: a stable id, display name,
/// availability, a short reason/hint, and a category. Produced by <see cref="IFeatureCapabilityProbe"/>
/// instances and aggregated by <see cref="IFeatureCapabilityReport"/>.
/// </summary>
/// <param name="Id">Stable key (e.g. <c>ableton-link</c>, <c>system-audio-tap</c>).</param>
/// <param name="Name">Human-readable display name.</param>
/// <param name="Availability">Whether the feature is functional on this host.</param>
/// <param name="Detail">Short reason or hint (e.g. "no native link_shim.dll"); may be empty.</param>
/// <param name="Category">Broad grouping for the capability.</param>
public sealed record FeatureCapabilityStatus(
    string Id,
    string Name,
    FeatureAvailability Availability,
    string Detail,
    FeatureCapabilityCategory Category);
