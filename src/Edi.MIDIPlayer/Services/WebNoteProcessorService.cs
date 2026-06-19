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
    private static readonly Dictionary<int, string> NoteNames = new()
    {
        { 0, "C" }, { 1, "C#" }, { 2, "D" }, { 3, "D#" }, { 4, "E" }, { 5, "F" },
        { 6, "F#" }, { 7, "G" }, { 8, "G#" }, { 9, "A" }, { 10, "A#" }, { 11, "B" }
    };

    public string GetNoteName(int noteNumber)
    {
        var octave = (noteNumber / 12) - 1;
        var note = NoteNames[noteNumber % 12];
        return $"{note}{octave}";
    }

    public ConsoleColor GetNoteColor(int noteNumber)
    {
        return (noteNumber % 12) switch
        {
            0 or 2 or 4 or 5 or 7 or 9 or 11 => ConsoleColor.White,
            _ => ConsoleColor.Magenta
        };
    }

    public void DisplayNoteOn(string timestamp, NoteEvent noteEvent, int activeNotesCount)
    {
        var noteName = GetNoteName(noteEvent.NoteNumber);
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
        var noteName = GetNoteName(noteEvent.NoteNumber);
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
        var controllerName = GetControllerName(controlEvent.Controller);
        _ = SignalRSendObserver.ObserveAsync(
            hubContext.Clients.All.SendAsync("ReceiveControlChange",
                controllerName,
                controlEvent.ControllerValue,
                controlEvent.Channel,
                timestamp),
            logger,
            "ReceiveControlChange");
    }

    private static string GetControllerName(MidiController controller)
    {
        return controller switch
        {
            MidiController.Sustain => "SUST",
            MidiController.MainVolume => "VOL",
            MidiController.Pan => "PAN",
            MidiController.Expression => "EXPR",
            MidiController.Modulation => "MOD",
            MidiController.AllNotesOff => "ANOF",
            _ => $"CC{(int)controller:D2}"
        };
    }
}
