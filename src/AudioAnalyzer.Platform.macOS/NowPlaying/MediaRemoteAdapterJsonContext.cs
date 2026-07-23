using System.Text.Json.Serialization;

namespace AudioAnalyzer.Platform.macOS.NowPlaying;

/// <summary>
/// Source-generated <see cref="JsonSerializerContext"/> for the <c>mediaremote-adapter</c> stream
/// envelope, so deserialization is trim/AOT-safe on the macOS host build (no reflection fallback).
/// </summary>
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(MediaRemoteAdapterMessage))]
internal sealed partial class MediaRemoteAdapterJsonContext : JsonSerializerContext
{
}
