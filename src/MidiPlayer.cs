using NAudio.Midi;
using System.Diagnostics;

namespace Edi.MIDIPlayer;

public class MidiPlayer
{
    public static async Task PlayMidiFileAsync(string filePath)
    {
        MidiOut? midiOut = null;
        try
        {
            var midiFile = new MidiFile(filePath, false);

            ConsoleDisplay.WriteMessage("SCAN", $"Detected {midiFile.Tracks:X2} tracks, {midiFile.DeltaTicksPerQuarterNote:X4} ticks/quarter", ConsoleColor.Gray);

            // Fix: Check for available MIDI devices before creating MidiOut
            if (MidiOut.NumberOfDevices == 0)
            {
                ConsoleDisplay.WriteMessage("ERROR", "No MIDI output devices available", ConsoleColor.Red);
                return;
            }

            midiOut = new MidiOut(0);

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

            ConsoleDisplay.WriteMessage("PROC", $"Processed 0x{allEvents.Count:X} MIDI opcodes", ConsoleColor.Green);

            await PlayEventsAsync(allEvents, midiOut, midiFile.DeltaTicksPerQuarterNote);
        }
        catch (Exception ex)
        {
            ConsoleDisplay.WriteMessage("ERROR", $"Execution failed: {ex.Message}", ConsoleColor.Red);
        }
        finally
        {
            midiOut?.Dispose();
        }
    }

    private static async Task PlayEventsAsync(List<MidiEventInfo> allEvents, MidiOut midiOut, int ticksPerQuarterNote)
    {
        var stopwatch = Stopwatch.StartNew();
        var activeNotes = new HashSet<int>();

        // Build tempo map
        var tempoMap = TempoManager.BuildTempoMap(allEvents, ticksPerQuarterNote);

        ConsoleDisplay.WriteMessage("EXEC", "Initiating MIDI stream injection...", ConsoleColor.Yellow);
        Thread.Sleep(500);
        ConsoleDisplay.WriteMessage("LIVE", "REAL-TIME MIDI ANALYSIS", ConsoleColor.Green);

        // Write a divider line
        Console.WriteLine(new string('-', 81), ConsoleColor.DarkGray);

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

        ConsoleDisplay.WriteMessage("COMP", "MIDI injection terminated successfully", ConsoleColor.Green);
        ConsoleDisplay.WriteMessage("STATS", $"Final buffer state: 0x{activeNotes.Count:X2} active notes", ConsoleColor.Gray);
    }

    private static void ProcessMidiEvent(MidiEventInfo midiEntry, MidiOut midiOut, Stopwatch stopwatch, HashSet<int> activeNotes)
    {
        var elapsed = stopwatch.Elapsed;
        var timestamp = $"{elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}.{elapsed.Milliseconds:D3}";

        switch (midiEntry.Event.CommandCode)
        {
            case MidiCommandCode.NoteOn:
                var noteEvent = (NoteEvent)midiEntry.Event;
                if (noteEvent.Velocity > 0)
                {
                    activeNotes.Add(noteEvent.NoteNumber);
                    NoteProcessor.DisplayNoteOn(timestamp, noteEvent, activeNotes.Count);
                    midiOut.Send(MidiMessage.StartNote(noteEvent.NoteNumber, noteEvent.Velocity, noteEvent.Channel).RawData);
                }
                else
                {
                    activeNotes.Remove(noteEvent.NoteNumber);
                    NoteProcessor.DisplayNoteOff(timestamp, noteEvent, activeNotes.Count);
                    midiOut.Send(MidiMessage.StopNote(noteEvent.NoteNumber, noteEvent.Velocity, noteEvent.Channel).RawData);
                }
                break;

            case MidiCommandCode.NoteOff:
                var noteOffEvent = (NoteEvent)midiEntry.Event;
                activeNotes.Remove(noteOffEvent.NoteNumber);
                NoteProcessor.DisplayNoteOff(timestamp, noteOffEvent, activeNotes.Count);
                midiOut.Send(MidiMessage.StopNote(noteOffEvent.NoteNumber, noteOffEvent.Velocity, noteOffEvent.Channel).RawData);
                break;

            case MidiCommandCode.ControlChange:
                var controlEvent = (ControlChangeEvent)midiEntry.Event;
                NoteProcessor.DisplayControlChange(timestamp, controlEvent, activeNotes.Count);
                midiOut.Send(MidiMessage.ChangeControl((int)controlEvent.Controller, controlEvent.ControllerValue, controlEvent.Channel).RawData);
                break;

            case MidiCommandCode.PatchChange:
                var programEvent = (PatchChangeEvent)midiEntry.Event;
                ConsoleDisplay.WriteMessage("PROG", $"Program Change: {programEvent.Patch}", ConsoleColor.Magenta);
                midiOut.Send(MidiMessage.ChangePatch(programEvent.Patch, programEvent.Channel).RawData);
                break;
        }

        // Update activity indicator
        ConsoleDisplay.UpdateActivityIndicator();
    }
}