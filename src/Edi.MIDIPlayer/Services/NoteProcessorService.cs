using Edi.MIDIPlayer.Interfaces;
using NAudio.Midi;

namespace Edi.MIDIPlayer.Services;

public class NoteProcessorService(IConsoleDisplay consoleDisplay) : INoteProcessor
{
    private static readonly Dictionary<int, string> NoteNames = new()
    {
        { 0, "C" }, { 1, "C#" }, { 2, "D" }, { 3, "D#" }, { 4, "E" }, { 5, "F" },
        { 6, "F#" }, { 7, "G" }, { 8, "G#" }, { 9, "A" }, { 10, "A#" }, { 11, "B" }
    };

    public string GetNoteName(int noteNumber)
    {
        var octave = (noteNumber / 12) - 1;
        var note = NoteNames[noteNumber % 12];
        return $"{note}{octave}";
    }

    public ConsoleColor GetNoteColor(int noteNumber)
    {
        return (noteNumber % 12) switch
        {
            0 or 2 or 4 or 5 or 7 or 9 or 11 => ConsoleColor.White,  // Natural notes
            _ => ConsoleColor.Magenta  // Sharp/flat notes
        };
    }

    public void DisplayNoteOn(string timestamp, NoteEvent noteEvent, int activeNotesCount)
    {
        var noteName = GetNoteName(noteEvent.NoteNumber);
        var velocityBar = consoleDisplay.CreateVelocityBar(noteEvent.Velocity);

        lock (consoleDisplay.GetConsoleLock())
        {
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(timestamp);
            Console.ResetColor();
            Console.Write("] ");

            Console.ForegroundColor = GetNoteColor(noteEvent.NoteNumber);
            Console.Write("▲ ");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("NOTE_ON  ");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{noteName,-4} ");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"CH{noteEvent.Channel + 1:D2} ");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write($"VEL:0x{noteEvent.Velocity:X2} ");
            Console.ResetColor();

            Console.Write(velocityBar);

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($" │ ACTV: 0x{activeNotesCount:X2} │ NOTE: 0x{noteEvent.NoteNumber:X2}");
            Console.WriteLine();
            Console.ResetColor();
        }
    }

    public void DisplayNoteOff(string timestamp, NoteEvent noteEvent, int activeNotesCount)
    {
        var noteName = GetNoteName(noteEvent.NoteNumber);

        lock (consoleDisplay.GetConsoleLock())
        {
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(timestamp);
            Console.ResetColor();
            Console.Write("] ");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("▼ ");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("NOTE_OFF ");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{noteName,-4} ");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"CH{noteEvent.Channel + 1:D2} ");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Write("VEL:0x00 ");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("░░░░░░░░░░");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($" │ ACTV: 0x{activeNotesCount:X2} │ NOTE: 0x{noteEvent.NoteNumber:X2}");
            Console.WriteLine();
            Console.ResetColor();
        }
    }

    public void DisplayControlChange(string timestamp, ControlChangeEvent controlEvent, int activeNotesCount)
    {
        var controllerName = GetControllerName(controlEvent.Controller);
        var valueBar = consoleDisplay.CreateVelocityBar(controlEvent.ControllerValue);

        lock (consoleDisplay.GetConsoleLock())
        {
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(timestamp);
            Console.ResetColor();
            Console.Write("] ");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("◄ ");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("CTRL_CHG ");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{controllerName,-4} ");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"CH{controlEvent.Channel + 1:D2} ");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write($"VAL:0x{controlEvent.ControllerValue:X2} ");
            Console.ResetColor();

            Console.Write(valueBar);

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($" │ ACTV: 0x{activeNotesCount:X2} │ CTRL: 0x{(int)controlEvent.Controller:X2}");
            Console.WriteLine();
            Console.ResetColor();
        }
    }

    private static string GetControllerName(MidiController controller)
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