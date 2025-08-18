using System.Text;

namespace Edi.MIDIPlayer;

public class MidiFileParser
{
    public static (List<MidiEvent> events, int ticksPerQuarter) ParseMidiFile(string filePath)
    {
        var events = new List<MidiEvent>();

        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var reader = new BinaryReader(fileStream);

        // Read MIDI header
        var headerChunk = reader.ReadBytes(4);
        if (!headerChunk.SequenceEqual(Encoding.ASCII.GetBytes("MThd")))
            throw new InvalidOperationException("Invalid MIDI file format");

        var headerLength = ReadBigEndianInt32(reader);
        var format = ReadBigEndianInt16(reader);
        var trackCount = ReadBigEndianInt16(reader);
        var division = ReadBigEndianInt16(reader);

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"MIDI Header Analysis: Format {format} | Tracks {trackCount} | Division {division} PPQ");
        Console.ResetColor();

        // Read tracks
        for (int track = 0; track < trackCount; track++)
        {
            ParseTrack(reader, events, track);
        }

        // Sort events by absolute ticks for proper timing
        events = events.OrderBy(e => e.Ticks).ToList();

        return (events, division);
    }

    private static void ParseTrack(BinaryReader reader, List<MidiEvent> events, int trackNumber)
    {
        var trackHeader = reader.ReadBytes(4);
        if (!trackHeader.SequenceEqual(Encoding.ASCII.GetBytes("MTrk")))
            return;

        var trackLength = ReadBigEndianInt32(reader);
        var trackEnd = reader.BaseStream.Position + trackLength;

        int currentTicks = 0;
        byte runningStatus = 0;

        while (reader.BaseStream.Position < trackEnd)
        {
            var deltaTime = ReadVariableLength(reader);
            currentTicks += deltaTime;

            var eventByte = reader.ReadByte();

            // Handle running status
            if ((eventByte & 0x80) == 0)
            {
                reader.BaseStream.Position--;
                eventByte = runningStatus;
            }
            else
            {
                runningStatus = eventByte;
            }

            var midiEvent = ParseEvent(reader, eventByte, currentTicks);
            if (midiEvent != null)
            {
                events.Add(midiEvent);
            }
        }
    }

    private static MidiEvent? ParseEvent(BinaryReader reader, byte eventType, int ticks)
    {
        var eventData = new List<byte> { eventType };

        if ((eventType & 0xF0) >= 0x80 && (eventType & 0xF0) <= 0xE0)
        {
            // Standard MIDI channel message
            eventData.Add(reader.ReadByte()); // Data byte 1

            if ((eventType & 0xF0) != 0xC0 && (eventType & 0xF0) != 0xD0)
            {
                eventData.Add(reader.ReadByte()); // Data byte 2 (if applicable)
            }
        }
        else if (eventType == 0xFF)
        {
            // Meta event
            var metaType = reader.ReadByte();
            var length = ReadVariableLength(reader);

            eventData.Add(metaType);
            for (int i = 0; i < length; i++)
            {
                eventData.Add(reader.ReadByte());
            }
        }
        else if (eventType == 0xF0 || eventType == 0xF7)
        {
            // SysEx event
            var length = ReadVariableLength(reader);
            for (int i = 0; i < length; i++)
            {
                eventData.Add(reader.ReadByte());
            }
        }

        return new MidiEvent
        {
            Ticks = ticks,
            EventType = eventType,
            Data = [.. eventData]
        };
    }

    private static int ReadVariableLength(BinaryReader reader)
    {
        int value = 0;
        byte currentByte;

        do
        {
            currentByte = reader.ReadByte();
            value = (value << 7) | (currentByte & 0x7F);
        } while ((currentByte & 0x80) != 0);

        return value;
    }

    private static int ReadBigEndianInt32(BinaryReader reader)
    {
        var bytes = reader.ReadBytes(4);
        Array.Reverse(bytes);
        return BitConverter.ToInt32(bytes, 0);
    }

    private static short ReadBigEndianInt16(BinaryReader reader)
    {
        var bytes = reader.ReadBytes(2);
        Array.Reverse(bytes);
        return BitConverter.ToInt16(bytes, 0);
    }
}