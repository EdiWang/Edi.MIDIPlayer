using NAudio.Midi;

namespace Edi.MIDIPlayer.Services;

internal static class MidiDisplayFormatter
{
    private static readonly string[] NoteNames =
    [
        "C", "C#", "D", "D#", "E", "F",
        "F#", "G", "G#", "A", "A#", "B"
    ];

    public static string GetNoteName(int noteNumber)
    {
        var octave = (noteNumber / 12) - 1;
        var note = NoteNames[noteNumber % 12];
        return $"{note}{octave}";
    }

    public static ConsoleColor GetNoteColor(int noteNumber)
    {
        return (noteNumber % 12) switch
        {
            0 or 2 or 4 or 5 or 7 or 9 or 11 => ConsoleColor.White,
            _ => ConsoleColor.Magenta
        };
    }

    public static string GetControllerName(MidiController controller)
    {
        return controller switch
        {
            MidiController.Sustain => "SUST",
            MidiController.MainVolume => "VOL",
            MidiController.Pan => "PAN",
            MidiController.Expression => "EXPR",
            MidiController.Modulation => "MOD",
            MidiController.AllNotesOff => "ANOF",
            _ => $"CC{(int)controller:D2}"
        };
    }
}
