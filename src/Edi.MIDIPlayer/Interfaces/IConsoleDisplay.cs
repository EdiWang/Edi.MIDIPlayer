namespace Edi.MIDIPlayer.Interfaces;

public interface IConsoleDisplay
{
    void DisplayHackerBanner();
    void WriteMessage(string type, string message, ConsoleColor color);
    void UpdateActivityIndicator();
    string CreateVelocityBar(int velocity);
    Lock GetConsoleLock();
}