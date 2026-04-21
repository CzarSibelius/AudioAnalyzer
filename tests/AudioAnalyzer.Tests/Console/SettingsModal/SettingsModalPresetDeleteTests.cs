using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Console;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;
using Xunit;

namespace AudioAnalyzer.Tests.Console.SettingsModal;

/// <summary>Tests for preset delete from the S modal when the Preset row is selected (<see cref="SettingsModalKeyHandlerConfig"/>).</summary>
public sealed class SettingsModalPresetDeleteTests
{
    private sealed class EmptyPaletteRepository : IPaletteRepository
    {
        public IReadOnlyList<PaletteInfo> GetAll() => [];

        public PaletteDefinition? GetById(string id) => null;
    }

    private sealed class StubCharsetRepo : ICharsetRepository
    {
        public IReadOnlyList<CharsetInfo> GetAll() => [];

        public CharsetDefinition? GetById(string id) => null;

        public void Save(string id, CharsetDefinition definition)
        {
        }

        public string Create(CharsetDefinition definition) => "charset-1";
    }

    private sealed class StubDeviceCatalog : IAsciiVideoDeviceCatalog
    {
        public IReadOnlyList<AsciiVideoDeviceEntry> GetDevices() => [];
    }

    private sealed class FakePresetRepository : IPresetRepository
    {
        private readonly Dictionary<string, Preset> _presets = new(StringComparer.OrdinalIgnoreCase);

        public List<string> DeletedIds { get; } = [];

        public void Put(string id, Preset preset) => _presets[id] = preset;

        public IReadOnlyList<PresetInfo> GetAll() =>
            _presets.Values
                .Select(p => new PresetInfo(p.Id, p.Name))
                .OrderBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();

        public Preset? GetById(string id) =>
            _presets.TryGetValue(id, out Preset? p) ? p : null;

        public void Save(string id, Preset preset) => _presets[id] = preset;

        public string Create(Preset preset)
        {
            string id = "created-" + _presets.Count;
            _presets[id] = preset;
            return id;
        }

        public void Delete(string id)
        {
            if (_presets.Remove(id))
            {
                DeletedIds.Add(id);
            }
        }
    }

    private sealed class NoOpLayerStateStore : ITextLayerStateStore
    {
        public void EnsureCapacity(int layerCount)
        {
        }

        public void ClearState(int layerIndex)
        {
        }

        public void RemoveSlotAt(int sortedLayerIndex)
        {
        }

        public void ApplySlotPermutation(IReadOnlyList<int> oldIndexByNewSlot)
        {
        }

        public void ClearAllSlots()
        {
        }
    }

    private static SettingsModalKeyHandlerConfig CreateHandler() =>
        new(new EmptyPaletteRepository(), new StubDeviceCatalog(), new StubCharsetRepo());

    private static TextLayersVisualizerSettings ConfigWithLayerCount(int count)
    {
        var layers = new List<TextLayerSettings>();
        for (int i = 0; i < count; i++)
        {
            layers.Add(
                new TextLayerSettings
                {
                    LayerType = TextLayerType.StaticText,
                    ZOrder = i,
                    Enabled = true
                });
        }

        return new TextLayersVisualizerSettings { Layers = layers, PaletteId = "p-" + count };
    }

    [Fact]
    public void Delete_on_preset_row_with_two_presets_switches_to_successor_and_removes_active_file()
    {
        var repo = new FakePresetRepository();
        var presetA = new Preset { Id = "id-a", Name = "Alpha", Config = ConfigWithLayerCount(2) };
        var presetB = new Preset { Id = "id-b", Name = "Beta", Config = ConfigWithLayerCount(5) };
        repo.Put("id-a", presetA);
        repo.Put("id-b", presetB);

        var vs = new VisualizerSettings
        {
            ActivePresetId = "id-a",
            Presets = [presetA, presetB],
            TextLayers = presetA.Config.DeepCopy()
        };

        int notifyCount = 0;
        var context = new SettingsModalKeyContext
        {
            State = new SettingsModalState { Focus = SettingsModalFocus.LayerList, LeftPanelPresetSelected = true },
            SortedLayers = (vs.TextLayers!.Layers ?? []).OrderBy(l => l.ZOrder).ToList(),
            TextLayers = vs.TextLayers,
            VisualizerSettings = vs,
            PresetRepository = repo,
            SaveSettings = () => { },
            DefaultTextLayersFactory = new DefaultTextLayersSettingsFactory(),
            LayerStateStore = new NoOpLayerStateStore(),
            NotifyLayersStructureChanged = () => notifyCount++
        };

        var handler = CreateHandler();
        bool handled = handler.Handle(new ConsoleKeyInfo('\0', ConsoleKey.Delete, false, false, false), context);

        Assert.False(handled);
        Assert.Single(repo.DeletedIds);
        Assert.Equal("id-a", repo.DeletedIds[0]);
        Assert.Equal("id-b", vs.ActivePresetId);
        Assert.Equal(5, vs.TextLayers!.Layers?.Count ?? 0);
        Assert.Equal("p-5", vs.TextLayers.PaletteId);
        Assert.Equal(1, notifyCount);
        Assert.Single(vs.Presets);
    }

    [Fact]
    public void Delete_on_preset_row_with_one_preset_is_no_op()
    {
        var repo = new FakePresetRepository();
        var presetA = new Preset { Id = "only", Name = "Solo", Config = ConfigWithLayerCount(1) };
        repo.Put("only", presetA);

        var vs = new VisualizerSettings
        {
            ActivePresetId = "only",
            Presets = [presetA],
            TextLayers = presetA.Config.DeepCopy()
        };

        var context = new SettingsModalKeyContext
        {
            State = new SettingsModalState { Focus = SettingsModalFocus.LayerList, LeftPanelPresetSelected = true },
            SortedLayers = (vs.TextLayers!.Layers ?? []).OrderBy(l => l.ZOrder).ToList(),
            TextLayers = vs.TextLayers,
            VisualizerSettings = vs,
            PresetRepository = repo,
            SaveSettings = () => { },
            DefaultTextLayersFactory = new DefaultTextLayersSettingsFactory(),
            LayerStateStore = new NoOpLayerStateStore(),
            NotifyLayersStructureChanged = () => { }
        };

        var handler = CreateHandler();
        handler.Handle(new ConsoleKeyInfo('\0', ConsoleKey.Delete, false, false, false), context);

        Assert.Empty(repo.DeletedIds);
        Assert.Equal("only", vs.ActivePresetId);
        Assert.Single(vs.TextLayers!.Layers!);
    }

    [Fact]
    public void Delete_on_layer_row_still_removes_layer()
    {
        var repo = new FakePresetRepository();
        var presetA = new Preset { Id = "id-a", Name = "Alpha", Config = ConfigWithLayerCount(3) };
        var presetB = new Preset { Id = "id-b", Name = "Beta", Config = ConfigWithLayerCount(1) };
        repo.Put("id-a", presetA);
        repo.Put("id-b", presetB);

        var vs = new VisualizerSettings
        {
            ActivePresetId = "id-a",
            Presets = [presetA, presetB],
            TextLayers = presetA.Config.DeepCopy()
        };

        var context = new SettingsModalKeyContext
        {
            State = new SettingsModalState
            {
                Focus = SettingsModalFocus.LayerList,
                LeftPanelPresetSelected = false,
                SelectedLayerIndex = 1
            },
            SortedLayers = (vs.TextLayers!.Layers ?? []).OrderBy(l => l.ZOrder).ToList(),
            TextLayers = vs.TextLayers,
            VisualizerSettings = vs,
            PresetRepository = repo,
            SaveSettings = () => { },
            DefaultTextLayersFactory = new DefaultTextLayersSettingsFactory(),
            LayerStateStore = new NoOpLayerStateStore(),
            NotifyLayersStructureChanged = () => { }
        };

        var handler = CreateHandler();
        handler.Handle(new ConsoleKeyInfo('\0', ConsoleKey.Delete, false, false, false), context);

        Assert.Empty(repo.DeletedIds);
        Assert.Equal(2, vs.TextLayers.Layers?.Count ?? 0);
        Assert.Equal("id-a", vs.ActivePresetId);
    }
}
