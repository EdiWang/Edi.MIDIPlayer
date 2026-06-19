using Edi.MIDIPlayer.Interfaces;
using Edi.MIDIPlayer.Models;
using NAudio.Midi;
using System.Diagnostics;

namespace Edi.MIDIPlayer.Services;

public class MidiPlayerService(
    IDisplayService displayService,
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

            if (Uri.TryCreate(fileUrl, UriKind.Absolute, out Uri? uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                if (!IsRemoteMidiUrl(uri))
                {
                    displayService.WriteMessage("ERROR", "Remote MIDI URLs must end with .mid or .midi", ConsoleColor.Red);
                    return;
                }

                displayService.WriteMessage("NET", $"Downloading MIDI file from: {fileUrl}", ConsoleColor.Cyan);
                var midiData = await fileDownloader.DownloadAsync(fileUrl, TimeSpan.FromSeconds(30));
                using var midiStream = new MemoryStream(midiData);
                displayService.WriteMessage("NET", $"Downloaded {midiData.Length} bytes", ConsoleColor.Green);
                midiFile = new MidiFile(midiStream, false);
            }
            else
            {
                midiFile = new MidiFile(fileUrl, false);
            }

            displayService.WriteMessage("SCAN", $"Detected {midiFile.Tracks:X2} tracks, {midiFile.DeltaTicksPerQuarterNote:X4} ticks/quarter", ConsoleColor.Gray);

            if (MidiDeviceWrapper.AvailableDeviceCount == 0)
            {
                displayService.WriteMessage("ERROR", "No MIDI output devices available", ConsoleColor.Red);
                return;
            }

            midiDevice = new MidiDeviceWrapper();

            var allEvents = new List<MidiEventInfo>();
            for (int track = 0; track < midiFile.Tracks; track++)
            {
                foreach (MidiEvent midiEvent in midiFile.Events[track])
                {
                    allEvents.Add(new MidiEventInfo { AbsoluteTime = midiEvent.AbsoluteTime, Event = midiEvent });
                }
            }
            allEvents = [.. allEvents.OrderBy(e => e.AbsoluteTime)];

            displayService.WriteMessage("PROC", $"Processed 0x{allEvents.Count:X} MIDI opcodes", ConsoleColor.Green);

            await PlayEventsAsync(allEvents, midiDevice, midiFile.DeltaTicksPerQuarterNote);
        }
        catch (HttpRequestException ex)
        {
            displayService.WriteMessage("ERROR", $"Network error: {ex.Message}", ConsoleColor.Red);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            displayService.WriteMessage("ERROR", "Download timeout: The request took too long to complete", ConsoleColor.Red);
        }
        catch (TimeoutException ex)
        {
            displayService.WriteMessage("ERROR", $"Download timeout: {ex.Message}", ConsoleColor.Red);
        }
        catch (FileDownloadException ex)
        {
            displayService.WriteMessage("ERROR", ex.Message, ConsoleColor.Red);
        }
        catch (Exception ex)
        {
            displayService.WriteMessage("ERROR", $"Execution failed: {ex.Message}", ConsoleColor.Red);
        }
        finally
        {
            midiDevice?.Dispose();
        }
    }

    private static bool IsRemoteMidiUrl(Uri uri)
    {
        var extension = Path.GetExtension(uri.AbsolutePath);
        return extension.Equals(".mid", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".midi", StringComparison.OrdinalIgnoreCase);
    }

    private async Task PlayEventsAsync(List<MidiEventInfo> allEvents, IMidiDeviceWrapper midiDevice, int ticksPerQuarterNote)
    {
        var stopwatch = Stopwatch.StartNew();
        var activeNotes = new ActiveNoteTracker();
        var tempoMap = tempoManager.BuildTempoMap(allEvents);

        displayService.WriteMessage("EXEC", "Initiating MIDI stream injection...", ConsoleColor.Yellow);
        Thread.Sleep(500);
        displayService.WriteMessage("LIVE", "REAL-TIME MIDI ANALYSIS", ConsoleColor.Green);

        var playbackStart = stopwatch.Elapsed;

        foreach (var midiEntry in allEvents)
        {
            var expectedTime = playbackStart.Add(tempoManager.TicksToTimeSpan(midiEntry.AbsoluteTime, tempoMap, ticksPerQuarterNote));
            var currentTime = stopwatch.Elapsed;

            var delayNeeded = expectedTime - currentTime;
            if (delayNeeded > TimeSpan.Zero)
            {
                await Task.Delay(delayNeeded);
            }

            ProcessMidiEvent(midiEntry, midiDevice, stopwatch, activeNotes);
        }

        displayService.WriteMessage("COMP", "MIDI injection terminated successfully", ConsoleColor.Green);
        displayService.WriteMessage("STATS", $"Final buffer state: 0x{activeNotes.ActiveCount:X2} active notes", ConsoleColor.Gray);
    }

    private void ProcessMidiEvent(MidiEventInfo midiEntry, IMidiDeviceWrapper midiDevice, Stopwatch stopwatch, ActiveNoteTracker activeNotes)
    {
        var elapsed = stopwatch.Elapsed;
        var timestamp = $"{elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}.{elapsed.Milliseconds:D3}";

        switch (midiEntry.Event.CommandCode)
        {
            case MidiCommandCode.NoteOn:
                var noteEvent = (NoteEvent)midiEntry.Event;
                if (noteEvent.Velocity > 0)
                {
                    activeNotes.NoteOn(noteEvent.Channel, noteEvent.NoteNumber);
                    noteProcessor.DisplayNoteOn(timestamp, noteEvent, activeNotes.ActiveCount);
                    midiDevice.Send(MidiMessage.StartNote(noteEvent.NoteNumber, noteEvent.Velocity, noteEvent.Channel).RawData);
                }
                else
                {
                    activeNotes.NoteOff(noteEvent.Channel, noteEvent.NoteNumber);
                    noteProcessor.DisplayNoteOff(timestamp, noteEvent, activeNotes.ActiveCount);
                    midiDevice.Send(MidiMessage.StopNote(noteEvent.NoteNumber, noteEvent.Velocity, noteEvent.Channel).RawData);
                }
                break;

            case MidiCommandCode.NoteOff:
                var noteOffEvent = (NoteEvent)midiEntry.Event;
                activeNotes.NoteOff(noteOffEvent.Channel, noteOffEvent.NoteNumber);
                noteProcessor.DisplayNoteOff(timestamp, noteOffEvent, activeNotes.ActiveCount);
                midiDevice.Send(MidiMessage.StopNote(noteOffEvent.NoteNumber, noteOffEvent.Velocity, noteOffEvent.Channel).RawData);
                break;

            case MidiCommandCode.ControlChange:
                var controlEvent = (ControlChangeEvent)midiEntry.Event;
                noteProcessor.DisplayControlChange(timestamp, controlEvent, activeNotes.ActiveCount);
                midiDevice.Send(MidiMessage.ChangeControl((int)controlEvent.Controller, controlEvent.ControllerValue, controlEvent.Channel).RawData);
                break;

            case MidiCommandCode.PatchChange:
                var programEvent = (PatchChangeEvent)midiEntry.Event;
                displayService.WriteMessage("PROG", $"Program Change: {programEvent.Patch}", ConsoleColor.Magenta);
                midiDevice.Send(MidiMessage.ChangePatch(programEvent.Patch, programEvent.Channel).RawData);
                break;
        }

        displayService.UpdateActivityIndicator();
    }
}
