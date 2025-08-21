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

            string fileUrl = InputHandler.GetMidiFileUrl(args);

            if (string.IsNullOrEmpty(fileUrl))
            {
                ConsoleDisplay.WriteMessage("ERROR", "MIDI file path or URL not provided", ConsoleColor.Red);
                return;
            }

            // Check if it's a URL or local file
            if (Uri.TryCreate(fileUrl, UriKind.Absolute, out Uri? uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                // It's a URL
                await MidiPlayer.PlayMidiFileAsync(fileUrl);
            }
            else
            {
                // It's a local file path
                if (!File.Exists(fileUrl))
                {
                    ConsoleDisplay.WriteMessage("ERROR", "MIDI file not found or invalid path", ConsoleColor.Red);
                    return;
                }
                await MidiPlayer.PlayMidiFileAsync(fileUrl);
            }
        }
        catch (Exception ex)
        {
            ConsoleDisplay.WriteMessage("FATAL", $"Unexpected error: {ex.Message}", ConsoleColor.Red);
        }
        finally
        {
            ConsoleDisplay.WriteMessage("SYSTEM", "Press any key to exit...", ConsoleColor.Yellow);
            Console.ReadKey();
        }
    }
}
