using System.Buffers;
using System.Collections.Concurrent;
using System.Text;

namespace Edi.MIDIPlayer;

public class MidiEventDisplay(ConsoleLogger logger)
{
    private readonly ArrayPool<char> _charPool = ArrayPool<char>.Shared;
    private readonly StringBuilder _stringBuilder = new(256);
    private readonly Random _random = new();
    
    // Animation state
    private int _animationFrame = 0;
    private DateTime _lastFrameTime = DateTime.UtcNow;
    
    // Pre-computed lookup tables for better performance
    private static readonly Dictionary<byte, string[]> _eventIconAnimations = new()
    {
        { 0x80, new[] { "🔴", "🟤", "⚫", "🔴" } }, // Note Off - fade effect
        { 0x90, new[] { "🟢", "💚", "✨", "🌟", "🟢" } }, // Note On - sparkle effect
        { 0xA0, new[] { "🟡", "🟨", "⭐", "🟡" } }, // Polyphonic pressure
        { 0xB0, new[] { "🔵", "🔷", "💎", "🔵" } }, // Control change
        { 0xC0, new[] { "🟣", "🟪", "💜", "🟣" } }, // Program change
        { 0xD0, new[] { "🟠", "🟧", "🔶", "🟠" } }, // Channel pressure
        { 0xE0, new[] { "⚪", "⭕", "🌙", "⚪" } }, // Pitch bend
        { 0xF0, new[] { "⚙️", "🔧", "🛠️", "⚙️" } } // System/Meta
    };
    
    private static readonly Dictionary<byte, ConsoleColor[]> _eventColorAnimations = new()
    {
        { 0x80, new[] { ConsoleColor.Red, ConsoleColor.DarkRed, ConsoleColor.Black, ConsoleColor.Red } },
        { 0x90, new[] { ConsoleColor.Green, ConsoleColor.Cyan, ConsoleColor.White, ConsoleColor.Yellow, ConsoleColor.Green } },
        { 0xA0, new[] { ConsoleColor.Yellow, ConsoleColor.DarkYellow, ConsoleColor.White, ConsoleColor.Yellow } },
        { 0xB0, new[] { ConsoleColor.Cyan, ConsoleColor.Blue, ConsoleColor.White, ConsoleColor.Cyan } },
        { 0xC0, new[] { ConsoleColor.Magenta, ConsoleColor.DarkMagenta, ConsoleColor.White, ConsoleColor.Magenta } },
        { 0xD0, new[] { ConsoleColor.Blue, ConsoleColor.DarkBlue, ConsoleColor.Cyan, ConsoleColor.Blue } },
        { 0xE0, new[] { ConsoleColor.White, ConsoleColor.Gray, ConsoleColor.DarkGray, ConsoleColor.White } },
        { 0xF0, new[] { ConsoleColor.Gray, ConsoleColor.DarkGray, ConsoleColor.White, ConsoleColor.Gray } }
    };

    // Visual intensity based on velocity/value
    private static readonly string[] _intensityBars = new[]
    {
        "▁", "▂", "▃", "▄", "▅", "▆", "▇", "█"
    };

    private static readonly string[] _sparkleEffects = new[]
    {
        "✨", "⭐", "🌟", "💫", "⚡", "🔥", "💥", "🎆"
    };

    public async Task DisplayMidiDataAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            // Enhanced header with animation
            await DisplayAnimatedHeaderAsync();
            logger.PrintMidiAnalysisHeader();

            // Parse file and display events with timing
            var (midiEvents, ticksPerQuarter) = await ParseMidiFileStreamingAsync(filePath, cancellationToken);
            
            double currentTempo = 500000.0; // Default MIDI tempo (120 BPM)
            long previousTicks = 0;
            var startTime = DateTime.UtcNow;

            foreach (var midiEvent in midiEvents)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                // Handle tempo changes
                if (midiEvent.EventType == 0xFF && midiEvent.Data.Length >= 5 && midiEvent.Data[1] == 0x51)
                {
                    currentTempo = (midiEvent.Data[2] << 16) | (midiEvent.Data[3] << 8) | midiEvent.Data[4];
                    await DisplayTempoChangeEffectAsync(currentTempo);
                }

                // Calculate timing based on delta ticks
                long deltaTicks = midiEvent.Ticks - previousTicks;
                previousTicks = midiEvent.Ticks;

                // Convert ticks to milliseconds
                double delayMs = (deltaTicks * currentTempo) / (ticksPerQuarter * 1000.0);

                // Wait for the appropriate time
                if (delayMs > 0)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(delayMs), cancellationToken);
                }

                // Calculate current playback time
                var currentTime = DateTime.UtcNow - startTime;
                
                // Display the event
                DisplayMidiEventOptimized(midiEvent, currentTime);
            }

            await DisplayEndAnimationAsync();
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelled
        }
        catch (Exception ex)
        {
            logger.PrintSystemStatus("MIDI Parser", "EXCEPTION", ConsoleColor.Red);
            await DisplayErrorEffectAsync(ex.Message);
        }
    }

    private async Task DisplayAnimatedHeaderAsync()
    {
        var headerFrames = new[]
        {
            "🎵 MIDI Event Display Starting... 🎵",
            "🎶 MIDI Event Display Starting... 🎶",
            "🎼 MIDI Event Display Starting... 🎼",
            "🎹 MIDI Event Display Starting... 🎹"
        };

        for (int i = 0; i < headerFrames.Length; i++)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n{new string('=', 50)}");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{headerFrames[i]:^50}");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"{new string('=', 50)}\n");
            Console.ResetColor();
            await Task.Delay(300);
        }
    }

    private async Task DisplayTempoChangeEffectAsync(double tempo)
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        var bpm = 60000000.0 / tempo;
        Console.Write($"🎵 TEMPO CHANGE: {bpm:F0} BPM ");
        
        // Animated tempo visualization
        for (int i = 0; i < 5; i++)
        {
            Console.Write("♪");
            await Task.Delay(100);
        }
        Console.WriteLine(" 🎵");
        Console.ResetColor();
    }

    private async Task DisplayErrorEffectAsync(string message)
    {
        // Flashing error effect
        for (int i = 0; i < 3; i++)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"🚨 PARSER ERROR: {message}");
            await Task.Delay(200);
            Console.Clear();
            await Task.Delay(200);
        }
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"🚨 PARSER ERROR: {message}");
        Console.ResetColor();
    }

    private async Task DisplayEndAnimationAsync()
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n🎉 MIDI playback complete! 🎉");
        
        // Celebration effect
        var celebrations = new[] { "🎊", "🎉", "✨", "🌟" };
        for (int i = 0; i < 10; i++)
        {
            Console.Write(celebrations[_random.Next(celebrations.Length)]);
            await Task.Delay(100);
        }
        Console.WriteLine();
        Console.ResetColor();
    }

    private void DisplayMidiEventOptimized(MidiEvent midiEvent, TimeSpan timestamp)
    {
        // Update animation frame
        var now = DateTime.UtcNow;
        if ((now - _lastFrameTime).TotalMilliseconds >= 250) // 4 FPS for animations
        {
            _animationFrame++;
            _lastFrameTime = now;
        }

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
            
            // Get animated icon and color
            var iconAnimations = _eventIconAnimations.GetValueOrDefault(eventType, new[] { "⚫" });
            var colorAnimations = _eventColorAnimations.GetValueOrDefault(eventType, new[] { ConsoleColor.DarkGray });
            
            var currentIcon = iconAnimations[_animationFrame % iconAnimations.Length];
            var currentColor = colorAnimations[_animationFrame % colorAnimations.Length];

            // Add intensity visualization for note events
            var intensityBar = GetIntensityVisualization(midiEvent);
            var sparkleEffect = GetSparkleEffect(midiEvent);

            // Enhanced console output with animations and effects
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"[{timestamp:mm\\:ss\\.fff}] ");
            
            Console.ForegroundColor = currentColor;
            Console.Write($"{currentIcon} {midiEvent.EventType:X2}: ");
            
            // Add intensity bar for note events
            if (!string.IsNullOrEmpty(intensityBar))
            {
                Console.ForegroundColor = GetIntensityColor(midiEvent);
                Console.Write($"{intensityBar} ");
            }
            
            Console.ResetColor();
            Console.Write($"{hexString,-10} ");
            
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(description);
            
            // Add sparkle effect for high-velocity notes
            if (!string.IsNullOrEmpty(sparkleEffect))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($" {sparkleEffect}");
            }
            
            Console.WriteLine();
            Console.ResetColor();
        }
        finally
        {
            _charPool.Return(hexData);
        }
    }

    private string GetIntensityVisualization(MidiEvent midiEvent)
    {
        var eventType = (byte)(midiEvent.EventType & 0xF0);
        
        if ((eventType == 0x80 || eventType == 0x90) && midiEvent.Data.Length >= 3)
        {
            var velocity = midiEvent.Data[2];
            var intensityIndex = Math.Min(velocity * _intensityBars.Length / 128, _intensityBars.Length - 1);
            return _intensityBars[intensityIndex];
        }
        
        if (eventType == 0xB0 && midiEvent.Data.Length >= 3) // Control Change
        {
            var value = midiEvent.Data[2];
            var intensityIndex = Math.Min(value * _intensityBars.Length / 128, _intensityBars.Length - 1);
            return _intensityBars[intensityIndex];
        }
        
        return string.Empty;
    }

    private ConsoleColor GetIntensityColor(MidiEvent midiEvent)
    {
        var eventType = (byte)(midiEvent.EventType & 0xF0);
        
        if ((eventType == 0x80 || eventType == 0x90) && midiEvent.Data.Length >= 3)
        {
            var velocity = midiEvent.Data[2];
            return velocity switch
            {
                >= 100 => ConsoleColor.Red,
                >= 80 => ConsoleColor.Yellow,
                >= 60 => ConsoleColor.Green,
                >= 40 => ConsoleColor.Cyan,
                _ => ConsoleColor.Blue
            };
        }
        
        return ConsoleColor.Gray;
    }

    private string GetSparkleEffect(MidiEvent midiEvent)
    {
        var eventType = (byte)(midiEvent.EventType & 0xF0);
        
        if (eventType == 0x90 && midiEvent.Data.Length >= 3) // Note On
        {
            var velocity = midiEvent.Data[2];
            if (velocity >= 100) // High velocity notes get sparkle effect
            {
                return _sparkleEffects[_random.Next(_sparkleEffects.Length)];
            }
        }
        
        return string.Empty;
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
}