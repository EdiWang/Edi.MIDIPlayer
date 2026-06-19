using Microsoft.Extensions.Logging;

namespace Edi.MIDIPlayer.Services;

internal static class SignalRSendObserver
{
    public static async Task ObserveAsync(Task sendTask, ILogger logger, string eventName)
    {
        try
        {
            await sendTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send SignalR event {EventName}.", eventName);
        }
    }
}
