using Microsoft.AspNetCore.SignalR;

namespace Edi.MIDIPlayer.Hubs;

public class MidiPlayerHub : Hub
{
    public async Task SendNotePreview(int noteNumber, int velocity, int channel, string noteName, double delayMs)
    {
        await Clients.All.SendAsync("ReceiveNotePreview", noteNumber, velocity, channel, noteName, delayMs);
    }

    public async Task SendNoteOn(int noteNumber, int velocity, int channel, string noteName, string timestamp)
    {
        await Clients.All.SendAsync("ReceiveNoteOn", noteNumber, velocity, channel, noteName, timestamp);
    }

    public async Task SendNoteOff(int noteNumber, int channel, string noteName, string timestamp)
    {
        await Clients.All.SendAsync("ReceiveNoteOff", noteNumber, channel, noteName, timestamp);
    }

    public async Task SendControlChange(string controllerName, int value, int channel, string timestamp)
    {
        await Clients.All.SendAsync("ReceiveControlChange", controllerName, value, channel, timestamp);
    }

    public async Task SendMessage(string type, string message, string color)
    {
        await Clients.All.SendAsync("ReceiveMessage", type, message, color);
    }
}