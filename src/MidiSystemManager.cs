using System.Runtime.InteropServices;

namespace Edi.MIDIPlayer;

public static class MidiSystemManager
{
    [DllImport("winmm.dll")]
    private static extern int midiOutGetNumDevs();

    public static bool Initialize()
    {
        var logger = new ConsoleLogger();

        try
        {
            logger.PrintInitializationHeader();

            int deviceCount = midiOutGetNumDevs();
            logger.PrintSystemStatus("MIDI Output Devices Detected", $"{deviceCount} devices", ConsoleColor.Cyan);

            if (deviceCount == 0)
            {
                logger.PrintSystemStatus("MIDI System Status", "CRITICAL ERROR", ConsoleColor.Red);
                logger.PrintCriticalError();
                return false;
            }

            logger.PrintSystemStatus("MIDI System Status", "ONLINE", ConsoleColor.Green);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.ResetColor();
            Console.WriteLine();
            return true;
        }
        catch (Exception ex)
        {
            logger.PrintSystemStatus("System Initialization", "FAILED", ConsoleColor.Red);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"🚨 EXCEPTION: {ex.Message}");
            Console.ResetColor();
            return false;
        }
    }
}