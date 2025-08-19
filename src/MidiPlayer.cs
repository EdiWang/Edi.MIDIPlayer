using NAudio.Midi;
using System.Diagnostics;

namespace Edi.MIDIPlayer;

public class MidiPlayer
{
    public async Task PlayMidiFileAsync(string filePath)
    {
        try
        {
            var midiFile = new MidiFile(filePath, false);

            ConsoleDisplay.WriteMessage("SCAN", $"Detected {midiFile.Tracks} tracks, {midiFile.DeltaTicksPerQuarterNote} ticks/quarter", ConsoleColor.Gray);

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

            ConsoleDisplay.WriteMessage("PROC", $"Processed {allEvents.Count} MIDI events", ConsoleColor.Green);

            // Get initial tempo (default to 500ms/quarter note if missing)
            int ticksPerQuarterNote = midiFile.DeltaTicksPerQuarterNote;
            double tempo = 500000; // microseconds per quarter note (default 120 BPM)

            // Find tempo if specified
            foreach (var ev in allEvents)
            {
                if (ev.Event is MetaEvent meta && meta.MetaEventType == MetaEventType.SetTempo)
                {
                    tempo = ((TempoEvent)meta).MicrosecondsPerQuarterNote;
                    var bpm = 60000000.0 / tempo;
                    ConsoleDisplay.WriteMessage("TEMPO", $"BPM: {bpm:F1} ({tempo:F0} ¦Ìs/quarter)", ConsoleColor.Magenta);
                    break;
                }
            }

            await PlayEventsAsync(allEvents, midiOut, tempo, ticksPerQuarterNote);
        }
        catch (Exception ex)
        {
            ConsoleDisplay.WriteMessage("ERROR", $"Playback failed: {ex.Message}", ConsoleColor.Red);
        }
    }

    private async Task PlayEventsAsync(List<MidiEventInfo> allEvents, MidiOut midiOut, double tempo, int ticksPerQuarterNote)
    {
        var stopwatch = Stopwatch.StartNew();
        long lastTime = 0;
        var activeNotes = new HashSet<int>();

        ConsoleDisplay.WriteMessage("EXEC", "Initiating MIDI stream injection...", ConsoleColor.Yellow);
        Thread.Sleep(1000);

        Console.WriteLine();
        ConsoleDisplay.WriteMessage("LIVE", "REAL-TIME MIDI ANALYSIS", ConsoleColor.Green);

        foreach (var midiEntry in allEvents)
        {
            // Calculate real-time delay
            long deltaTicks = midiEntry.AbsoluteTime - lastTime;
            double msPerTick = tempo / 1000.0 / ticksPerQuarterNote;
            int delayMs = (int)(deltaTicks * msPerTick);

            if (delayMs > 0)
            {
                await Task.Delay(delayMs);
            }
            lastTime = midiEntry.AbsoluteTime;

            ProcessMidiEvent(midiEntry, midiOut, stopwatch, activeNotes);
        }

        ConsoleDisplay.WriteMessage("COMP", "MIDI injection completed successfully", ConsoleColor.Green);
        ConsoleDisplay.WriteMessage("STATS", $"Active notes at end: {activeNotes.Count}", ConsoleColor.Gray);
    }

    private void ProcessMidiEvent(MidiEventInfo midiEntry, MidiOut midiOut, Stopwatch stopwatch, HashSet<int> activeNotes)
    {
        var elapsed = stopwatch.Elapsed;
        var timestamp = $"{elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";

        if (midiEntry.Event.CommandCode == MidiCommandCode.NoteOn)
        {
            var noteEvent = (NoteEvent)midiEntry.Event;
            if (noteEvent.Velocity > 0)
            {
                activeNotes.Add(noteEvent.NoteNumber);
                var noteName = NoteUtilities.GetNoteName(noteEvent.NoteNumber);
                var velocityBar = NoteUtilities.CreateVelocityBar(noteEvent.Velocity);

                ConsoleDisplay.WriteNoteOn(timestamp, noteName, noteEvent.Channel, velocityBar, activeNotes.Count);

                midiOut.Send(MidiMessage.StartNote(noteEvent.NoteNumber, noteEvent.Velocity, noteEvent.Channel).RawData);
            }
        }
        else if (midiEntry.Event.CommandCode == MidiCommandCode.NoteOff ||
                 (midiEntry.Event.CommandCode == MidiCommandCode.NoteOn &&
                  ((NoteEvent)midiEntry.Event).Velocity == 0))
        {
            var noteEvent = (NoteEvent)midiEntry.Event;
            activeNotes.Remove(noteEvent.NoteNumber);
            var noteName = NoteUtilities.GetNoteName(noteEvent.NoteNumber);

            ConsoleDisplay.WriteNoteOff(timestamp, noteName, noteEvent.Channel, activeNotes.Count);

            midiOut.Send(MidiMessage.StopNote(noteEvent.NoteNumber, noteEvent.Velocity, noteEvent.Channel).RawData);
        }

        // Update activity indicator
        ConsoleDisplay.UpdateActivityIndicator();
    }
}