namespace Edi.MIDIPlayer;

public class InputHandler
{
    public static string GetMidiFileUrl(string[] args)
    {
        if (args.Length > 0)
        {
            return args[0];
        }

        ConsoleDisplay.WriteMessage("INPUT", "Enter MIDI file path or URL:", ConsoleColor.Yellow);
        Console.ForegroundColor = ConsoleColor.White;

        var path = Console.ReadLine()?.Trim().Trim('"') ?? string.Empty;
        
        // Handle null characters and other control characters as empty input
        if (string.IsNullOrEmpty(path) || path.All(c => char.IsControl(c)))
        {
            path = string.Empty;
        }
        
        Console.ResetColor();

        return path;
    }
}