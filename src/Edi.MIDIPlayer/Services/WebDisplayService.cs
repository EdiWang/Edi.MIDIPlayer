using Edi.MIDIPlayer.Hubs;
using Edi.MIDIPlayer.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Edi.MIDIPlayer.Services;

public class WebDisplayService(
    IHubContext<MidiPlayerHub> hubContext,
    ILogger<WebDisplayService> logger) : IDisplayService
{
    private int _activityIndex = 0;

    public void DisplayHackerBanner()
    {
        _ = SignalRSendObserver.ObserveAsync(
            hubContext.Clients.All.SendAsync("ReceiveMessage", "INIT", "EDI.MIDIPLAYER Web Visualizer", "cyan"),
            logger,
            "ReceiveMessage");
    }

    public void WriteMessage(string type, string message, ConsoleColor color)
    {
        var colorString = color.ToString().ToLower();
        _ = SignalRSendObserver.ObserveAsync(
            hubContext.Clients.All.SendAsync("ReceiveMessage", type, message, colorString),
            logger,
            "ReceiveMessage");
    }

    public void UpdateActivityIndicator()
    {
        _activityIndex = (_activityIndex + 1) % 5;
    }
}
