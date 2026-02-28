using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;

namespace AudioAnalyzer.Console;

/// <summary>Aggregates key handler bindings and returns help sections ordered by application mode. Per ADR-0049.</summary>
internal sealed class HelpContentProvider : IHelpContentProvider
{
    private const string SectionKeyboardControls = "Keyboard controls";
    private const string SectionLayeredText = "Layered text";
    private const string SectionPresetSettingsModal = "Preset settings modal (S)";
    private const string SectionShowEditModal = "Show edit modal (S when in Show play)";
    private const string SectionDeviceSelection = "Device selection";
    private const string SectionGeneral = "General";

    private readonly IKeyHandlerConfig<MainLoopKeyContext> _mainLoopConfig;
    private readonly IKeyHandlerConfig<DeviceSelectionKeyContext> _deviceSelectionConfig;
    private readonly IKeyHandlerConfig<SettingsModalKeyContext> _settingsModalConfig;
    private readonly IKeyHandlerConfig<ShowEditModalKeyContext> _showEditConfig;
    private readonly IKeyHandlerConfig<TextLayersKeyContext> _textLayersConfig;

    public HelpContentProvider(
        IKeyHandlerConfig<MainLoopKeyContext> mainLoopConfig,
        IKeyHandlerConfig<DeviceSelectionKeyContext> deviceSelectionConfig,
        IKeyHandlerConfig<SettingsModalKeyContext> settingsModalConfig,
        IKeyHandlerConfig<ShowEditModalKeyContext> showEditConfig,
        IKeyHandlerConfig<TextLayersKeyContext> textLayersConfig)
    {
        _mainLoopConfig = mainLoopConfig ?? throw new ArgumentNullException(nameof(mainLoopConfig));
        _deviceSelectionConfig = deviceSelectionConfig ?? throw new ArgumentNullException(nameof(deviceSelectionConfig));
        _settingsModalConfig = settingsModalConfig ?? throw new ArgumentNullException(nameof(settingsModalConfig));
        _showEditConfig = showEditConfig ?? throw new ArgumentNullException(nameof(showEditConfig));
        _textLayersConfig = textLayersConfig ?? throw new ArgumentNullException(nameof(textLayersConfig));
    }

    /// <inheritdoc />
    public IReadOnlyList<HelpSection> GetSections(ApplicationMode currentMode)
    {
        var allBindings = new List<KeyBinding>();
        allBindings.AddRange(_mainLoopConfig.GetBindings());
        allBindings.AddRange(_deviceSelectionConfig.GetBindings());
        allBindings.AddRange(_settingsModalConfig.GetBindings());
        allBindings.AddRange(_showEditConfig.GetBindings());
        allBindings.AddRange(_textLayersConfig.GetBindings());

        var bySection = new Dictionary<string, List<KeyBinding>>(StringComparer.Ordinal);
        foreach (var b in allBindings)
        {
            if (b.ApplicableMode is { } mode && mode != currentMode)
            {
                continue;
            }
            var section = string.IsNullOrWhiteSpace(b.Section) ? SectionGeneral : b.Section;
            if (!bySection.TryGetValue(section, out var list))
            {
                list = [];
                bySection[section] = list;
            }
            list.Add(b);
        }

        // Only include the modal section for the current mode (mode-specific help).
        string modalSection = currentMode == ApplicationMode.ShowPlay ? SectionShowEditModal : SectionPresetSettingsModal;
        string[] sectionOrder = [SectionKeyboardControls, SectionLayeredText, modalSection, SectionDeviceSelection, SectionGeneral];

        var result = new List<HelpSection>();
        foreach (var title in sectionOrder)
        {
            if (bySection.TryGetValue(title, out var bindings) && bindings.Count > 0)
            {
                result.Add(new HelpSection(title, bindings));
            }
        }
        return result;
    }
}
