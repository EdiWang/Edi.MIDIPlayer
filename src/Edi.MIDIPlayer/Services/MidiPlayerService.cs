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
    private const double PREVIEW_TIME_MS = 2500; // 音符提前2.5秒显示

    public async Task PlayMidiFileAsync(string fileUrl)
    {
        IMidiDeviceWrapper? midiDevice = null;
        try
        {
            MidiFile midiFile;

            if (Uri.TryCreate(fileUrl, UriKind.Absolute, out Uri? uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                consoleDisplay.WriteMessage("NET", $"Downloading MIDI file from: {fileUrl}", ConsoleColor.Cyan);
                var midiData = await fileDownloader.DownloadAsync(fileUrl, TimeSpan.FromSeconds(30));
                using var midiStream = new MemoryStream(midiData);
                consoleDisplay.WriteMessage("NET", $"Downloaded {midiData.Length} bytes", ConsoleColor.Green);
                midiFile = new MidiFile(midiStream, false);
            }
            else
            {
                midiFile = new MidiFile(fileUrl, false);
            }

            consoleDisplay.WriteMessage("SCAN", $"Detected {midiFile.Tracks:X2} tracks, {midiFile.DeltaTicksPerQuarterNote:X4} ticks/quarter", ConsoleColor.Gray);

            midiDevice = new MidiDeviceWrapper();
            if (midiDevice.NumberOfDevices == 0)
            {
                consoleDisplay.WriteMessage("ERROR", "No MIDI output devices available", ConsoleColor.Red);
                return;
            }

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
        var tempoMap = tempoManager.BuildTempoMap(allEvents);

        consoleDisplay.WriteMessage("EXEC", "Initiating MIDI stream injection...", ConsoleColor.Yellow);
        Thread.Sleep(500);
        consoleDisplay.WriteMessage("LIVE", "REAL-TIME MIDI ANALYSIS", ConsoleColor.Green);

        var playbackStart = stopwatch.Elapsed;

        // 预先发送音符预告
        _ = Task.Run(async () =>
        {
            foreach (var midiEntry in allEvents)
            {
                if (midiEntry.Event.CommandCode == MidiCommandCode.NoteOn)
                {
                    var noteEvent = (NoteEvent)midiEntry.Event;
                    if (noteEvent.Velocity > 0)
                    {
                        var eventTime = tempoManager.TicksToTimeSpan(midiEntry.AbsoluteTime, tempoMap, ticksPerQuarterNote);
                        var delayMs = eventTime.TotalMilliseconds;

                        // 等待到预告时间
                        var previewDelay = delayMs - PREVIEW_TIME_MS;
                        if (previewDelay > 0)
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(previewDelay));
                        }

                        // 发送预告，告诉前端这个音符将在 PREVIEW_TIME_MS 毫秒后播放
                        noteProcessor.SendNotePreview(noteEvent.NoteNumber, noteEvent.Velocity, noteEvent.Channel, PREVIEW_TIME_MS);
                    }
                }
            }
        });

        // 正常播放事件
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

        consoleDisplay.UpdateActivityIndicator();
    }
}