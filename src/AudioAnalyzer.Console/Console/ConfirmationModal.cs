using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>
/// Reusable yes/no confirmation modal (ADR-0093): breadcrumb + prompt + choice hint, rendered full-screen via
/// <see cref="ModalSystem.RunModal"/>. Y/Enter confirms, N/Esc cancels (the default). Currently wired for quit only.
/// </summary>
internal sealed class ConfirmationModal : IConfirmationModal
{
    private const int PromptRow = 1;
    private const int ChoiceRow = 2;

    private readonly IKeyHandler<ConfirmationKeyContext> _keyHandler;
    private readonly IUiThemeResolver _uiThemeResolver;
    private readonly IConsoleDimensions _consoleDimensions;
    private readonly ITitleBarNavigationContext _navigation;
    private readonly ITitleBarBreadcrumbFormatter _breadcrumbFormatter;

    /// <summary>Initializes a new instance of the <see cref="ConfirmationModal"/> class.</summary>
    public ConfirmationModal(
        IKeyHandler<ConfirmationKeyContext> keyHandler,
        IUiThemeResolver uiThemeResolver,
        IConsoleDimensions consoleDimensions,
        ITitleBarNavigationContext navigation,
        ITitleBarBreadcrumbFormatter breadcrumbFormatter)
    {
        _keyHandler = keyHandler ?? throw new ArgumentNullException(nameof(keyHandler));
        _uiThemeResolver = uiThemeResolver ?? throw new ArgumentNullException(nameof(uiThemeResolver));
        _consoleDimensions = consoleDimensions ?? throw new ArgumentNullException(nameof(consoleDimensions));
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        _breadcrumbFormatter = breadcrumbFormatter ?? throw new ArgumentNullException(nameof(breadcrumbFormatter));
    }

    /// <inheritdoc />
    public bool Show(string title, string prompt, Action<bool> setModalOpen)
    {
        ArgumentNullException.ThrowIfNull(setModalOpen);
        string verb = string.IsNullOrWhiteSpace(title) ? "confirm" : title.Trim();
        var context = new ConfirmationKeyContext();

        void DrawContent()
        {
            int width = _consoleDimensions.GetConsoleWidth();
            TitleBarBreadcrumbRow.Write(0, width, _breadcrumbFormatter);

            var palette = _uiThemeResolver.GetEffectiveUiPalette();
            string normalCode = AnsiConsole.ColorCode(palette.Normal);
            string highlightedCode = AnsiConsole.ColorCode(palette.Highlighted);
            string reset = AnsiConsole.ResetCode;

            try
            {
                System.Console.SetCursorPosition(0, PromptRow);
                System.Console.Write(normalCode + "  " + prompt + reset);

                System.Console.SetCursorPosition(0, ChoiceRow);
                System.Console.Write(
                    MenuSelectionAffordance.UnselectedPrefix
                    + normalCode + "Y/Enter: " + verb + reset
                    + "    "
                    + highlightedCode + "N/Esc: cancel" + reset);
            }
            catch (Exception ex)
            {
                _ = ex; /* Console unavailable: skip this frame */
            }
        }

        bool HandleKey(ConsoleKeyInfo key) => _keyHandler.Handle(key, context);

        ModalSystem.RunModal(
            DrawContent,
            HandleKey,
            onClose: () =>
            {
                _navigation.View = TitleBarViewKind.Main;
                _navigation.ConfirmationBreadcrumbSuffix = null;
                setModalOpen(false);
            },
            onEnter: () =>
            {
                _navigation.ConfirmationBreadcrumbSuffix = verb;
                _navigation.View = TitleBarViewKind.ConfirmationModal;
                setModalOpen(true);
            });

        return context.Result == true;
    }
}
