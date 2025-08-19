using System.Runtime.InteropServices;
using System.Text;

namespace Edi.MIDIPlayer;

internal class Program
{
    static async Task Main(string[] args)
    {
        // Check if running on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ERROR: This program is currently Windows only.");
            Console.WriteLine("The MIDI player requires Windows-specific audio subsystems to function properly.");
            Console.ResetColor();
            Environment.Exit(1);
        }

        try
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Clear();

            ConsoleDisplay.DisplayHackerBanner();

            string filePath = InputHandler.GetMidiFilePath(args);

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                ConsoleDisplay.WriteMessage("ERROR", "0xDEADBEEF", "MIDI file not found or invalid path", ConsoleColor.Red);
                return;
            }

            await MidiPlayer.PlayMidiFileAsync(filePath);
        }
        catch (Exception ex)
        {
            ConsoleDisplay.WriteMessage("FATAL", "0xC0000005", $"Unexpected error: {ex.Message}", ConsoleColor.Red);
        }
        finally
        {
            ConsoleDisplay.WriteMessage("SYSTEM", "0x00000000", "Press any key to exit...", ConsoleColor.Yellow);
            Console.ReadKey();
        }
    }
}
