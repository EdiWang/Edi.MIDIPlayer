namespace Edi.MIDIPlayer;

public class MidiEventDisplay(ConsoleLogger logger)
{
    public async Task DisplayMidiDataAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            logger.PrintMidiAnalysisHeader();

            var (midiEvents, ticksPerQuarter) = MidiFileParser.ParseMidiFile(filePath);
            var startTime = DateTime.Now;
            double currentTempo = 500000.0; // Default 120 BPM (500,000 microseconds per quarter note)
            double totalTicks = 0;

            foreach (var midiEvent in midiEvents)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                // Update tempo if tempo change event
                if (midiEvent.EventType == 0xFF && midiEvent.Data.Length >= 5 && midiEvent.Data[1] == 0x51)
                {
                    currentTempo = (midiEvent.Data[2] << 16) | (midiEvent.Data[3] << 8) | midiEvent.Data[4];
                }

                // Calculate accurate timing
                var ticksSinceStart = midiEvent.Ticks - totalTicks;
                totalTicks = midiEvent.Ticks;

                // Convert ticks to milliseconds using current tempo
                var eventDelayMs = (ticksSinceStart * currentTempo) / (ticksPerQuarter * 1000.0);
                var eventTime = TimeSpan.FromMilliseconds(totalTicks * currentTempo / (ticksPerQuarter * 1000.0));

                if (eventDelayMs > 0)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(eventDelayMs), cancellationToken);
                }

                DisplayMidiEvent(midiEvent, eventTime);
            }

            // Wait for cancellation signal before marking complete
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(100, cancellationToken);
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("MIDI stream analysis complete - All events processed");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            logger.PrintSystemStatus("MIDI Parser", "EXCEPTION", ConsoleColor.Red);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"🚨 PARSER ERROR: {ex.Message}");
            Console.ResetColor();
        }
    }

    private static void DisplayMidiEvent(MidiEvent midiEvent, TimeSpan timestamp)
    {
        var hexData = string.Join(" ", midiEvent.Data.Select(b => $"{b:X2}"));
        var description = GetMidiEventDescription(midiEvent);
        var eventIcon = GetEventIcon(midiEvent.EventType);

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"[{timestamp:mm\\:ss\\.fff}]");
        Console.ResetColor();
        Console.Write(" ");
        Console.ForegroundColor = GetEventColor(midiEvent.EventType);
        Console.Write($"{eventIcon} {midiEvent.EventType:X2}:");
        Console.ResetColor();
        Console.Write($" {hexData,-10} ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"{description}");
        Console.ResetColor();

        Console.WriteLine();
    }

    private static string GetEventIcon(byte eventType)
    {
        return (eventType & 0xF0) switch
        {
            0x80 => "🔴", // Note Off
            0x90 => "🟢", // Note On
            0xA0 => "🟡", // Aftertouch
            0xB0 => "🔵", // Control Change
            0xC0 => "🟣", // Program Change
            0xD0 => "🟠", // Channel Pressure
            0xE0 => "⚪", // Pitch Bend
            0xF0 => "⚙️",  // System/Meta events
            _ => "⚫"
        };
    }

    private static ConsoleColor GetEventColor(byte eventType)
    {
        return (eventType & 0xF0) switch
        {
            0x80 => ConsoleColor.Red,     // Note Off
            0x90 => ConsoleColor.Green,   // Note On
            0xA0 => ConsoleColor.Yellow,  // Aftertouch
            0xB0 => ConsoleColor.Cyan,    // Control Change
            0xC0 => ConsoleColor.Magenta, // Program Change
            0xD0 => ConsoleColor.Blue,    // Channel Pressure
            0xE0 => ConsoleColor.White,   // Pitch Bend
            0xF0 => ConsoleColor.Gray,    // System/Meta events
            _ => ConsoleColor.DarkGray
        };
    }

    private static string GetMidiEventDescription(MidiEvent midiEvent)
    {
        if (midiEvent.Data.Length < 2) return "Invalid Event";

        return (midiEvent.EventType & 0xF0) switch
        {
            0x80 => $"Note Off - CH {(midiEvent.EventType & 0x0F) + 1}, NOTE {midiEvent.Data[1]}, VEL {midiEvent.Data[2]}",
            0x90 => $"CH {(midiEvent.EventType & 0x0F) + 1} | NOTE {midiEvent.Data[1]} | VEL {midiEvent.Data[2]}",
            0xA0 => $"Aftertouch - CH {(midiEvent.EventType & 0x0F) + 1}, NOTE {midiEvent.Data[1]}, Pressure {midiEvent.Data[2]}",
            0xB0 => $"Control Change - CH {(midiEvent.EventType & 0x0F) + 1}, CC {midiEvent.Data[1]}, Val {midiEvent.Data[2]}",
            0xC0 => $"Program Change - CH {(midiEvent.EventType & 0x0F) + 1}, Program {midiEvent.Data[1]}",
            0xD0 => $"Channel Pressure - CH {(midiEvent.EventType & 0x0F) + 1}, Pressure {midiEvent.Data[1]}",
            0xE0 => $"Pitch Bend - Ch{(midiEvent.EventType & 0x0F) + 1}, Value {((midiEvent.Data.Length > 2 ? midiEvent.Data[2] : 0) << 7) | midiEvent.Data[1]}",
            0xF0 when midiEvent.EventType == 0xFF => GetMetaEventDescription(midiEvent.Data),
            _ => "Unknown Event"
        };
    }

    private static string GetMetaEventDescription(byte[] data)
    {
        if (data.Length < 2) return "Invalid Meta Event";

        return data[1] switch
        {
            0x00 => "Sequence Number",
            0x01 => "Text Event",
            0x02 => "Copyright Notice",
            0x03 => "Track Name",
            0x04 => "Instrument Name",
            0x05 => "Lyric",
            0x06 => "Marker",
            0x07 => "Cue Point",
            0x20 => "MIDI Channel Prefix",
            0x2F => "End of Track",
            0x51 => "Set Tempo",
            0x54 => "SMPTE Offset",
            0x58 => "Time Signature",
            0x59 => "Key Signature",
            0x7F => "Sequencer Specific",
            _ => $"Meta Event {data[1]:X2}"
        };
    }
}