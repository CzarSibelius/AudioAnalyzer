using System.Text.Json;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;
using Xunit;

namespace AudioAnalyzer.Tests.Domain.VisualizerSettings;

/// <summary><see cref="TextLayerSettings.GetCustom{T}"/> must accept string enums and camelCase inside <c>Custom</c> (same expectations as preset JSON).</summary>
public sealed class TextLayerSettingsCustomJsonTests
{
    [Fact]
    public void GetCustom_AsciiVideoSettings_DeserializesStringEnums()
    {
        var layer = new TextLayerSettings();
        layer.Custom = JsonDocument.Parse(
            """{"SourceKind":"Webcam","PaletteSource":"ImageColors","WebcamDeviceIndex":0}""").RootElement;

        var s = layer.GetCustom<AsciiVideoSettings>();

        Assert.NotNull(s);
        Assert.Equal(AsciiVideoSourceKind.Webcam, s!.SourceKind);
        Assert.Equal(AsciiImagePaletteSource.ImageColors, s.PaletteSource);
    }

    [Fact]
    public void GetCustom_AsciiVideoSettings_DeserializesNumericPaletteSource()
    {
        var layer = new TextLayerSettings();
        layer.Custom = JsonDocument.Parse(
            """{"SourceKind":0,"PaletteSource":1,"WebcamDeviceIndex":0}""").RootElement;

        var s = layer.GetCustom<AsciiVideoSettings>();

        Assert.NotNull(s);
        Assert.Equal(AsciiVideoSourceKind.Webcam, s!.SourceKind);
        Assert.Equal(AsciiImagePaletteSource.ImageColors, s.PaletteSource);
    }

    [Fact]
    public void GetCustom_AsciiVideoSettings_DeserializesCamelCasePropertyNames()
    {
        var layer = new TextLayerSettings();
        layer.Custom = JsonDocument.Parse(
            """{"paletteSource":"ImageColors","sourceKind":"Webcam","webcamDeviceIndex":2}""").RootElement;

        var s = layer.GetCustom<AsciiVideoSettings>();

        Assert.NotNull(s);
        Assert.Equal(2, s!.WebcamDeviceIndex);
        Assert.Equal(AsciiImagePaletteSource.ImageColors, s.PaletteSource);
    }

    [Fact]
    public void SetCustom_GetCustom_RoundTripsAsciiVideoSettings()
    {
        var layer = new TextLayerSettings();
        layer.SetCustom(new AsciiVideoSettings
        {
            SourceKind = AsciiVideoSourceKind.Webcam,
            PaletteSource = AsciiImagePaletteSource.ImageColors,
            WebcamDeviceIndex = 1,
            MaxCaptureWidth = 320,
            MaxCaptureHeight = 240,
            FlipHorizontal = true
        });

        var s = layer.GetCustom<AsciiVideoSettings>();

        Assert.NotNull(s);
        Assert.Equal(AsciiVideoSourceKind.Webcam, s!.SourceKind);
        Assert.Equal(AsciiImagePaletteSource.ImageColors, s.PaletteSource);
        Assert.Equal(1, s.WebcamDeviceIndex);
        Assert.Equal(320, s.MaxCaptureWidth);
        Assert.Equal(240, s.MaxCaptureHeight);
        Assert.True(s.FlipHorizontal);
    }
}
