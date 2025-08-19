using System.Text;

namespace Edi.MIDIPlayer;

public static class ConsoleDisplay
{
    private static readonly char[] ActivityChars = ['█', '▓', '▒', '░', '·'];
    private static int _activityIndex = 0;
    private static readonly Lock _consoleLock = new();

    public static void Initialize()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.Clear();
        DisplayBanner();
    }

    private static void DisplayBanner()
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.ResetColor();

        WriteMessage("INIT", "EDI.MIDIPLAYER Terminal", ConsoleColor.Cyan);
        WriteMessage("INIT", "Initializing audio subsystems...", ConsoleColor.Gray);
        Thread.Sleep(500);
        Console.WriteLine();
    }

    public static void WriteMessage(string type, string message, ConsoleColor color)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");

        Console.Write("[");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(timestamp);
        Console.ResetColor();
        Console.Write("] ");

        Console.Write("[");
        Console.ForegroundColor = color;
        Console.Write($"{type,-5}");
        Console.ResetColor();
        Console.Write("] ");

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public static void WriteNoteOn(string timestamp, string noteName, int channel, string velocityBar, int activeNotes)
    {
        lock (_consoleLock)
        {
            Console.ForegroundColor = NoteUtilities.GetNoteColor(noteName);
            Console.Write("▲");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($" {timestamp} ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"NOTE_ON  ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{noteName,-4} ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"CH{channel + 1:D2} ");
            Console.Write(velocityBar);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($" │ Active: {activeNotes:D3}");
            Console.ResetColor();
        }
    }

    public static void WriteNoteOff(string timestamp, string noteName, int channel, int activeNotes)
    {
        lock (_consoleLock)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("▼");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($" {timestamp} ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"NOTE_OFF ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{noteName,-4} ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"CH{channel + 1:D2} ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("░░░░░░░░░░");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($" │ Active: {activeNotes:D3}");
            Console.ResetColor();
        }
    }

    public static void UpdateActivityIndicator()
    {
        _activityIndex = (_activityIndex + 1) % ActivityChars.Length;
    }

    public static string GetUserInput(string prompt)
    {
        WriteMessage("INPUT", prompt, ConsoleColor.Yellow);
        Console.Write("     > ");
        Console.ForegroundColor = ConsoleColor.White;

        var input = Console.ReadLine()?.Trim('"') ?? string.Empty;
        Console.ResetColor();

        return input;
    }

    public static void WaitForKeyPress()
    {
        WriteMessage("SYSTEM", "Press any key to exit...", ConsoleColor.Yellow);
        Console.ReadKey();
    }
}