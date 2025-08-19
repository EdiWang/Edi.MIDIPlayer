namespace Edi.MIDIPlayer;

public class NoteProcessor
{
    private static readonly Dictionary<int, string> NoteNames = new()
    {
        { 0, "C" }, { 1, "C#" }, { 2, "D" }, { 3, "D#" }, { 4, "E" }, { 5, "F" },
        { 6, "F#" }, { 7, "G" }, { 8, "G#" }, { 9, "A" }, { 10, "A#" }, { 11, "B" }
    };

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
            0 or 2 or 4 or 5 or 7 or 9 or 11 => ConsoleColor.White,  // Natural notes
            _ => ConsoleColor.Magenta  // Sharp/flat notes
        };
    }

    public static void DisplayNoteOn(string timestamp, NAudio.Midi.NoteEvent noteEvent, int activeNotesCount)
    {
        var noteName = GetNoteName(noteEvent.NoteNumber);
        var velocityBar = ConsoleDisplay.CreateVelocityBar(noteEvent.Velocity);

        lock (ConsoleDisplay.GetConsoleLock())
        {
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(timestamp);
            Console.ResetColor();
            Console.Write("] ");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"0x{noteEvent.NoteNumber:X2}{noteEvent.Channel:X1}{noteEvent.Velocity:X2} ");
            Console.ResetColor();

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
            Console.Write($" │ HEAP: 0x{activeNotesCount:X3} │ NOTE: 0x{noteEvent.NoteNumber:X2}");
            Console.WriteLine();
            Console.ResetColor();
        }
    }

    public static void DisplayNoteOff(string timestamp, NAudio.Midi.NoteEvent noteEvent, int activeNotesCount)
    {
        var noteName = GetNoteName(noteEvent.NoteNumber);

        lock (ConsoleDisplay.GetConsoleLock())
        {
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(timestamp);
            Console.ResetColor();
            Console.Write("] ");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"0x{noteEvent.NoteNumber:X2}{noteEvent.Channel:X1}00 ");
            Console.ResetColor();

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
            Console.Write($" │ HEAP: 0x{activeNotesCount:X3} │ NOTE: 0x{noteEvent.NoteNumber:X2}");
            Console.WriteLine();
            Console.ResetColor();
        }
    }
}