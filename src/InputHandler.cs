namespace Edi.MIDIPlayer;

public class InputHandler
{
    public static string GetMidiFilePath(string[] args)
    {
        if (args.Length > 0)
        {
            return args[0];
        }

        ConsoleDisplay.WriteMessage("INPUT", "0x00000001", "Enter MIDI file path for injection:", ConsoleColor.Yellow);
        Console.Write("     0x1234 > ");
        Console.ForegroundColor = ConsoleColor.White;

        var path = Console.ReadLine()?.Trim('"') ?? string.Empty;
        Console.ResetColor();

        return path;
    }
}