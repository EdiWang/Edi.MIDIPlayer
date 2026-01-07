using Edi.MIDIPlayer.Hubs;
using Edi.MIDIPlayer.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Edi.MIDIPlayer.Services;

public class WebDisplayService(IHubContext<MidiPlayerHub> hubContext) : IConsoleDisplay
{
    private int _activityIndex = 0;
    private readonly Lock _consoleLock = new();

    public void DisplayHackerBanner()
    {
        _ = hubContext.Clients.All.SendAsync("ReceiveMessage", "INIT", "EDI.MIDIPLAYER Web Visualizer", "cyan");
    }

    public void WriteMessage(string type, string message, ConsoleColor color)
    {
        var colorString = color.ToString().ToLower();
        _ = hubContext.Clients.All.SendAsync("ReceiveMessage", type, message, colorString);
    }

    public void UpdateActivityIndicator()
    {
        _activityIndex = (_activityIndex + 1) % 5;
    }

    public Lock GetConsoleLock() => _consoleLock;

    public string CreateVelocityBar(int velocity)
    {
        return velocity.ToString();
    }
}