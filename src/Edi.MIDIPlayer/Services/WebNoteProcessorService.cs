using Edi.MIDIPlayer.Hubs;
using Edi.MIDIPlayer.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NAudio.Midi;

namespace Edi.MIDIPlayer.Services;

public class WebNoteProcessorService(
    IHubContext<MidiPlayerHub> hubContext,
    ILogger<WebNoteProcessorService> logger) : INoteProcessor
{
    public void DisplayNoteOn(string timestamp, NoteEvent noteEvent, int activeNotesCount)
    {
        var noteName = MidiDisplayFormatter.GetNoteName(noteEvent.NoteNumber);
        _ = SignalRSendObserver.ObserveAsync(
            hubContext.Clients.All.SendAsync("ReceiveNoteOn",
                noteEvent.NoteNumber,
                noteEvent.Velocity,
                noteEvent.Channel,
                noteName,
                timestamp),
            logger,
            "ReceiveNoteOn");
    }

    public void DisplayNoteOff(string timestamp, NoteEvent noteEvent, int activeNotesCount)
    {
        var noteName = MidiDisplayFormatter.GetNoteName(noteEvent.NoteNumber);
        _ = SignalRSendObserver.ObserveAsync(
            hubContext.Clients.All.SendAsync("ReceiveNoteOff",
                noteEvent.NoteNumber,
                noteEvent.Channel,
                noteName,
                timestamp),
            logger,
            "ReceiveNoteOff");
    }

    public void DisplayControlChange(string timestamp, ControlChangeEvent controlEvent, int activeNotesCount)
    {
        var controllerName = MidiDisplayFormatter.GetControllerName(controlEvent.Controller);
        _ = SignalRSendObserver.ObserveAsync(
            hubContext.Clients.All.SendAsync("ReceiveControlChange",
                controllerName,
                controlEvent.ControllerValue,
                controlEvent.Channel,
                timestamp),
            logger,
            "ReceiveControlChange");
    }

}
