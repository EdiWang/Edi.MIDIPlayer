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
    private static extern int midiOutOpen(ref IntPtr handle, int deviceId, IntPtr callback, IntPtr instance, int flags);

    [DllImport("winmm.dll")]
    private static extern int midiOutClose(IntPtr handle);

    [DllImport("winmm.dll")]
    private static extern int midiOutShortMsg(IntPtr handle, int message);

    [DllImport("winmm.dll")]
    private static extern int midiOutGetNumDevs();

    [DllImport("winmm.dll")]
    private static extern int midiOutReset(IntPtr handle);

    public static bool Initialize()
    {
        try
        {
            int deviceCount = midiOutGetNumDevs();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[INFO] Detected {deviceCount} MIDI output devices on system.");
            Console.ResetColor();

            if (deviceCount == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[ERROR] No MIDI output devices found!");
                Console.WriteLine("[HINT] Try installing a software MIDI synthesizer or check Windows audio settings.");
                Console.ResetColor();
                return false;
            }

            // Don't open MIDI device here - we'll use MCI for file playback
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[SUCCESS] MIDI system initialized!");
            Console.ResetColor();
            return true;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] Failed to initialize MIDI: {ex.Message}");
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
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[ERROR] MIDI file not found for playback!");
                Console.ResetColor();
                return;
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[PLAYER] Attempting MIDI playback...");
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
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[ERROR] MIDI playback failed with all methods.");
                Console.WriteLine("[SUGGESTION] Your system may not have a working MIDI synthesizer.");
                Console.WriteLine("[ALTERNATIVE] Try the note testing feature instead.");
                Console.ResetColor();
            }
            
            // Wait for visualization to complete
            await visualTask;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] MIDI playback failed: {ex.Message}");
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
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("\n[MIDI DATA] Starting MIDI data visualization...");
            Console.WriteLine("Format: [Timestamp] Command: HEX_DATA (Description)");
            Console.WriteLine("═══════════════════════════════════════════════════════");
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

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine("[MIDI DATA] Visualization complete.");
            Console.ResetColor();
        }
        catch (OperationCanceledException)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine("[MIDI DATA] Visualization stopped with audio playback.");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] MIDI data visualization failed: {ex.Message}");
            Console.ResetColor();
        }
    }

    private static void DisplayMidiEvent(MidiEvent midiEvent, TimeSpan timestamp)
    {
        var hexData = string.Join(" ", midiEvent.Data.Select(b => $"{b:X2}"));
        var description = GetMidiEventDescription(midiEvent);
        
        Console.ForegroundColor = GetEventColor(midiEvent.EventType);
        Console.WriteLine($"[{timestamp:mm\\:ss\\.fff}] {midiEvent.EventType:X2}: {hexData,-20} ({description})");
        Console.ResetColor();
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
        Console.WriteLine($"[MIDI INFO] Format: {format}, Tracks: {trackCount}, Division: {division}");
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

            var midiEvent = await ParseEventAsync(reader, eventByte, currentTicks);
            if (midiEvent != null)
            {
                events.Add(midiEvent);
            }
        }
    }

    private static async Task<MidiEvent?> ParseEventAsync(BinaryReader reader, byte eventType, int ticks)
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
            Data = eventData.ToArray()
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

            // Step 1: Open the MIDI file
            string openCommand = $"open \"{filePath}\" type sequencer alias {_currentAlias}";
            int result = mciSendString(openCommand, null, 0, IntPtr.Zero);

            if (result != 0)
            {
                StringBuilder errorBuffer = new StringBuilder(256);
                GetMciErrorString(result, errorBuffer);
                Console.WriteLine($"Could not open MIDI file: {errorBuffer}");
                return false;
            }

            Console.WriteLine("MIDI file opened successfully!");

            // Step 2: Get file information
            StringBuilder lengthBuffer = new StringBuilder(255);
            string statusCommand = $"status {_currentAlias} length";
            result = mciSendString(statusCommand, lengthBuffer, 255, IntPtr.Zero);

            if (result == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[INFO] MIDI length: {lengthBuffer} time units");
                Console.ResetColor();
            }

            // Step 3: Start playback
            string playCommand = $"play {_currentAlias}";
            result = mciSendString(playCommand, null, 0, IntPtr.Zero);

            if (result != 0)
            {
                StringBuilder errorBuffer = new StringBuilder(256);
                GetMciErrorString(result, errorBuffer);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] Could not start playback: {errorBuffer}");
                Console.ResetColor();

                // Clean up the opened file
                mciSendString($"close {_currentAlias}", null, 0, IntPtr.Zero);
                return false;
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[AUDIO] Audio playback started...");
            Console.ResetColor();

            // Step 4: Monitor playback
            MonitorPlayback();

            // Step 5: Clean up
            StopCurrentPlayback();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Simple playback failed: {ex.Message}");
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
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\n[AUDIO] Audio playback finished.");
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
                Console.WriteLine($"[CLEANUP] Closing MIDI alias: {_currentAlias}");
                mciSendString($"stop {_currentAlias}", null, 0, IntPtr.Zero);
                mciSendString($"close {_currentAlias}", null, 0, IntPtr.Zero);
                _currentAlias = string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] Cleanup failed: {ex.Message}");
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
            Console.WriteLine($"[DEBUG] Cleanup attempt: {ex.Message}");
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
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }
}