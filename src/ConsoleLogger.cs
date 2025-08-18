namespace Edi.MIDIPlayer;

public class ConsoleLogger
{
    public void PrintSystemStatus(string message, string status, ConsoleColor statusColor)
    {
        Console.Write("[");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("SYS");
        Console.ResetColor();
        Console.Write("] ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"{message,-35}");
        Console.ResetColor();
        Console.Write(" [");
        Console.ForegroundColor = statusColor;
        Console.Write(status);
        Console.ResetColor();
        Console.WriteLine("]");
    }

    public void PrintInitializationHeader()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("─────────────────── SYSTEM INITIALIZATION ────────────────────");
        Console.ResetColor();
        Console.WriteLine("Loading MIDI drivers");
        Thread.Sleep(100);
        Console.WriteLine("Scanning audio devices");
        Thread.Sleep(100);
        Console.WriteLine("Initializing synthesizers");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("──────────────────────────────────────────────────────────────");
        Console.ResetColor();
        Console.WriteLine();
    }

    public void PrintPlaybackHeader()
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("┌─ INITIATING PLAYBACK SEQUENCE ──────────────────────────────┐");
        Console.WriteLine("│ Loading MIDI stream into memory buffer...                   │");
        Console.WriteLine("│ Preparing dual-thread audio/visual processing...            │");
        Console.WriteLine("└─────────────────────────────────────────────────────────────┘");
        Console.ResetColor();
    }

    public void PrintCriticalError()
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("┌─ ERROR ANALYSIS ─────────────────────────────────────────────┐");
        Console.WriteLine("│ ⚠️  No MIDI output devices found in system registry          │");
        Console.WriteLine("│ 💡 SUGGESTION: Install virtual MIDI synthesizer             │");
        Console.WriteLine("│ 🔧 Check Windows Audio Service status                       │");
        Console.WriteLine("└──────────────────────────────────────────────────────────────┘");
        Console.ResetColor();
    }

    public void PrintMidiAnalysisHeader()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n┌─ MIDI STREAM ANALYZER ──────────────────────────────────────┐");
        Console.WriteLine("│ Real-time MIDI event processing and hex dump visualization  │ ");
        Console.WriteLine("│ Format: [Timestamp] Command: HEX_DATA (Event Description)   │ ");
        Console.WriteLine("└─────────────────────────────────────────────────────────────┘");
        Console.ResetColor();
    }
}