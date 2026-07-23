using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Platform.macOS.NowPlaying;
using Xunit;

namespace AudioAnalyzer.Tests.Platform.macOS.NowPlaying;

public sealed class MediaRemoteAdapterPayloadParserTests
{
    [Fact]
    public void TryParse_EmptyPayload_ReturnsTrueWithNullInfo()
    {
        bool parsed = MediaRemoteAdapterPayloadParser.TryParse(
            """{"type":"data","diff":false,"payload":{}}""",
            out NowPlayingInfo? info);

        Assert.True(parsed);
        Assert.Null(info);
    }

    [Fact]
    public void TryParse_FullPayload_MapsTitleArtistAlbum()
    {
        bool parsed = MediaRemoteAdapterPayloadParser.TryParse(
            """{"type":"data","diff":false,"payload":{"title":"Song","artist":"Band","album":"Record","playing":true}}""",
            out NowPlayingInfo? info);

        Assert.True(parsed);
        Assert.NotNull(info);
        Assert.Equal("Song", info!.Title);
        Assert.Equal("Band", info.Artist);
        Assert.Equal("Record", info.Album);
    }

    [Fact]
    public void TryParse_MissingArtist_MapsWithNullArtist()
    {
        bool parsed = MediaRemoteAdapterPayloadParser.TryParse(
            """{"type":"data","diff":false,"payload":{"title":"Solo","album":"Record"}}""",
            out NowPlayingInfo? info);

        Assert.True(parsed);
        Assert.NotNull(info);
        Assert.Equal("Solo", info!.Title);
        Assert.Null(info.Artist);
        Assert.Equal("Record", info.Album);
    }

    [Fact]
    public void TryParse_WhitespaceFields_TreatedAsAbsent()
    {
        bool parsed = MediaRemoteAdapterPayloadParser.TryParse(
            """{"type":"data","diff":false,"payload":{"title":"  ","artist":""}}""",
            out NowPlayingInfo? info);

        Assert.True(parsed);
        Assert.Null(info);
    }

    [Fact]
    public void TryParse_TrimsValues()
    {
        bool parsed = MediaRemoteAdapterPayloadParser.TryParse(
            """{"type":"data","diff":false,"payload":{"title":"  Padded  "}}""",
            out NowPlayingInfo? info);

        Assert.True(parsed);
        Assert.NotNull(info);
        Assert.Equal("Padded", info!.Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json")]
    [InlineData("{ broken")]
    public void TryParse_NonJsonOrBlank_ReturnsFalse(string line)
    {
        bool parsed = MediaRemoteAdapterPayloadParser.TryParse(line, out NowPlayingInfo? info);

        Assert.False(parsed);
        Assert.Null(info);
    }

    [Fact]
    public void TryParse_NonDataMessageType_ReturnsFalse()
    {
        bool parsed = MediaRemoteAdapterPayloadParser.TryParse(
            """{"type":"error","message":"something"}""",
            out NowPlayingInfo? info);

        Assert.False(parsed);
        Assert.Null(info);
    }

    [Fact]
    public void MapPayload_Null_ReturnsNull()
    {
        Assert.Null(MediaRemoteAdapterPayloadParser.MapPayload(null));
    }
}
