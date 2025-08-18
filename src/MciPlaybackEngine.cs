using System.Runtime.InteropServices;
using System.Text;

namespace Edi.MIDIPlayer;

public class MciPlaybackEngine : IDisposable
{
    private readonly ConsoleLogger _logger;
    private IntPtr _midiOutHandle = IntPtr.Zero;
    private string _currentAlias = string.Empty;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _disposed = false;

    // Windows API declarations
    [DllImport("winmm.dll")]
    private static extern int mciSendString(string command, StringBuilder? returnValue, int returnLength, IntPtr winHandle);

    [DllImport("winmm.dll")]
    private static extern int mciGetErrorString(int errorCode, StringBuilder errorText, int errorTextLen);

    [DllImport("winmm.dll")]
    private static extern int midiOutClose(IntPtr handle);

    [DllImport("winmm.dll")]
    private static extern int midiOutReset(IntPtr handle);

    public MciPlaybackEngine(ConsoleLogger logger)
    {
        _logger = logger;
    }

    public async Task<bool> PlayFileAsync(string filePath)
    {
        return await Task.Run(() => TrySimplePlayback(filePath));
    }

    public async Task CleanupAllMciDevicesAsync()
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

    private bool TrySimplePlayback(string filePath)
    {
        try
        {
            // Generate a truly unique alias
            _currentAlias = $"MIDI_{Environment.TickCount}_{Thread.CurrentThread.ManagedThreadId}";

            _logger.PrintSystemStatus("Audio Engine", "CONNECTING", ConsoleColor.Yellow);

            // Step 1: Open the MIDI file
            string openCommand = $"open \"{filePath}\" type sequencer alias {_currentAlias}";
            int result = mciSendString(openCommand, null, 0, IntPtr.Zero);

            if (result != 0)
            {
                StringBuilder errorBuffer = new StringBuilder(256);
                GetMciErrorString(result, errorBuffer);
                _logger.PrintSystemStatus("MCI Interface", "OPEN FAILED", ConsoleColor.Red);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"MCI Error: {errorBuffer}");
                Console.ResetColor();
                return false;
            }

            _logger.PrintSystemStatus("MCI Interface", "FILE LOADED", ConsoleColor.Green);

            // Step 2: Get file information
            StringBuilder lengthBuffer = new StringBuilder(255);
            string statusCommand = $"status {_currentAlias} length";
            result = mciSendString(statusCommand, lengthBuffer, 255, IntPtr.Zero);

            if (result == 0)
            {
                _logger.PrintSystemStatus("Duration Analysis", $"{lengthBuffer} time units", ConsoleColor.Cyan);
            }

            // Step 3: Start playback
            string playCommand = $"play {_currentAlias}";
            result = mciSendString(playCommand, null, 0, IntPtr.Zero);

            if (result != 0)
            {
                StringBuilder errorBuffer = new StringBuilder(256);
                GetMciErrorString(result, errorBuffer);
                _logger.PrintSystemStatus("Audio Engine", "PLAYBACK FAILED", ConsoleColor.Red);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"🚨 Playback Error: {errorBuffer}");
                Console.ResetColor();

                // Clean up the opened file
                mciSendString($"close {_currentAlias}", null, 0, IntPtr.Zero);
                return false;
            }

            _logger.PrintSystemStatus("Audio Engine", "STREAMING ACTIVE", ConsoleColor.Green);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Audio pipeline established - Real-time streaming initiated");
            Console.ResetColor();

            // Step 4: Monitor playback
            MonitorPlayback();

            // Step 5: Clean up
            StopCurrentPlayback();
            return true;
        }
        catch (Exception ex)
        {
            _logger.PrintSystemStatus("Audio Engine", "EXCEPTION", ConsoleColor.Red);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"CRITICAL ERROR: {ex.Message}");
            Console.ResetColor();
            StopCurrentPlayback();
            return false;
        }
    }

    private void MonitorPlayback()
    {
        _cancellationTokenSource = new CancellationTokenSource();

        while (!_cancellationTokenSource.Token.IsCancellationRequested)
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
                    _logger.PrintSystemStatus("Audio Engine", "STREAM ENDED", ConsoleColor.Yellow);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Audio processing completed successfully");
                    Console.ResetColor();
                    _cancellationTokenSource.Cancel();
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
                _logger.PrintSystemStatus("Cleanup Process", $"Terminating {_currentAlias}", ConsoleColor.DarkYellow);
                mciSendString($"stop {_currentAlias}", null, 0, IntPtr.Zero);
                mciSendString($"close {_currentAlias}", null, 0, IntPtr.Zero);
                _currentAlias = string.Empty;
            }
            catch (Exception ex)
            {
                _logger.PrintSystemStatus("Cleanup Process", "WARNING", ConsoleColor.Red);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"⚠️  Cleanup issue: {ex.Message}");
                Console.ResetColor();
            }
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
            _cancellationTokenSource?.Cancel();
            StopCurrentPlayback();

            if (_midiOutHandle != IntPtr.Zero)
            {
                midiOutReset(_midiOutHandle);
                midiOutClose(_midiOutHandle);
                _midiOutHandle = IntPtr.Zero;
            }

            _cancellationTokenSource?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    ~MciPlaybackEngine()
    {
        Dispose();
    }
}