using System.Runtime.InteropServices;
using System.Text;

namespace Edi.MIDIPlayer;

public class MidiPlayer : IDisposable
{
    private IntPtr _midiOutHandle = IntPtr.Zero;
    private bool _disposed = false;
    private string _currentAlias = string.Empty;
    private CancellationTokenSource? _playbackCancellation;

    // Windows API declarations
    [DllImport("winmm.dll")]
    private static extern int mciSendString(string command, StringBuilder? returnValue, int returnLength, IntPtr winHandle);

    [DllImport("winmm.dll")]
    private static extern int mciGetErrorString(int errorCode, StringBuilder errorText, int errorTextLen);

    [DllImport("winmm.dll")]
    private static extern int midiOutClose(IntPtr handle);

    [DllImport("winmm.dll")]
    private static extern int midiOutGetNumDevs();

    [DllImport("winmm.dll")]
    private static extern int midiOutReset(IntPtr handle);

    private static void PrintSystemStatus(string message, string status, ConsoleColor statusColor)
    {
        Console.Write("[");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("SYS");
        Console.ResetColor();
        Console.Write("] ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"{message,-35}");
        Console.ResetColor();
        Console.Write(" [");
        Console.ForegroundColor = statusColor;
        Console.Write(status);
        Console.ResetColor();
        Console.WriteLine("]");
    }

    private static void PrintProgressBar(string label, int percentage, ConsoleColor color = ConsoleColor.Green)
    {
        const int barWidth = 40;
        int filled = (percentage * barWidth) / 100;
        
        Console.Write($"[{label}] [");
        Console.ForegroundColor = color;
        Console.Write(new string('█', filled));
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(new string('░', barWidth - filled));
        Console.ResetColor();
        Console.WriteLine($"] {percentage}%");
    }

    public static bool Initialize()
    {
        try
        {
            // Simulate system initialization with progress bars
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("┌─────────────────── SYSTEM INITIALIZATION ───────────────────┐");
            Console.ResetColor();
            
            PrintProgressBar("Loading MIDI drivers", 25, ConsoleColor.Cyan);
            Thread.Sleep(100);
            PrintProgressBar("Scanning audio devices", 50, ConsoleColor.Yellow);
            Thread.Sleep(100);
            PrintProgressBar("Initializing synthesizers", 75, ConsoleColor.Green);
            Thread.Sleep(100);
            PrintProgressBar("System ready", 100, ConsoleColor.Green);
            
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("└──────────────────────────────────────────────────────────────┘");
            Console.ResetColor();
            Console.WriteLine();

            int deviceCount = midiOutGetNumDevs();
            PrintSystemStatus("MIDI Output Devices Detected", $"{deviceCount} devices", ConsoleColor.Cyan);

            if (deviceCount == 0)
            {
                PrintSystemStatus("MIDI System Status", "CRITICAL ERROR", ConsoleColor.Red);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("┌─ ERROR ANALYSIS ─────────────────────────────────────────────┐");
                Console.WriteLine("│ ⚠️  No MIDI output devices found in system registry          │");
                Console.WriteLine("│ 💡 SUGGESTION: Install virtual MIDI synthesizer             │");
                Console.WriteLine("│ 🔧 Check Windows Audio Service status                       │");
                Console.WriteLine("└──────────────────────────────────────────────────────────────┘");
                Console.ResetColor();
                return false;
            }

            PrintSystemStatus("MIDI System Status", "ONLINE", ConsoleColor.Green);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ All systems operational - Ready for audio stream processing");
            Console.ResetColor();
            Console.WriteLine();
            return true;
        }
        catch (Exception ex)
        {
            PrintSystemStatus("System Initialization", "FAILED", ConsoleColor.Red);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"🚨 EXCEPTION: {ex.Message}");
            Console.ResetColor();
            return false;
        }
    }

    public async Task PlayMidiFileAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                PrintSystemStatus("File System Check", "FILE NOT FOUND", ConsoleColor.Red);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"📁 Target: {filePath}");
                Console.ResetColor();
                return;
            }

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("┌─ INITIATING PLAYBACK SEQUENCE ──────────────────────────────┐");
            Console.WriteLine("│ 🎵 Loading MIDI stream into memory buffer...                │");
            Console.WriteLine("│ 🔄 Preparing dual-thread audio/visual processing...         │");
            Console.WriteLine("└──────────────────────────────────────────────────────────────┘");
            Console.ResetColor();

            // Clean up any previous playback first
            await CleanupAllMciDevices();

            // Start MIDI data visualization in parallel with audio playback
            _playbackCancellation = new CancellationTokenSource();

            var audioTask = Task.Run(() => TrySimplePlayback(filePath));
            var visualTask = Task.Run(() => DisplayMidiDataAsync(filePath, _playbackCancellation.Token));

            // Wait for audio task to complete first, then stop visualization
            var audioResult = await audioTask;

            if (!audioResult)
            {
                PrintSystemStatus("Audio Engine", "PLAYBACK FAILED", ConsoleColor.Red);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("┌─ TROUBLESHOOTING GUIDE ─────────────────────────────────────┐");
                Console.WriteLine("│ 🔍 MIDI synthesizer may not be properly configured          │");
                Console.WriteLine("│ 🎹 Try the note testing feature for hardware verification   │");
                Console.WriteLine("│ 🔧 Check system audio drivers and MIDI mapper settings      │");
                Console.WriteLine("└──────────────────────────────────────────────────────────────┘");
                Console.ResetColor();
            }

            // Wait for visualization to complete
            await visualTask;
        }
        catch (Exception ex)
        {
            PrintSystemStatus("Playback Engine", "EXCEPTION", ConsoleColor.Red);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"🚨 STACK TRACE: {ex.Message}");
            Console.ResetColor();
        }
        finally
        {
            _playbackCancellation?.Cancel();
            _playbackCancellation?.Dispose();
            _playbackCancellation = null;
        }
    }

    private async Task DisplayMidiDataAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n╔═══════════════════ MIDI STREAM ANALYZER ═══════════════════╗");
            Console.WriteLine("║ Real-time MIDI event processing and hex dump visualization  ║");
            Console.WriteLine("║ Format: [Timestamp] Command: HEX_DATA (Event Description)   ║");
            Console.WriteLine("╠══════════════════════════════════════════════════════════════╣");
            Console.ResetColor();

            var (midiEvents, ticksPerQuarter) = await ParseMidiFileAsync(filePath);
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
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            Console.WriteLine("🎯 MIDI stream analysis complete - All events processed");
            Console.ResetColor();
        }
        catch (OperationCanceledException)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            Console.WriteLine("⏹️  MIDI stream analysis terminated by audio completion");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            PrintSystemStatus("MIDI Parser", "EXCEPTION", ConsoleColor.Red);
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
        Console.Write($" {hexData,-20} ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"({description})");
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
            0x80 => $"Note Off - Ch{(midiEvent.EventType & 0x0F) + 1}, Note {midiEvent.Data[1]}, Vel {midiEvent.Data[2]}",
            0x90 => $"Note On  - Ch{(midiEvent.EventType & 0x0F) + 1}, Note {midiEvent.Data[1]}, Vel {midiEvent.Data[2]}",
            0xA0 => $"Aftertouch - Ch{(midiEvent.EventType & 0x0F) + 1}, Note {midiEvent.Data[1]}, Pressure {midiEvent.Data[2]}",
            0xB0 => $"Control Change - Ch{(midiEvent.EventType & 0x0F) + 1}, CC {midiEvent.Data[1]}, Val {midiEvent.Data[2]}",
            0xC0 => $"Program Change - Ch{(midiEvent.EventType & 0x0F) + 1}, Program {midiEvent.Data[1]}",
            0xD0 => $"Channel Pressure - Ch{(midiEvent.EventType & 0x0F) + 1}, Pressure {midiEvent.Data[1]}",
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

    private static async Task<(List<MidiEvent> events, int ticksPerQuarter)> ParseMidiFileAsync(string filePath)
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
        Console.WriteLine($"📊 MIDI Header Analysis: Format {format} | Tracks {trackCount} | Division {division} PPQ");
        Console.ResetColor();

        // Read tracks
        for (int track = 0; track < trackCount; track++)
        {
            await ParseTrackAsync(reader, events, track);
        }

        // Sort events by absolute ticks for proper timing
        events = events.OrderBy(e => e.Ticks).ToList();

        return (events, division);
    }

    private static async Task ParseTrackAsync(BinaryReader reader, List<MidiEvent> events, int trackNumber)
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

    private bool TrySimplePlayback(string filePath)
    {
        try
        {
            // Generate a truly unique alias
            _currentAlias = $"MIDI_{Environment.TickCount}_{Thread.CurrentThread.ManagedThreadId}";

            PrintSystemStatus("Audio Engine", "CONNECTING", ConsoleColor.Yellow);

            // Step 1: Open the MIDI file
            string openCommand = $"open \"{filePath}\" type sequencer alias {_currentAlias}";
            int result = mciSendString(openCommand, null, 0, IntPtr.Zero);

            if (result != 0)
            {
                StringBuilder errorBuffer = new StringBuilder(256);
                GetMciErrorString(result, errorBuffer);
                PrintSystemStatus("MCI Interface", "OPEN FAILED", ConsoleColor.Red);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"🚨 MCI Error: {errorBuffer}");
                Console.ResetColor();
                return false;
            }

            PrintSystemStatus("MCI Interface", "FILE LOADED", ConsoleColor.Green);

            // Step 2: Get file information
            StringBuilder lengthBuffer = new StringBuilder(255);
            string statusCommand = $"status {_currentAlias} length";
            result = mciSendString(statusCommand, lengthBuffer, 255, IntPtr.Zero);

            if (result == 0)
            {
                PrintSystemStatus("Duration Analysis", $"{lengthBuffer} time units", ConsoleColor.Cyan);
            }

            // Step 3: Start playback
            string playCommand = $"play {_currentAlias}";
            result = mciSendString(playCommand, null, 0, IntPtr.Zero);

            if (result != 0)
            {
                StringBuilder errorBuffer = new StringBuilder(256);
                GetMciErrorString(result, errorBuffer);
                PrintSystemStatus("Audio Engine", "PLAYBACK FAILED", ConsoleColor.Red);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"🚨 Playback Error: {errorBuffer}");
                Console.ResetColor();

                // Clean up the opened file
                mciSendString($"close {_currentAlias}", null, 0, IntPtr.Zero);
                return false;
            }

            PrintSystemStatus("Audio Engine", "STREAMING ACTIVE", ConsoleColor.Green);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("🎵 Audio pipeline established - Real-time streaming initiated");
            Console.ResetColor();

            // Step 4: Monitor playback
            MonitorPlayback();

            // Step 5: Clean up
            StopCurrentPlayback();
            return true;
        }
        catch (Exception ex)
        {
            PrintSystemStatus("Audio Engine", "EXCEPTION", ConsoleColor.Red);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"🚨 CRITICAL ERROR: {ex.Message}");
            Console.ResetColor();
            StopCurrentPlayback();
            return false;
        }
    }

    private void MonitorPlayback()
    {
        while (_playbackCancellation?.Token.IsCancellationRequested == false)
        {
            StringBuilder status = new(255);
            string statusCommand = $"status {_currentAlias} mode";
            int result = mciSendString(statusCommand, status, 255, IntPtr.Zero);

            // Only check status if the command succeeded
            if (result == 0)
            {
                string statusText = status.ToString().ToLower();
                if (statusText.Contains("stopped") || statusText.Contains("not ready"))
                {
                    PrintSystemStatus("Audio Engine", "STREAM ENDED", ConsoleColor.Yellow);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✅ Audio processing completed successfully");
                    Console.ResetColor();
                    _playbackCancellation?.Cancel();
                    break;
                }
            }

            Thread.Sleep(100); // Check every 100ms
        }
    }

    private void StopCurrentPlayback()
    {
        if (!string.IsNullOrEmpty(_currentAlias))
        {
            try
            {
                PrintSystemStatus("Cleanup Process", $"Terminating {_currentAlias}", ConsoleColor.DarkYellow);
                mciSendString($"stop {_currentAlias}", null, 0, IntPtr.Zero);
                mciSendString($"close {_currentAlias}", null, 0, IntPtr.Zero);
                _currentAlias = string.Empty;
            }
            catch (Exception ex)
            {
                PrintSystemStatus("Cleanup Process", "WARNING", ConsoleColor.Red);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"⚠️  Cleanup issue: {ex.Message}");
                Console.ResetColor();
            }
        }
    }

    private static async Task CleanupAllMciDevices()
    {
        try
        {
            // Close all potential leftover MCI devices
            string[] potentialAliases = ["MidiFile", "MIDI", "sequencer"];

            foreach (string alias in potentialAliases)
            {
                mciSendString($"close {alias}", null, 0, IntPtr.Zero);
                mciSendString($"close all", null, 0, IntPtr.Zero);
            }

            // Small delay to ensure cleanup
            await Task.Delay(100);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"🔧 Cleanup routine: {ex.Message}");
            Console.ResetColor();
        }
    }

    private static void GetMciErrorString(int errorCode, StringBuilder errorBuffer)
    {
        errorBuffer.Clear();
        int result = mciGetErrorString(errorCode, errorBuffer, errorBuffer.Capacity);
        if (result == 0)
        {
            errorBuffer.Append($"MCI Error {errorCode} (no description available)");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _playbackCancellation?.Cancel();
            StopCurrentPlayback();

            if (_midiOutHandle != IntPtr.Zero)
            {
                midiOutReset(_midiOutHandle);
                midiOutClose(_midiOutHandle);
                _midiOutHandle = IntPtr.Zero;
            }

            _playbackCancellation?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    ~MidiPlayer()
    {
        Dispose();
    }

    // Helper class for MIDI events
    private class MidiEvent
    {
        public int Ticks { get; set; }
        public byte EventType { get; set; }
        public byte[] Data { get; set; } = [];
    }
}