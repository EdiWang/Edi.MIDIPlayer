namespace Edi.MIDIPlayer;

internal class Program
{
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // Initialize MIDI reader and player
        using var midiPlayer = new MidiPlayer();

        // Get file path from args or prompt user
        string filePath = args.Length > 0 ? args[0] : GetMidiFilePath();

        if (string.IsNullOrEmpty(filePath))
        {
            Console.WriteLine("No MIDI file specified. Exiting...");
            return;
        }

        // Initialize MIDI player
        bool midiPlayerReady = MidiPlayer.Initialize();
        if (midiPlayerReady)
        {
            Console.ResetColor();

            await RunPlayback(midiPlayer, filePath, midiPlayerReady);

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }

    private static string GetMidiFilePath()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("[INPUT] Enter MIDI file path: ");
        Console.ResetColor();

        var path = Console.ReadLine();
        return path?.Trim('"') ?? string.Empty;
    }

    private static async Task RunPlayback(MidiPlayer midiPlayer, string filePath, bool midiPlayerReady)
    {
        // Audio playback task (if available)
        if (midiPlayerReady)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Starting audio playback...");
            Console.ResetColor();

            try
            {
                await midiPlayer.PlayMidiFileAsync(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AUDIO ERROR] {ex.Message}");
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Audio playback unavailable - visual mode only");
            Console.ResetColor();
        }

        Console.WriteLine();

        // Wait a moment for cleanup
        await Task.Delay(500);

        Console.ResetColor();
        Console.WriteLine("\n[COMPLETE] Synchronized playback finished!");
    }
}
