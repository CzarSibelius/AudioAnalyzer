using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Console.KeyHandling;
using Xunit;

namespace AudioAnalyzer.Tests.ConsoleKeyHandling;

public sealed class ConsoleShiftLetterVTests
{
    private sealed class FakeCapsLockState : ICapsLockState
    {
        public FakeCapsLockState(bool isCapsLockOn) => IsCapsLockOn = isCapsLockOn;

        public bool IsCapsLockOn { get; }
    }

    private static ConsoleShiftLetterV Create(bool capsLockOn) =>
        new(new FakeCapsLockState(capsLockOn));

    [Fact]
    public void IsShiftVChord_true_when_modifiers_include_shift()
    {
        var detector = Create(capsLockOn: false);
        var key = new ConsoleKeyInfo('v', ConsoleKey.V, shift: true, alt: false, control: false);
        Assert.True(detector.IsShiftVChord(key));
        Assert.False(detector.IsPlainPresetVChord(key));
    }

    [Fact]
    public void IsShiftVChord_true_for_uppercase_V_when_caps_lock_off_even_without_shift_modifier()
    {
        var detector = Create(capsLockOn: false);
        var key = new ConsoleKeyInfo('V', ConsoleKey.V, shift: false, alt: false, control: false);
        Assert.True(detector.IsShiftVChord(key));
        Assert.False(detector.IsPlainPresetVChord(key));
    }

    [Fact]
    public void IsPlainPresetVChord_true_for_lowercase_v()
    {
        var detector = Create(capsLockOn: false);
        var key = new ConsoleKeyInfo('v', ConsoleKey.V, shift: false, alt: false, control: false);
        Assert.False(detector.IsShiftVChord(key));
        Assert.True(detector.IsPlainPresetVChord(key));
    }

    [Fact]
    public void IsShiftVChord_false_when_caps_lock_on_and_uppercase_V_without_shift()
    {
        var detector = Create(capsLockOn: true);
        var key = new ConsoleKeyInfo('V', ConsoleKey.V, shift: false, alt: false, control: false);
        Assert.False(detector.IsShiftVChord(key));
        Assert.True(detector.IsPlainPresetVChord(key));
    }

    [Fact]
    public void IsShiftVChord_false_when_control_modifier_present()
    {
        var detector = Create(capsLockOn: false);
        var key = new ConsoleKeyInfo('v', ConsoleKey.V, shift: true, alt: false, control: true);
        Assert.False(detector.IsShiftVChord(key));
        Assert.False(detector.IsPlainPresetVChord(key));
    }
}
