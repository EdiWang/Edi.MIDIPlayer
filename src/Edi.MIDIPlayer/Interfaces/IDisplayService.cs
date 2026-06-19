namespace Edi.MIDIPlayer.Interfaces;

public interface IDisplayService
{
    void DisplayHackerBanner();
    void WriteMessage(string type, string message, ConsoleColor color);
    void UpdateActivityIndicator();
}
