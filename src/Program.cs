using NAudio.Midi;

namespace Edi.MIDIPlayer;

internal class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        string filePath = args.Length > 0 ? args[0] : GetMidiFilePath();

        if (string.IsNullOrEmpty(filePath))
        {
            Console.WriteLine("No MIDI file specified. Exiting...");
            return;
        }

        var midiFile = new MidiFile(filePath, false);

        using var midiOut = new MidiOut(0);

        // Gather all events from all tracks, in absolute (chronological) order
        var allEvents = new List<MidiEventInfo>();
        for (int track = 0; track < midiFile.Tracks; track++)
        {
            foreach (MidiEvent midiEvent in midiFile.Events[track])
            {
                allEvents.Add(new MidiEventInfo { AbsoluteTime = midiEvent.AbsoluteTime, Event = midiEvent });
            }
        }
        allEvents = allEvents.OrderBy(e => e.AbsoluteTime).ToList();

        // Get initial tempo (default to 500ms/quarter note if missing)
        int ticksPerQuarterNote = midiFile.DeltaTicksPerQuarterNote;
        double tempo = 500000; // microseconds per quarter note (default 120 BPM)

        // Find tempo if specified
        foreach (var ev in allEvents)
        {
            if (ev.Event is MetaEvent meta && meta.MetaEventType == MetaEventType.SetTempo)
            {
                tempo = ((TempoEvent)meta).MicrosecondsPerQuarterNote;
                break;
            }
        }

        long lastTime = 0;
        var startTime = DateTime.Now;

        Console.WriteLine("MIDI Visualizer Start!\n");

        foreach (var midiEntry in allEvents)
        {
            // Calculate real-time delay
            long deltaTicks = midiEntry.AbsoluteTime - lastTime;
            double msPerTick = tempo / 1000.0 / ticksPerQuarterNote;
            int delayMs = (int)(deltaTicks * msPerTick);

            if (delayMs > 0)
            {
                Thread.Sleep(delayMs);
            }
            lastTime = midiEntry.AbsoluteTime;

            if (midiEntry.Event.CommandCode == MidiCommandCode.NoteOn)
            {
                var noteEvent = (NoteEvent)midiEntry.Event;
                if (noteEvent.Velocity > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[+{(DateTime.Now - startTime).TotalSeconds:F2}s] Note ON : {noteEvent.NoteNumber} Velocity: {noteEvent.Velocity}");
                    Console.ResetColor();
                }

                midiOut.Send(MidiMessage.StartNote(noteEvent.NoteNumber, noteEvent.Velocity, noteEvent.Channel).RawData);
            }
            else if (midiEntry.Event.CommandCode == MidiCommandCode.NoteOff ||
                     (midiEntry.Event.CommandCode == MidiCommandCode.NoteOn &&
                      ((NoteEvent)midiEntry.Event).Velocity == 0))
            {
                var noteEvent = (NoteEvent)midiEntry.Event;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[+{(DateTime.Now - startTime).TotalSeconds:F2}s] Note OFF: {noteEvent.NoteNumber}");
                Console.ResetColor();

                midiOut.Send(MidiMessage.StopNote(noteEvent.NoteNumber, noteEvent.Velocity, noteEvent.Channel).RawData);
            }
        }

        Console.WriteLine("\nDone!");
    }

    private static string GetMidiFilePath()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("[INPUT] Enter MIDI file path: ");
        Console.ResetColor();

        var path = Console.ReadLine();
        return path?.Trim('"') ?? string.Empty;
    }
}
