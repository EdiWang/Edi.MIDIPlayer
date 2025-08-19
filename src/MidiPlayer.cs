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
                DisplayControlChange(timestamp, controlEvent, activeNotes.Count);
                midiOut.Send(MidiMessage.ChangeControl((int)controlEvent.Controller, controlEvent.ControllerValue, controlEvent.Channel).RawData);
                break;

            case MidiCommandCode.PatchChange:
                var programEvent = (PatchChangeEvent)midiEntry.Event;
                ConsoleDisplay.WriteMessage("PROG", "0xPR0GRAM", $"Program Change: {programEvent.Patch}", ConsoleColor.Magenta);
                midiOut.Send(MidiMessage.ChangePatch(programEvent.Patch, programEvent.Channel).RawData);
                break;
        }

        // Update activity indicator
        ConsoleDisplay.UpdateActivityIndicator();
    }

    private static void DisplayControlChange(string timestamp, ControlChangeEvent controlEvent, int activeNotesCount)
    {
        var controllerName = GetControllerName(controlEvent.Controller);
        var valueBar = CreateControlValueBar(controlEvent.ControllerValue);

        lock (ConsoleDisplay.GetConsoleLock())
        {
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(timestamp);
            Console.ResetColor();
            Console.Write("] ");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"0x{(int)controlEvent.Controller:X2}{controlEvent.Channel:X1}{controlEvent.ControllerValue:X2} ");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("◄ ");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("CTRL_CHG ");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{controllerName,-4} ");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"CH{controlEvent.Channel + 1:D2} ");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write($"VAL:0x{controlEvent.ControllerValue:X2} ");
            Console.ResetColor();

            Console.Write(valueBar);

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($" │ HEAP: 0x{activeNotesCount:X3} │ CTRL: 0x{(int)controlEvent.Controller:X2}");
            Console.WriteLine();
            Console.ResetColor();
        }
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

    private static string CreateControlValueBar(int value)
    {
        var barLength = 10;
        var filledLength = (int)((value / 127.0) * barLength);
        var bar = new System.Text.StringBuilder();

        for (int i = 0; i < barLength; i++)
        {
            if (i < filledLength)
            {
                if (i < 3) Console.ForegroundColor = ConsoleColor.Green;
                else if (i < 7) Console.ForegroundColor = ConsoleColor.Yellow;
                else Console.ForegroundColor = ConsoleColor.Red;
                bar.Append('█');
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                bar.Append('░');
            }
        }
        Console.ResetColor();

        return bar.ToString();
    }
}