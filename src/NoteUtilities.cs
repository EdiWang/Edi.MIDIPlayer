using System.Text;

namespace Edi.MIDIPlayer;

public static class NoteUtilities
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

    public static ConsoleColor GetNoteColor(string noteName)
    {
        if (string.IsNullOrEmpty(noteName)) return ConsoleColor.White;
        
        var noteChar = noteName[0];
        var hasSharp = noteName.Contains('#');
        
        return hasSharp ? ConsoleColor.Magenta : ConsoleColor.White;
    }

    public static string CreateVelocityBar(int velocity)
    {
        var barLength = 10;
        var filledLength = (int)((velocity / 127.0) * barLength);
        var bar = new StringBuilder();

        for (int i = 0; i < barLength; i++)
        {
            if (i < filledLength)
            {
                bar.Append('█');
            }
            else
            {
                bar.Append('░');
            }
        }

        return bar.ToString();
    }
}