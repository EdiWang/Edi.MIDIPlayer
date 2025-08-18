namespace Edi.MIDIPlayer;

public class MidiPlayer : IDisposable
{
    private readonly MciPlaybackEngine _playbackEngine;
    private readonly MidiEventDisplay _eventDisplay;
    private readonly ConsoleLogger _logger;
    private CancellationTokenSource? _playbackCancellation;
    private bool _disposed = false;

    public MidiPlayer()
    {
        _logger = new ConsoleLogger();
        _playbackEngine = new MciPlaybackEngine(_logger);
        _eventDisplay = new MidiEventDisplay(_logger);
    }

    public static bool Initialize()
    {
        return MidiSystemManager.Initialize();
    }

    public async Task PlayMidiFileAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                _logger.PrintSystemStatus("File System Check", "FILE NOT FOUND", ConsoleColor.Red);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"📁 Target: {filePath}");
                Console.ResetColor();
                return;
            }

            _logger.PrintPlaybackHeader();

            // Clean up any previous playback first
            await _playbackEngine.CleanupAllMciDevicesAsync();

            // Display countdown
            await ShowCountdownAsync();

            // Start MIDI data visualization in parallel with audio playback
            _playbackCancellation = new CancellationTokenSource();

            var audioTask = Task.Run(() => _playbackEngine.PlayFileAsync(filePath));
            var visualTask = Task.Run(() => _eventDisplay.DisplayMidiDataAsync(filePath, _playbackCancellation.Token));

            // Wait for audio task to complete first, then stop visualization
            var audioResult = await audioTask;

            if (!audioResult)
            {
                _logger.PrintSystemStatus("Audio Engine", "PLAYBACK FAILED", ConsoleColor.Red);
                Console.ResetColor();
            }

            // Wait for visualization to complete
            await visualTask;
        }
        catch (Exception ex)
        {
            _logger.PrintSystemStatus("Playback Engine", "EXCEPTION", ConsoleColor.Red);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"STACK TRACE: {ex.Message}");
            Console.ResetColor();
        }
        finally
        {
            _playbackCancellation?.Cancel();
            _playbackCancellation?.Dispose();
            _playbackCancellation = null;
        }
    }

    private static async Task ShowCountdownAsync()
    {
        for (int i = 3; i > 0; i--)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"\rStarting playback in {i} seconds... ");
            Console.ResetColor();
            await Task.Delay(1000);
        }
        Console.WriteLine();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _playbackCancellation?.Cancel();
            _playbackEngine?.Dispose();
            _playbackCancellation?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    ~MidiPlayer()
    {
        Dispose();
    }
}