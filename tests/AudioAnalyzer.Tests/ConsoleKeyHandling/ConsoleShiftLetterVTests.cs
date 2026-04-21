using AudioAnalyzer.Console.KeyHandling;
using Xunit;

namespace AudioAnalyzer.Tests.ConsoleKeyHandling;

public sealed class ConsoleShiftLetterVTests
{
    [Fact]
    public void IsShiftVChord_true_when_modifiers_include_shift()
    {
        var key = new ConsoleKeyInfo('v', ConsoleKey.V, shift: true, alt: false, control: false);
        Assert.True(ConsoleShiftLetterV.IsShiftVChord(key));
        Assert.False(ConsoleShiftLetterV.IsPlainPresetVChord(key));
    }

    [Fact]
    public void IsShiftVChord_true_for_uppercase_V_when_caps_lock_off_even_without_shift_modifier()
    {
        if (global::System.Console.CapsLock)
        {
            return;
        }

        var key = new ConsoleKeyInfo('V', ConsoleKey.V, shift: false, alt: false, control: false);
        Assert.True(ConsoleShiftLetterV.IsShiftVChord(key));
        Assert.False(ConsoleShiftLetterV.IsPlainPresetVChord(key));
    }

    [Fact]
    public void IsPlainPresetVChord_true_for_lowercase_v()
    {
        var key = new ConsoleKeyInfo('v', ConsoleKey.V, shift: false, alt: false, control: false);
        Assert.False(ConsoleShiftLetterV.IsShiftVChord(key));
        Assert.True(ConsoleShiftLetterV.IsPlainPresetVChord(key));
    }

    [Fact]
    public void IsShiftVChord_false_when_caps_lock_on_and_uppercase_V_without_shift()
    {
        if (!global::System.Console.CapsLock)
        {
            return;
        }

        var key = new ConsoleKeyInfo('V', ConsoleKey.V, shift: false, alt: false, control: false);
        Assert.False(ConsoleShiftLetterV.IsShiftVChord(key));
        Assert.True(ConsoleShiftLetterV.IsPlainPresetVChord(key));
    }
}
