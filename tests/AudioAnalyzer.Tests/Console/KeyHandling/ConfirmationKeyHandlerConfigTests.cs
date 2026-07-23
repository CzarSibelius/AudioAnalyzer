using AudioAnalyzer.Console;
using Xunit;

namespace AudioAnalyzer.Tests.Console.KeyHandling;

/// <summary>Tests for <see cref="ConfirmationKeyHandlerConfig"/>: Y/Enter confirm, N/Esc cancel (ADR-0093).</summary>
public sealed class ConfirmationKeyHandlerConfigTests
{
    private static ConsoleKeyInfo Key(ConsoleKey key) => new('\0', key, shift: false, alt: false, control: false);

    [Theory]
    [InlineData(ConsoleKey.Y)]
    [InlineData(ConsoleKey.Enter)]
    public void ConfirmKeys_SetResultTrue_AndCloseModal(ConsoleKey key)
    {
        var config = new ConfirmationKeyHandlerConfig();
        var ctx = new ConfirmationKeyContext();

        bool handled = config.Handle(Key(key), ctx);

        Assert.True(handled);
        Assert.True(ctx.Result);
    }

    [Theory]
    [InlineData(ConsoleKey.N)]
    [InlineData(ConsoleKey.Escape)]
    public void CancelKeys_SetResultFalse_AndCloseModal(ConsoleKey key)
    {
        var config = new ConfirmationKeyHandlerConfig();
        var ctx = new ConfirmationKeyContext();

        bool handled = config.Handle(Key(key), ctx);

        Assert.True(handled);
        Assert.False(ctx.Result);
    }

    [Fact]
    public void UnknownKey_DoesNotDecide_AndKeepsModalOpen()
    {
        var config = new ConfirmationKeyHandlerConfig();
        var ctx = new ConfirmationKeyContext();

        bool handled = config.Handle(Key(ConsoleKey.Spacebar), ctx);

        Assert.False(handled);
        Assert.Null(ctx.Result);
    }

    [Fact]
    public void GetBindings_ExposesConfirmAndCancel()
    {
        var config = new ConfirmationKeyHandlerConfig();

        var bindings = config.GetBindings();

        Assert.Contains(bindings, b => b.Key == "Y/Enter" && b.Description.Contains("Confirm", StringComparison.Ordinal));
        Assert.Contains(bindings, b => b.Key == "N/Esc" && b.Description.Contains("Cancel", StringComparison.Ordinal));
    }
}
