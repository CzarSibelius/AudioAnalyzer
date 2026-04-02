using System.Text;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Renders the General Settings hub menu and optional inline text edit line.</summary>
internal sealed class GeneralSettingsHubAreaRenderer : IUiComponentRenderer<GeneralSettingsHubAreaComponent>
{
    private readonly GeneralSettingsHubState _state;
    private readonly UiSettings _uiSettings;
    private readonly AppSettings _appSettings;
    private readonly IPaletteRepository _paletteRepo;
    private readonly IUiComponentRenderer<HorizontalRowComponent> _horizontalRowRenderer;
    private readonly HorizontalRowComponent _audioMenuRow = new();
    private readonly HorizontalRowComponent _bpmSourceMenuRow = new();
    private readonly HorizontalRowComponent _appNameMenuRow = new();
    private readonly HorizontalRowComponent _defaultAssetMenuRow = new();
    private readonly HorizontalRowComponent _themeMenuRow = new();
    private readonly HorizontalRowComponent _showRenderFpsMenuRow = new();
    private bool _regionCleared;

    public GeneralSettingsHubAreaRenderer(
        GeneralSettingsHubState state,
        UiSettings uiSettings,
        AppSettings appSettings,
        IPaletteRepository paletteRepo,
        IUiComponentRenderer<HorizontalRowComponent> horizontalRowRenderer)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        _paletteRepo = paletteRepo ?? throw new ArgumentNullException(nameof(paletteRepo));
        _horizontalRowRenderer = horizontalRowRenderer ?? throw new ArgumentNullException(nameof(horizontalRowRenderer));
    }

    /// <inheritdoc />
    public ComponentRenderResult Render(GeneralSettingsHubAreaComponent component, RenderContext context)
    {
        if (context.Snapshot == null)
        {
            return ComponentRenderResult.Written(0);
        }

        int startRow = context.StartRow;
        int maxLines = context.MaxLines;
        int width = context.Width;
        if (maxLines <= 0 || width <= 0)
        {
            return ComponentRenderResult.Written(0);
        }

        if (!_regionCleared)
        {
            ClearRegion(startRow, maxLines, width);
            _regionCleared = true;
        }

        var palette = context.Palette;
        var snapshot = context.Snapshot;
        IReadOnlyList<PaletteColor> beatColors =
            GeneralSettingsHubMenuLines.ResolveHubBeatPaletteColors(_uiSettings, _paletteRepo, palette);
        string appDisplay = EffectiveAppName.FromUiSettings(_uiSettings);
        string themeDisplay = GeneralSettingsHubMenuLines.ResolveUiThemeDisplaySummary(_uiSettings, _paletteRepo);

        WriteRaw(0, startRow, width, sb =>
        {
            int phase = PaletteSwatchFormatter.ComputeToolbarPhaseOffset(snapshot, beatColors.Count);
            sb.Append(PaletteSwatchFormatter.FormatPaletteColoredName("General settings", beatColors, phase));
        });

        _audioMenuRow.SetRowData(
            [
                new LabeledValueDescriptor(
                    "",
                    () => new AnsiText(GeneralSettingsHubMenuLines.FormatAudioLine(
                        _state,
                        palette,
                        snapshot,
                        beatColors,
                        context.DeviceName)),
                    preformattedAnsi: true)
            ],
            [width]);
        _bpmSourceMenuRow.SetRowData(
            [
                new LabeledValueDescriptor(
                    "",
                    () => new AnsiText(GeneralSettingsHubMenuLines.FormatBpmSourceLine(
                        _state,
                        palette,
                        snapshot,
                        beatColors,
                        _appSettings.BpmSource)),
                    preformattedAnsi: true)
            ],
            [width]);
        _appNameMenuRow.SetRowData(
            [
                new LabeledValueDescriptor(
                    "",
                    () => new AnsiText(GeneralSettingsHubMenuLines.FormatApplicationNameLine(
                        _state,
                        palette,
                        snapshot,
                        beatColors,
                        appDisplay)),
                    preformattedAnsi: true)
            ],
            [width]);
        _defaultAssetMenuRow.SetRowData(
            [
                new LabeledValueDescriptor(
                    "",
                    () => new AnsiText(GeneralSettingsHubMenuLines.FormatDefaultAssetFolderLine(
                        _state,
                        palette,
                        snapshot,
                        beatColors,
                        _uiSettings)),
                    preformattedAnsi: true)
            ],
            [width]);
        _themeMenuRow.SetRowData(
            [
                new LabeledValueDescriptor(
                    "",
                    () => new AnsiText(GeneralSettingsHubMenuLines.FormatUiThemeLine(
                        _state,
                        palette,
                        snapshot,
                        beatColors,
                        themeDisplay)),
                    preformattedAnsi: true)
            ],
            [width]);

        var rowContext = new RenderContext
        {
            Width = width,
            StartRow = startRow + 2,
            MaxLines = 1,
            Palette = palette,
            ScrollSpeed = context.ScrollSpeed,
            Snapshot = context.Snapshot,
            DeviceName = context.DeviceName,
            PaletteDisplayName = context.PaletteDisplayName,
            InvalidateWriteCache = context.InvalidateWriteCache
        };
        _horizontalRowRenderer.Render(_audioMenuRow, rowContext);
        rowContext.StartRow = startRow + 3;
        _horizontalRowRenderer.Render(_bpmSourceMenuRow, rowContext);
        rowContext.StartRow = startRow + 4;
        _horizontalRowRenderer.Render(_appNameMenuRow, rowContext);
        rowContext.StartRow = startRow + 5;
        _horizontalRowRenderer.Render(_defaultAssetMenuRow, rowContext);
        rowContext.StartRow = startRow + 6;
        _horizontalRowRenderer.Render(_themeMenuRow, rowContext);
        _showRenderFpsMenuRow.SetRowData(
            [
                new LabeledValueDescriptor(
                    "",
                    () => new AnsiText(GeneralSettingsHubMenuLines.FormatShowRenderFpsLine(
                        _state,
                        palette,
                        snapshot,
                        beatColors,
                        _uiSettings)),
                    preformattedAnsi: true)
            ],
            [width]);
        rowContext.StartRow = startRow + 7;
        _horizontalRowRenderer.Render(_showRenderFpsMenuRow, rowContext);

        if (_state.EditMode != GeneralSettingsHubEditMode.None)
        {
            WriteRaw(9, startRow, width, sb =>
            {
                AnsiConsole.AppendColored(sb, "  Edit: ", palette.Label);
                AnsiConsole.AppendColored(sb, _state.RenameBuffer, palette.Highlighted);
            });
        }

        return ComponentRenderResult.Written(maxLines);
    }

    /// <inheritdoc />
    public void ResetVisualizerAreaCleared()
    {
        _regionCleared = false;
    }

    private static void WriteRaw(int lineIndex, int startRow, int width, Action<StringBuilder> build)
    {
        if (lineIndex < 0)
        {
            return;
        }

        try
        {
            System.Console.SetCursorPosition(0, startRow + lineIndex);
            var sb = new StringBuilder();
            build(sb);
            string ansi = sb.ToString();
            int w = AnsiConsole.GetDisplayWidth(ansi);
            if (w > width)
            {
                System.Console.Write(StaticTextViewport.TruncateToWidth(new AnsiText(ansi), width));
            }
            else
            {
                System.Console.Write(ansi);
            }
        }
        catch (Exception ex)
        {
            _ = ex;
        }
    }

    private static void ClearRegion(int startRow, int lineCount, int width)
    {
        if (width <= 0 || lineCount <= 0)
        {
            return;
        }

        string blank = new string(' ', width);
        try
        {
            for (int i = 0; i < lineCount; i++)
            {
                System.Console.SetCursorPosition(0, startRow + i);
                System.Console.Write(blank);
            }
        }
        catch (Exception ex)
        {
            _ = ex;
        }
    }
}
