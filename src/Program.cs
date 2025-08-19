using NAudio.Midi;
using System.Diagnostics;
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
        try
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Clear();

            DisplayHackerBanner();

            string filePath = GetMidiFilePath(args);

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                WriteMessage("ERROR", "MIDI file not found or invalid path", ConsoleColor.Red);
                return;
            }

            await PlayMidiFileAsync(filePath);
        }
        catch (Exception ex)
        {
            WriteMessage("FATAL", $"Unexpected error: {ex.Message}", ConsoleColor.Red);
        }
        finally
        {
            WriteMessage("SYSTEM", "Press any key to exit...", ConsoleColor.Yellow);
            Console.ReadKey();
        }
    }

    private static void DisplayHackerBanner()
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.ResetColor();

        WriteMessage("INIT", "EDI.MIDIPLAYER Terminal", ConsoleColor.Cyan);
        WriteMessage("INIT", "Initializing audio subsystems...", ConsoleColor.Gray);
        Thread.Sleep(500);
        Console.WriteLine();
    }

    private static string GetMidiFilePath(string[] args)
    {
        if (args.Length > 0)
        {
            // WriteHackerMessage("INPUT", $"Using command line path: {args[0]}", ConsoleColor.Cyan);
            return args[0];
        }

        WriteMessage("INPUT", "Enter MIDI file path for injection:", ConsoleColor.Yellow);
        Console.Write("     > ");
        Console.ForegroundColor = ConsoleColor.White;

        var path = Console.ReadLine()?.Trim('"') ?? string.Empty;
        Console.ResetColor();

        return path;
    }

    private static async Task PlayMidiFileAsync(string filePath)
    {
        try
        {
            // WriteMessage("LOAD", $"Reading MIDI data from: {Path.GetFileName(filePath)}", ConsoleColor.Cyan);

            var midiFile = new MidiFile(filePath, false);

            WriteMessage("SCAN", $"Detected {midiFile.Tracks} tracks, {midiFile.DeltaTicksPerQuarterNote} ticks/quarter", ConsoleColor.Gray);

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

            WriteMessage("PROC", $"Processed {allEvents.Count} MIDI events", ConsoleColor.Green);

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
                    WriteMessage("TEMPO", $"BPM: {bpm:F1} ({tempo:F0} μs/quarter)", ConsoleColor.Magenta);
                    break;
                }
            }

            await PlayEventsAsync(allEvents, midiOut, tempo, ticksPerQuarterNote);
        }
        catch (Exception ex)
        {
            WriteMessage("ERROR", $"Playback failed: {ex.Message}", ConsoleColor.Red);
        }
    }

    private static async Task PlayEventsAsync(List<MidiEventInfo> allEvents, MidiOut midiOut, double tempo, int ticksPerQuarterNote)
    {
        var stopwatch = Stopwatch.StartNew();
        long lastTime = 0;
        var activeNotes = new HashSet<int>();

        WriteMessage("EXEC", "Initiating MIDI stream injection...", ConsoleColor.Yellow);
        Thread.Sleep(1000);

        Console.WriteLine();
        WriteMessage("LIVE", "REAL-TIME MIDI ANALYSIS", ConsoleColor.Green);

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

        WriteMessage("COMP", "MIDI injection completed successfully", ConsoleColor.Green);
        WriteMessage("STATS", $"Active notes at end: {activeNotes.Count}", ConsoleColor.Gray);
    }

    private static void ProcessMidiEvent(MidiEventInfo midiEntry, MidiOut midiOut, Stopwatch stopwatch, HashSet<int> activeNotes)
    {
        var elapsed = stopwatch.Elapsed;
        var timestamp = $"{elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";

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
                    Console.ForegroundColor = GetNoteColor(noteEvent.NoteNumber);
                    Console.Write("▲");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write($" {timestamp} ");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"NOTE_ON  ");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write($"{noteName,-4} ");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write($"CH{noteEvent.Channel + 1:D2} ");
                    Console.Write(velocityBar);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine($" │ Active: {activeNotes.Count:D3}");
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
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("▼");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($" {timestamp} ");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($"NOTE_OFF ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"{noteName,-4} ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"CH{noteEvent.Channel + 1:D2} ");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("░░░░░░░░░░");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($" │ Active: {activeNotes.Count:D3}");
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

        for (int i = 0; i < barLength; i++)
        {
            if (i < filledLength)
            {
                bar.Append('█');
            }
            else
            {
                bar.Append('░');
            }
        }

        return bar.ToString();
    }

    private static void WriteMessage(string type, string message, ConsoleColor color)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");

        Console.Write("[");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(timestamp);
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
