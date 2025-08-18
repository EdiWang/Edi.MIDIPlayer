using System.Buffers;
using System.Collections.Concurrent;
using System.Text;

namespace Edi.MIDIPlayer;

public class MidiEventDisplay(ConsoleLogger logger)
{
    private readonly ConcurrentQueue<DisplayEvent> _displayQueue = new();
    private readonly ArrayPool<char> _charPool = ArrayPool<char>.Shared;
    private readonly StringBuilder _stringBuilder = new(256);
    
    // Pre-computed lookup tables for better performance
    private static readonly Dictionary<byte, string> _eventIcons = new()
    {
        { 0x80, "🔴" }, { 0x90, "🟢" }, { 0xA0, "🟡" }, { 0xB0, "🔵" },
        { 0xC0, "🟣" }, { 0xD0, "🟠" }, { 0xE0, "⚪" }, { 0xF0, "⚙️" }
    };
    
    private static readonly Dictionary<byte, ConsoleColor> _eventColors = new()
    {
        { 0x80, ConsoleColor.Red }, { 0x90, ConsoleColor.Green }, { 0xA0, ConsoleColor.Yellow },
        { 0xB0, ConsoleColor.Cyan }, { 0xC0, ConsoleColor.Magenta }, { 0xD0, ConsoleColor.Blue },
        { 0xE0, ConsoleColor.White }, { 0xF0, ConsoleColor.Gray }
    };

    public async Task DisplayMidiDataAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            logger.PrintMidiAnalysisHeader();

            // Parse file with streaming approach
            var (midiEvents, ticksPerQuarter) = await ParseMidiFileStreamingAsync(filePath, cancellationToken);
            
            // Start display worker task
            var displayTask = ProcessDisplayQueueAsync(cancellationToken);
            
            double currentTempo = 500000.0;
            double totalTicks = 0;
            var lastDisplayTime = DateTime.UtcNow;
            const int batchSize = 10; // Process events in batches
            var eventBatch = new List<MidiEvent>(batchSize);

            foreach (var midiEvent in midiEvents)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                // Update tempo if tempo change event
                if (midiEvent.EventType == 0xFF && midiEvent.Data.Length >= 5 && midiEvent.Data[1] == 0x51)
                {
                    currentTempo = (midiEvent.Data[2] << 16) | (midiEvent.Data[3] << 8) | midiEvent.Data[4];
                }

                var ticksSinceStart = midiEvent.Ticks - totalTicks;
                totalTicks = midiEvent.Ticks;

                var eventDelayMs = (ticksSinceStart * currentTempo) / (ticksPerQuarter * 1000.0);
                var eventTime = TimeSpan.FromMilliseconds(totalTicks * currentTempo / (ticksPerQuarter * 1000.0));

                eventBatch.Add(midiEvent);

                // Process events in batches to reduce overhead
                if (eventBatch.Count >= batchSize || eventDelayMs > 50)
                {
                    await ProcessEventBatch(eventBatch, eventTime, eventDelayMs, cancellationToken);
                    eventBatch.Clear();
                }
            }

            // Process remaining events
            if (eventBatch.Count > 0)
            {
                var eventTime = TimeSpan.FromMilliseconds(totalTicks * currentTempo / (ticksPerQuarter * 1000.0));
                await ProcessEventBatch(eventBatch, eventTime, 0, cancellationToken);
            }

            await displayTask;
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelled
        }
        catch (Exception ex)
        {
            logger.PrintSystemStatus("MIDI Parser", "EXCEPTION", ConsoleColor.Red);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"🚨 PARSER ERROR: {ex.Message}");
            Console.ResetColor();
        }
    }

    private async Task ProcessEventBatch(List<MidiEvent> events, TimeSpan eventTime, double delayMs, CancellationToken cancellationToken)
    {
        if (delayMs > 0)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(delayMs), cancellationToken);
        }

        foreach (var midiEvent in events)
        {
            var displayEvent = new DisplayEvent(midiEvent, eventTime);
            _displayQueue.Enqueue(displayEvent);
        }
    }

    private async Task ProcessDisplayQueueAsync(CancellationToken cancellationToken)
    {
        const int maxBatchSize = 20;
        var displayEvents = new List<DisplayEvent>(maxBatchSize);

        while (!cancellationToken.IsCancellationRequested)
        {
            // Batch dequeue for better performance
            while (_displayQueue.TryDequeue(out var displayEvent) && displayEvents.Count < maxBatchSize)
            {
                displayEvents.Add(displayEvent);
            }

            if (displayEvents.Count > 0)
            {
                DisplayEventBatch(displayEvents);
                displayEvents.Clear();
            }

            await Task.Delay(16, cancellationToken); // ~60 FPS update rate
        }
    }

    private void DisplayEventBatch(List<DisplayEvent> events)
    {
        foreach (var displayEvent in events)
        {
            DisplayMidiEventOptimized(displayEvent.Event, displayEvent.Timestamp);
        }
    }

    private void DisplayMidiEventOptimized(MidiEvent midiEvent, TimeSpan timestamp)
    {
        // Pre-allocate and reuse StringBuilder
        _stringBuilder.Clear();
        
        // Build hex data more efficiently
        var hexData = _charPool.Rent(midiEvent.Data.Length * 3);
        try
        {
            int hexIndex = 0;
            for (int i = 0; i < midiEvent.Data.Length; i++)
            {
                if (i > 0) hexData[hexIndex++] = ' ';
                var hex = midiEvent.Data[i].ToString("X2");
                hex.CopyTo(0, hexData, hexIndex, 2);
                hexIndex += 2;
            }

            var hexString = new string(hexData, 0, hexIndex);
            var description = GetMidiEventDescriptionOptimized(midiEvent);
            var eventType = (byte)(midiEvent.EventType & 0xF0);
            
            var eventIcon = _eventIcons.GetValueOrDefault(eventType, "⚫");
            var eventColor = _eventColors.GetValueOrDefault(eventType, ConsoleColor.DarkGray);

            // Single console write operation for better performance
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"[{timestamp:mm\\:ss\\.fff}] ");
            Console.ForegroundColor = eventColor;
            Console.Write($"{eventIcon} {midiEvent.EventType:X2}: ");
            Console.ResetColor();
            Console.Write($"{hexString,-10} ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(description);
            Console.ResetColor();
        }
        finally
        {
            _charPool.Return(hexData);
        }
    }

    private static string GetMidiEventDescriptionOptimized(MidiEvent midiEvent)
    {
        if (midiEvent.Data.Length < 2) return "Invalid Event";

        var eventType = (byte)(midiEvent.EventType & 0xF0);
        var channel = (midiEvent.EventType & 0x0F) + 1;

        return eventType switch
        {
            0x80 or 0x90 => $"CH {channel} | NOTE {midiEvent.Data[1]} | VEL {midiEvent.Data[2]}",
            0xA0 => $"CH {channel} | NOTE {midiEvent.Data[1]} | Pressure {midiEvent.Data[2]}",
            0xB0 => $"CH {channel} | CC {midiEvent.Data[1]} | VAL {midiEvent.Data[2]}",
            0xC0 => $"CH {channel} | Program {midiEvent.Data[1]}",
            0xD0 => $"CH {channel} | Pressure {midiEvent.Data[1]}",
            0xE0 => $"CH {channel} | Value {((midiEvent.Data.Length > 2 ? midiEvent.Data[2] : 0) << 7) | midiEvent.Data[1]}",
            0xF0 when midiEvent.EventType == 0xFF => GetMetaEventDescriptionOptimized(midiEvent.Data),
            _ => "Unknown Event"
        };
    }

    private static string GetMetaEventDescriptionOptimized(ReadOnlySpan<byte> data)
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

    private static async Task<(IEnumerable<MidiEvent> events, int ticksPerQuarter)> ParseMidiFileStreamingAsync(string filePath, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return MidiFileParser.ParseMidiFile(filePath);
        }, cancellationToken);
    }

    private readonly record struct DisplayEvent(MidiEvent Event, TimeSpan Timestamp);
}