using Edi.MIDIPlayer.Interfaces;
using Edi.MIDIPlayer.Models;
using NAudio.Midi;
using System.Diagnostics;

namespace Edi.MIDIPlayer.Services;

public class MidiPlayerService(
    IConsoleDisplay consoleDisplay,
    ITempoManager tempoManager,
    INoteProcessor noteProcessor,
    IFileDownloader fileDownloader) : IMidiPlayerService
{
    public async Task PlayMidiFileAsync(string fileUrl)
    {
        IMidiDeviceWrapper? midiDevice = null;
        try
        {
            MidiFile midiFile;

            // Check if it's a URL or local file
            if (Uri.TryCreate(fileUrl, UriKind.Absolute, out Uri? uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                // Download MIDI file from URL
                consoleDisplay.WriteMessage("NET", $"Downloading MIDI file from: {fileUrl}", ConsoleColor.Cyan);

                var midiData = await fileDownloader.DownloadAsync(fileUrl, TimeSpan.FromSeconds(30));
                using var midiStream = new MemoryStream(midiData);

                consoleDisplay.WriteMessage("NET", $"Downloaded {midiData.Length} bytes", ConsoleColor.Green);
                midiFile = new MidiFile(midiStream, false);
            }
            else
            {
                // Load local file
                midiFile = new MidiFile(fileUrl, false);
            }

            consoleDisplay.WriteMessage("SCAN", $"Detected {midiFile.Tracks:X2} tracks, {midiFile.DeltaTicksPerQuarterNote:X4} ticks/quarter", ConsoleColor.Gray);

            // Fix: Check for available MIDI devices before creating MidiOut
            midiDevice = new MidiDeviceWrapper();
            if (midiDevice.NumberOfDevices == 0)
            {
                consoleDisplay.WriteMessage("ERROR", "No MIDI output devices available", ConsoleColor.Red);
                return;
            }

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

            consoleDisplay.WriteMessage("PROC", $"Processed 0x{allEvents.Count:X} MIDI opcodes", ConsoleColor.Green);

            await PlayEventsAsync(allEvents, midiDevice, midiFile.DeltaTicksPerQuarterNote);
        }
        catch (HttpRequestException ex)
        {
            consoleDisplay.WriteMessage("ERROR", $"Network error: {ex.Message}", ConsoleColor.Red);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            consoleDisplay.WriteMessage("ERROR", "Download timeout: The request took too long to complete", ConsoleColor.Red);
        }
        catch (Exception ex)
        {
            consoleDisplay.WriteMessage("ERROR", $"Execution failed: {ex.Message}", ConsoleColor.Red);
        }
        finally
        {
            midiDevice?.Dispose();
        }
    }

    private async Task PlayEventsAsync(List<MidiEventInfo> allEvents, IMidiDeviceWrapper midiDevice, int ticksPerQuarterNote)
    {
        var stopwatch = Stopwatch.StartNew();
        var activeNotes = new HashSet<int>();

        // Build tempo map
        var tempoMap = tempoManager.BuildTempoMap(allEvents);

        consoleDisplay.WriteMessage("EXEC", "Initiating MIDI stream injection...", ConsoleColor.Yellow);
        Thread.Sleep(500);
        consoleDisplay.WriteMessage("LIVE", "REAL-TIME MIDI ANALYSIS", ConsoleColor.Green);

        // Write a divider line
        Console.WriteLine(new string('-', 81), ConsoleColor.DarkGray);

        var playbackStart = stopwatch.Elapsed;

        foreach (var midiEntry in allEvents)
        {
            // Calculate the expected time for this event
            var expectedTime = playbackStart.Add(tempoManager.TicksToTimeSpan(midiEntry.AbsoluteTime, tempoMap, ticksPerQuarterNote));
            var currentTime = stopwatch.Elapsed;

            // Wait until it's time to play this event
            var delayNeeded = expectedTime - currentTime;
            if (delayNeeded > TimeSpan.Zero)
            {
                await Task.Delay(delayNeeded);
            }

            ProcessMidiEvent(midiEntry, midiDevice, stopwatch, activeNotes);
        }

        consoleDisplay.WriteMessage("COMP", "MIDI injection terminated successfully", ConsoleColor.Green);
        consoleDisplay.WriteMessage("STATS", $"Final buffer state: 0x{activeNotes.Count:X2} active notes", ConsoleColor.Gray);
    }

    private void ProcessMidiEvent(MidiEventInfo midiEntry, IMidiDeviceWrapper midiDevice, Stopwatch stopwatch, HashSet<int> activeNotes)
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
                    noteProcessor.DisplayNoteOn(timestamp, noteEvent, activeNotes.Count);
                    midiDevice.Send(MidiMessage.StartNote(noteEvent.NoteNumber, noteEvent.Velocity, noteEvent.Channel).RawData);
                }
                else
                {
                    activeNotes.Remove(noteEvent.NoteNumber);
                    noteProcessor.DisplayNoteOff(timestamp, noteEvent, activeNotes.Count);
                    midiDevice.Send(MidiMessage.StopNote(noteEvent.NoteNumber, noteEvent.Velocity, noteEvent.Channel).RawData);
                }
                break;

            case MidiCommandCode.NoteOff:
                var noteOffEvent = (NoteEvent)midiEntry.Event;
                activeNotes.Remove(noteOffEvent.NoteNumber);
                noteProcessor.DisplayNoteOff(timestamp, noteOffEvent, activeNotes.Count);
                midiDevice.Send(MidiMessage.StopNote(noteOffEvent.NoteNumber, noteOffEvent.Velocity, noteOffEvent.Channel).RawData);
                break;

            case MidiCommandCode.ControlChange:
                var controlEvent = (ControlChangeEvent)midiEntry.Event;
                noteProcessor.DisplayControlChange(timestamp, controlEvent, activeNotes.Count);
                midiDevice.Send(MidiMessage.ChangeControl((int)controlEvent.Controller, controlEvent.ControllerValue, controlEvent.Channel).RawData);
                break;

            case MidiCommandCode.PatchChange:
                var programEvent = (PatchChangeEvent)midiEntry.Event;
                consoleDisplay.WriteMessage("PROG", $"Program Change: {programEvent.Patch}", ConsoleColor.Magenta);
                midiDevice.Send(MidiMessage.ChangePatch(programEvent.Patch, programEvent.Channel).RawData);
                break;
        }

        // Update activity indicator
        consoleDisplay.UpdateActivityIndicator();
    }
}