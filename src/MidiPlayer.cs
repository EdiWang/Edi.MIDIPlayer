using NAudio.Midi;
using System.Diagnostics;

namespace Edi.MIDIPlayer;

public class MidiPlayer
{
    public static async Task PlayMidiFileAsync(string filePath)
    {
        try
        {
            var midiFile = new MidiFile(filePath, false);

            ConsoleDisplay.WriteMessage("SCAN", "0xBEEFCAFE", $"Detected {midiFile.Tracks:X2} tracks, {midiFile.DeltaTicksPerQuarterNote:X4} ticks/quarter", ConsoleColor.Gray);

            using var midiOut = new MidiOut(0);

            // Simple event collection without parallel processing
            var allEvents = new List<MidiEventInfo>();
            for (int track = 0; track < midiFile.Tracks; track++)
            {
                foreach (MidiEvent midiEvent in midiFile.Events[track])
                {
                    allEvents.Add(new MidiEventInfo { AbsoluteTime = midiEvent.AbsoluteTime, Event = midiEvent });
                }
            }
            allEvents = [.. allEvents.OrderBy(e => e.AbsoluteTime)];

            ConsoleDisplay.WriteMessage("PROC", "0xDEADC0DE", $"Processed 0x{allEvents.Count:X} MIDI opcodes", ConsoleColor.Green);

            await PlayEventsAsync(allEvents, midiOut, midiFile.DeltaTicksPerQuarterNote);
        }
        catch (Exception ex)
        {
            ConsoleDisplay.WriteMessage("ERROR", "0xFFFFFFF", $"Execution failed: {ex.Message}", ConsoleColor.Red);
        }
    }

    private static async Task PlayEventsAsync(List<MidiEventInfo> allEvents, MidiOut midiOut, int ticksPerQuarterNote)
    {
        var stopwatch = Stopwatch.StartNew();
        var activeNotes = new HashSet<int>();

        // Build tempo map
        var tempoMap = TempoManager.BuildTempoMap(allEvents, ticksPerQuarterNote);

        ConsoleDisplay.WriteMessage("EXEC", "0xC0FFEE", "Initiating MIDI stream injection...", ConsoleColor.Yellow);
        Thread.Sleep(1000);

        Console.WriteLine();
        ConsoleDisplay.WriteMessage("LIVE", "0x1337", "REAL-TIME MIDI ANALYSIS", ConsoleColor.Green);

        var playbackStart = stopwatch.Elapsed;

        foreach (var midiEntry in allEvents)
        {
            // Calculate the expected time for this event
            var expectedTime = playbackStart.Add(TempoManager.TicksToTimeSpan(midiEntry.AbsoluteTime, tempoMap, ticksPerQuarterNote));
            var currentTime = stopwatch.Elapsed;

            // Wait until it's time to play this event
            var delayNeeded = expectedTime - currentTime;
            if (delayNeeded > TimeSpan.Zero)
            {
                await Task.Delay(delayNeeded);
            }

            ProcessMidiEvent(midiEntry, midiOut, stopwatch, activeNotes);
        }

        ConsoleDisplay.WriteMessage("COMP", "0xABCD", "MIDI injection terminated successfully", ConsoleColor.Green);
        ConsoleDisplay.WriteMessage("STATS", "0x5000D000", $"Final buffer state: 0x{activeNotes.Count:X2} active notes", ConsoleColor.Gray);
    }

    private static void ProcessMidiEvent(MidiEventInfo midiEntry, MidiOut midiOut, Stopwatch stopwatch, HashSet<int> activeNotes)
    {
        var elapsed = stopwatch.Elapsed;
        var timestamp = $"{elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}.{elapsed.Milliseconds:D3}";

        if (midiEntry.Event.CommandCode == MidiCommandCode.NoteOn)
        {
            var noteEvent = (NoteEvent)midiEntry.Event;
            if (noteEvent.Velocity > 0)
            {
                activeNotes.Add(noteEvent.NoteNumber);
                NoteProcessor.DisplayNoteOn(timestamp, noteEvent, activeNotes.Count);
                midiOut.Send(MidiMessage.StartNote(noteEvent.NoteNumber, noteEvent.Velocity, noteEvent.Channel).RawData);
            }
        }
        else if (midiEntry.Event.CommandCode == MidiCommandCode.NoteOff ||
                 (midiEntry.Event.CommandCode == MidiCommandCode.NoteOn &&
                  ((NoteEvent)midiEntry.Event).Velocity == 0))
        {
            var noteEvent = (NoteEvent)midiEntry.Event;
            activeNotes.Remove(noteEvent.NoteNumber);
            NoteProcessor.DisplayNoteOff(timestamp, noteEvent, activeNotes.Count);
            midiOut.Send(MidiMessage.StopNote(noteEvent.NoteNumber, noteEvent.Velocity, noteEvent.Channel).RawData);
        }
        else if (midiEntry.Event.CommandCode == MidiCommandCode.ControlChange)
        {
            var controlEvent = (ControlChangeEvent)midiEntry.Event;
            ConsoleDisplay.WriteMessage("CTRL", "0xC0NTROL", $"Control Change: {controlEvent.Controller} Value: {controlEvent.ControllerValue}", ConsoleColor.Cyan);
            midiOut.Send(MidiMessage.ChangeControl((int)controlEvent.Controller, controlEvent.ControllerValue, controlEvent.Channel).RawData);
        }
        else if (midiEntry.Event.CommandCode == MidiCommandCode.PatchChange)
        {
            var programEvent = (PatchChangeEvent)midiEntry.Event;
            ConsoleDisplay.WriteMessage("PROG", "0xPR0GRAM", $"Program Change: {programEvent.Patch}", ConsoleColor.Magenta);
            midiOut.Send(MidiMessage.ChangePatch(programEvent.Patch, programEvent.Channel).RawData);
        }

        // Update activity indicator
        ConsoleDisplay.UpdateActivityIndicator();
    }
}