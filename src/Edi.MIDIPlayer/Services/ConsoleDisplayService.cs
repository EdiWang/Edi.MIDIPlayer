using Edi.MIDIPlayer.Interfaces;
using System.Text;

namespace Edi.MIDIPlayer.Services;

public class ConsoleDisplayService : IConsoleDisplay
{
    private static readonly char[] ActivityChars = ['█', '▓', '▒', '░', '·'];
    private int _activityIndex = 0;
    private readonly Lock _consoleLock = new();

    public void DisplayHackerBanner()
    {
        Console.ForegroundColor = ConsoleColor.Green;

        string pianoArt = @"
 _______________________________________________________
|:::::: o o o o . |..... . .. . | [  ]  o o o o o ::::::|
|:::::: o o o o   | ..  . ..... |       o o o o o ::::::|
|::::::___________|__..._...__._|_________________::::::|
| # # | # # # | # # | # # # | # # | # # # | # # | # # # |
| # # | # # # | # # | # # # | # # | # # # | # # | # # # |
| # # | # # # | # # | # # # | # # | # # # | # # | # # # |
| | | | | | | | | | | | | | | | | | | | | | | | | | | | |
|_|_|_|_|_|_|_|_|_|_|_|_|_|_|_|_|_|_|_|_|_|_|_|_|_|_|_|_|
        ";

        Console.WriteLine(pianoArt);
        Console.ResetColor();

        WriteMessage("INIT", "EDI.MIDIPLAYER Terminal", ConsoleColor.Cyan);
        Thread.Sleep(500);
    }

    public void WriteMessage(string type, string message, ConsoleColor color)
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

    public void UpdateActivityIndicator()
    {
        _activityIndex = (_activityIndex + 1) % ActivityChars.Length;
    }

    public Lock GetConsoleLock() => _consoleLock;

    public string CreateVelocityBar(int velocity)
    {
        var barLength = 10;
        var filledLength = (int)((velocity / 127.0) * barLength);
        var bar = new StringBuilder();

        Console.ForegroundColor = ConsoleColor.Green;
        for (int i = 0; i < barLength; i++)
        {
            if (i < filledLength)
            {
                if (i < 3) Console.ForegroundColor = ConsoleColor.Green;
                else if (i < 7) Console.ForegroundColor = ConsoleColor.Yellow;
                else Console.ForegroundColor = ConsoleColor.Red;
                bar.Append('█');
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                bar.Append('░');
            }
        }
        Console.ResetColor();

        return bar.ToString();
    }
}