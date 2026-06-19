using Edi.MIDIPlayer.Services;
using NAudio.Midi;
using Xunit;

namespace Edi.MIDIPlayer.Tests;

public class MidiDisplayFormatterTests
{
    [Theory]
    [InlineData(60, "C4")]
    [InlineData(61, "C#4")]
    [InlineData(21, "A0")]
    [InlineData(108, "C8")]
    public void GetNoteName_FormatsMidiNoteNames(int noteNumber, string expected)
    {
        Assert.Equal(expected, MidiDisplayFormatter.GetNoteName(noteNumber));
    }

    [Theory]
    [InlineData(60, ConsoleColor.White)]
    [InlineData(61, ConsoleColor.Magenta)]
    public void GetNoteColor_DistinguishesNaturalAndSharpNotes(int noteNumber, ConsoleColor expected)
    {
        Assert.Equal(expected, MidiDisplayFormatter.GetNoteColor(noteNumber));
    }

    [Theory]
    [InlineData(MidiController.Sustain, "SUST")]
    [InlineData(MidiController.MainVolume, "VOL")]
    [InlineData(MidiController.Pan, "PAN")]
    [InlineData(MidiController.Expression, "EXPR")]
    [InlineData(MidiController.Modulation, "MOD")]
    [InlineData(MidiController.AllNotesOff, "ANOF")]
    public void GetControllerName_FormatsKnownControllers(MidiController controller, string expected)
    {
        Assert.Equal(expected, MidiDisplayFormatter.GetControllerName(controller));
    }

    [Fact]
    public void GetControllerName_FormatsUnknownControllersAsControlChangeNumbers()
    {
        Assert.Equal("CC02", MidiDisplayFormatter.GetControllerName((MidiController)2));
    }
}
