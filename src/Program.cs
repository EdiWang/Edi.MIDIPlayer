namespace Edi.MIDIPlayer;

internal class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            ConsoleDisplay.Initialize();

            string filePath = GetMidiFilePath(args);

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                ConsoleDisplay.WriteMessage("ERROR", "MIDI file not found or invalid path", ConsoleColor.Red);
                return;
            }

            var player = new MidiPlayer();
            await player.PlayMidiFileAsync(filePath);
        }
        catch (Exception ex)
        {
            ConsoleDisplay.WriteMessage("FATAL", $"Unexpected error: {ex.Message}", ConsoleColor.Red);
        }
        finally
        {
            ConsoleDisplay.WaitForKeyPress();
        }
    }

    private static string GetMidiFilePath(string[] args)
    {
        if (args.Length > 0)
        {
            return args[0];
        }

        return ConsoleDisplay.GetUserInput("Enter MIDI file path for injection:");
    }
}
