using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Console;
using AudioAnalyzer.Console.KeyHandling;
using AudioAnalyzer.Domain;
using Xunit;

namespace AudioAnalyzer.Tests.Console.KeyHandling;

/// <summary>
/// Tests that the main-loop Escape / Q / Ctrl+Q bindings route through the quit confirmation and only quit on confirm (ADR-0093).
/// </summary>
public sealed class MainLoopKeyHandlerConfigQuitTests
{
    private sealed class StubCapsLock : ICapsLockState
    {
        public bool IsCapsLockOn => false;
    }

    private sealed class StubCanvasLayerStack : IPresetEditorCanvasLayerStackService
    {
        public bool TryInsertLayer(VisualizerSettings visualizerSettings, IVisualizer visualizer) => false;

        public bool TryDeleteActiveLayer(VisualizerSettings visualizerSettings, IVisualizer visualizer) => false;
    }

    private static MainLoopKeyHandlerConfig CreateConfig() =>
        new(new StubCanvasLayerStack(), new ConsoleShiftLetterV(new StubCapsLock()));

    private static MainLoopKeyContext CreateContext(Func<bool> requestQuitConfirmation)
    {
        return new MainLoopKeyContext
        {
            // Members exercised by the quit bindings:
            RequestQuitConfirmation = requestQuitConfirmation,
            GetApplicationMode = () => ApplicationMode.PresetEditor,
            AppSettings = new AppSettings(),
            VisualizerSettings = new VisualizerSettings(),
            // Unused by the quit bindings under test; intentionally left unset:
            DisplayState = null!,
            Orchestrator = null!,
            SetModalOpen = _ => { },
            ConsoleLock = new object(),
            RefreshHeaderAndRedraw = () => { },
            SaveSettings = () => { },
            SaveVisualizerSettings = () => { },
            GetDeviceName = () => "",
            Engine = null!,
            HeaderContainer = null!,
            OnModeSwitch = () => { },
            OnPresetCycle = () => { },
            OnPresetCyclePrevious = () => { },
            SettingsModal = null!,
            ShowEditModal = null!,
            StopCapture = () => { },
            ReleaseCaptureForDeviceSelection = () => { },
            StartCapture = (_, _) => { },
            RestartCapture = () => { },
            DeviceSelectionModal = null!,
            HelpModal = null!,
            OnPaletteCycle = () => { },
            DumpScreen = () => null,
            PerformFullLayerRuntimeReset = () => { },
            LayerPickerModal = null!,
            VisualizationRenderer = null!,
            Visualizer = null!
        };
    }

    private static ConsoleKeyInfo Key(ConsoleKey key, bool control = false) =>
        new('\0', key, shift: false, alt: false, control: control);

    [Theory]
    [InlineData(ConsoleKey.Escape, false)]
    [InlineData(ConsoleKey.Q, false)]
    [InlineData(ConsoleKey.Q, true)]
    public void QuitKeys_RequestConfirmation_AndQuitWhenConfirmed(ConsoleKey key, bool control)
    {
        int requestCount = 0;
        var config = CreateConfig();
        var ctx = CreateContext(() => { requestCount++; return true; });

        bool handled = config.Handle(Key(key, control), ctx);

        Assert.True(handled);
        Assert.Equal(1, requestCount);
        Assert.True(ctx.ShouldQuit);
    }

    [Theory]
    [InlineData(ConsoleKey.Escape, false)]
    [InlineData(ConsoleKey.Q, false)]
    [InlineData(ConsoleKey.Q, true)]
    public void QuitKeys_DoNotQuit_WhenConfirmationCancelled(ConsoleKey key, bool control)
    {
        int requestCount = 0;
        var config = CreateConfig();
        var ctx = CreateContext(() => { requestCount++; return false; });

        bool handled = config.Handle(Key(key, control), ctx);

        Assert.True(handled);
        Assert.Equal(1, requestCount);
        Assert.False(ctx.ShouldQuit);
    }

    [Fact]
    public void Bindings_AdvertiseQuitWithConfirmation()
    {
        var config = CreateConfig();

        var bindings = config.GetBindings();

        Assert.Contains(bindings, b => b.Key == "Q / Ctrl+Q" && b.Description.Contains("Quit", StringComparison.Ordinal));
        Assert.Contains(bindings, b => b.Key == "Esc" && b.Description.Contains("Quit", StringComparison.Ordinal));
    }
}
