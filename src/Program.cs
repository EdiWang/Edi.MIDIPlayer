using NAudio.Midi;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Edi.MIDIPlayer;

internal class Program
{
    private static readonly Dictionary<int, string> NoteNames = new()
    {
        { 0, "C" }, { 1, "C#" }, { 2, "D" }, { 3, "D#" }, { 4, "E" }, { 5, "F" },
        { 6, "F#" }, { 7, "G" }, { 8, "G#" }, { 9, "A" }, { 10, "A#" }, { 11, "B" }
    };

    private static readonly char[] ActivityChars = ['█', '▓', '▒', '░', '·'];
    private static int _activityIndex = 0;
    private static readonly Lock _consoleLock = new();

    static async Task Main(string[] args)
    {
        // Check if running on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ERROR: This program is currently Windows only.");
            Console.WriteLine("The MIDI player requires Windows-specific audio subsystems to function properly.");
            Console.ResetColor();
            Environment.Exit(1);
        }

        try
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Clear();

            DisplayHackerBanner();

            string filePath = GetMidiFilePath(args);

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                WriteMessage("ERROR", "0xDEADBEEF", "MIDI file not found or invalid path", ConsoleColor.Red);
                return;
            }

            await PlayMidiFileAsync(filePath);
        }
        catch (Exception ex)
        {
            WriteMessage("FATAL", "0xC0000005", $"Unexpected error: {ex.Message}", ConsoleColor.Red);
        }
        finally
        {
            WriteMessage("SYSTEM", "0x00000000", "Press any key to exit...", ConsoleColor.Yellow);
            Console.ReadKey();
        }
    }

    private static void DisplayHackerBanner()
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.ResetColor();

        WriteMessage("INIT", "0x7FF00000", "EDI.MIDIPLAYER Terminal", ConsoleColor.Cyan);
        Thread.Sleep(500);
        Console.WriteLine();
    }

    private static string GetMidiFilePath(string[] args)
    {
        if (args.Length > 0)
        {
            // WriteMessage("ARGS", "0x00401000", $"Command line injection: {args[0]}", ConsoleColor.Cyan);
            return args[0];
        }

        WriteMessage("INPUT", "0x00000001", "Enter MIDI file path for injection:", ConsoleColor.Yellow);
        Console.Write("     0x1234 > ");
        Console.ForegroundColor = ConsoleColor.White;

        var path = Console.ReadLine()?.Trim('"') ?? string.Empty;
        Console.ResetColor();

        return path;
    }

    private static async Task PlayMidiFileAsync(string filePath)
    {
        try
        {
            // WriteMessage("LOAD", "0x4000C000", $"Reading MIDI bytecode: {Path.GetFileName(filePath)}", ConsoleColor.Cyan);

            var midiFile = new MidiFile(filePath, false);

            WriteMessage("SCAN", "0xBEEFCAFE", $"Detected {midiFile.Tracks:X2} tracks, {midiFile.DeltaTicksPerQuarterNote:X4} ticks/quarter", ConsoleColor.Gray);

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

            WriteMessage("PROC", "0xDEADC0DE", $"Processed 0x{allEvents.Count:X} MIDI opcodes", ConsoleColor.Green);

            await PlayEventsAsync(allEvents, midiOut, midiFile.DeltaTicksPerQuarterNote);
        }
        catch (Exception ex)
        {
            WriteMessage("ERROR", "0xFFFFFFF", $"Execution failed: {ex.Message}", ConsoleColor.Red);
        }
    }

    private static async Task PlayEventsAsync(List<MidiEventInfo> allEvents, MidiOut midiOut, int ticksPerQuarterNote)
    {
        var stopwatch = Stopwatch.StartNew();
        var activeNotes = new HashSet<int>();

        // Build tempo map
        var tempoMap = BuildTempoMap(allEvents, ticksPerQuarterNote);

        WriteMessage("EXEC", "0xC0FFEE", "Initiating MIDI stream injection...", ConsoleColor.Yellow);
        Thread.Sleep(1000);

        Console.WriteLine();
        WriteMessage("LIVE", "0x1337", "REAL-TIME MIDI ANALYSIS", ConsoleColor.Green);

        var playbackStart = stopwatch.Elapsed;

        foreach (var midiEntry in allEvents)
        {
            // Calculate the expected time for this event
            var expectedTime = playbackStart.Add(TicksToTimeSpan(midiEntry.AbsoluteTime, tempoMap, ticksPerQuarterNote));
            var currentTime = stopwatch.Elapsed;

            // Wait until it's time to play this event
            var delayNeeded = expectedTime - currentTime;
            if (delayNeeded > TimeSpan.Zero)
            {
                await Task.Delay(delayNeeded);
            }

            ProcessMidiEvent(midiEntry, midiOut, stopwatch, activeNotes);
        }

        WriteMessage("COMP", "0xABCD", "MIDI injection terminated successfully", ConsoleColor.Green);
        WriteMessage("STATS", "0x5000D000", $"Final buffer state: 0x{activeNotes.Count:X2} active notes", ConsoleColor.Gray);
    }

    private static List<TempoChange> BuildTempoMap(List<MidiEventInfo> allEvents, int ticksPerQuarterNote)
    {
        var tempoMap = new List<TempoChange>
        {
            new() { Tick = 0, MicrosecondsPerQuarterNote = 500000 } // Default 120 BPM
        };

        foreach (var eventInfo in allEvents)
        {
            if (eventInfo.Event is MetaEvent meta && meta.MetaEventType == MetaEventType.SetTempo)
            {
                var tempoEvent = (TempoEvent)meta;
                tempoMap.Add(new TempoChange 
                { 
                    Tick = eventInfo.AbsoluteTime, 
                    MicrosecondsPerQuarterNote = tempoEvent.MicrosecondsPerQuarterNote 
                });

                var bpm = 60000000.0 / tempoEvent.MicrosecondsPerQuarterNote;
                WriteMessage("TEMPO", "0xBEAT", $"BPM: {bpm:F1} (0x{tempoEvent.MicrosecondsPerQuarterNote:X} μs/quarter)", ConsoleColor.Magenta);
            }
        }

        return tempoMap;
    }

    private static TimeSpan TicksToTimeSpan(long ticks, List<TempoChange> tempoMap, int ticksPerQuarterNote)
    {
        var totalMicroseconds = 0.0;
        var currentTick = 0L;

        for (int i = 0; i < tempoMap.Count; i++)
        {
            var tempoChange = tempoMap[i];
            var nextTick = (i + 1 < tempoMap.Count) ? tempoMap[i + 1].Tick : ticks;
            
            if (nextTick > ticks)
                nextTick = ticks;

            if (nextTick > currentTick)
            {
                var ticksInThisSegment = nextTick - currentTick;
                // Use the actual ticks per quarter note from the MIDI file
                var microsecondsPerTick = (double)tempoChange.MicrosecondsPerQuarterNote / ticksPerQuarterNote;
                totalMicroseconds += ticksInThisSegment * microsecondsPerTick;
            }

            currentTick = nextTick;
            if (currentTick >= ticks)
                break;
        }

        return TimeSpan.FromMilliseconds(totalMicroseconds / 1000.0);
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
                var noteName = GetNoteName(noteEvent.NoteNumber);
                var velocityBar = CreateVelocityBar(noteEvent.Velocity);

                lock (_consoleLock)
                {
                    Console.Write("[");
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write(timestamp);
                    Console.ResetColor();
                    Console.Write("] ");

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"0x{noteEvent.NoteNumber:X2}{noteEvent.Channel:X1}{noteEvent.Velocity:X2} ");
                    Console.ResetColor();

                    Console.ForegroundColor = GetNoteColor(noteEvent.NoteNumber);
                    Console.Write("▲ ");
                    Console.ResetColor();

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("NOTE_ON  ");
                    Console.ResetColor();

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write($"{noteName,-4} ");
                    Console.ResetColor();

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write($"CH{noteEvent.Channel + 1:D2} ");
                    Console.ResetColor();

                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.Write($"VEL:0x{noteEvent.Velocity:X2} ");
                    Console.ResetColor();

                    Console.Write(velocityBar);

                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write($" │ HEAP: 0x{activeNotes.Count:X3} │ NOTE: 0x{noteEvent.NoteNumber:X2}");
                    Console.WriteLine();
                    Console.ResetColor();
                }

                midiOut.Send(MidiMessage.StartNote(noteEvent.NoteNumber, noteEvent.Velocity, noteEvent.Channel).RawData);
            }
        }
        else if (midiEntry.Event.CommandCode == MidiCommandCode.NoteOff ||
                 (midiEntry.Event.CommandCode == MidiCommandCode.NoteOn &&
                  ((NoteEvent)midiEntry.Event).Velocity == 0))
        {
            var noteEvent = (NoteEvent)midiEntry.Event;
            activeNotes.Remove(noteEvent.NoteNumber);
            var noteName = GetNoteName(noteEvent.NoteNumber);

            lock (_consoleLock)
            {
                Console.Write("[");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write(timestamp);
                Console.ResetColor();
                Console.Write("] ");

                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($"0x{noteEvent.NoteNumber:X2}{noteEvent.Channel:X1}00 ");
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("▼ ");
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("NOTE_OFF ");
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"{noteName,-4} ");
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"CH{noteEvent.Channel + 1:D2} ");
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.Write("VEL:0x00 ");
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("░░░░░░░░░░");
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write($" │ HEAP: 0x{activeNotes.Count:X3} │ NOTE: 0x{noteEvent.NoteNumber:X2}");
                Console.WriteLine();
                Console.ResetColor();
            }

            midiOut.Send(MidiMessage.StopNote(noteEvent.NoteNumber, noteEvent.Velocity, noteEvent.Channel).RawData);
        }

        // Update activity indicator
        UpdateActivityIndicator();
    }

    private static void UpdateActivityIndicator()
    {
        _activityIndex = (_activityIndex + 1) % ActivityChars.Length;
    }

    private static string GetNoteName(int noteNumber)
    {
        var octave = (noteNumber / 12) - 1;
        var note = NoteNames[noteNumber % 12];
        return $"{note}{octave}";
    }

    private static ConsoleColor GetNoteColor(int noteNumber)
    {
        return (noteNumber % 12) switch
        {
            0 or 2 or 4 or 5 or 7 or 9 or 11 => ConsoleColor.White,  // Natural notes
            _ => ConsoleColor.Magenta  // Sharp/flat notes
        };
    }

    private static string CreateVelocityBar(int velocity)
    {
        var barLength = 10;
        var filledLength = (int)((velocity / 127.0) * barLength);
        var bar = new StringBuilder();

        Console.ForegroundColor = ConsoleColor.Green;
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

    private static void WriteMessage(string type, string hexCode, string message, ConsoleColor color)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");

        Console.Write("[");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(timestamp);
        Console.ResetColor();
        Console.Write("] ");

        Console.Write("[");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write(hexCode);
        Console.ResetColor();
        Console.Write("] ");

        Console.Write("[");
        Console.ForegroundColor = color;
        Console.Write($"{type,-5}");
        Console.ResetColor();
        Console.Write("] ");

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}
