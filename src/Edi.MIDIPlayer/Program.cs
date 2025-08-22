using Edi.MIDIPlayer.Interfaces;
using Edi.MIDIPlayer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Runtime.InteropServices;
using System.Text;

namespace Edi.MIDIPlayer;

internal class Program
{
    static async Task Main(string[] args)
    {
        // Check if running on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ERROR: This program is currently Windows only.");
            Console.WriteLine("The MIDI player requires Windows-specific audio subsystems to function properly.");
            Console.ResetColor();
            Environment.Exit(1);
        }

        // Setup dependency injection
        var host = CreateHostBuilder(args).Build();
        var serviceProvider = host.Services;

        try
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Clear();

            var consoleDisplay = serviceProvider.GetRequiredService<IConsoleDisplay>();
            var inputHandler = serviceProvider.GetRequiredService<IInputHandler>();
            var midiPlayer = serviceProvider.GetRequiredService<IMidiPlayerService>();

            consoleDisplay.DisplayHackerBanner();

            string fileUrl = inputHandler.GetMidiFileUrl(args);

            if (string.IsNullOrEmpty(fileUrl))
            {
                consoleDisplay.WriteMessage("ERROR", "MIDI file path or URL not provided", ConsoleColor.Red);
                return;
            }

            // Check if it's a URL or local file
            if (Uri.TryCreate(fileUrl, UriKind.Absolute, out Uri? uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                // It's a URL
                await midiPlayer.PlayMidiFileAsync(fileUrl);
            }
            else
            {
                // It's a local file path
                if (!File.Exists(fileUrl))
                {
                    consoleDisplay.WriteMessage("ERROR", "MIDI file not found or invalid path", ConsoleColor.Red);
                    return;
                }
                await midiPlayer.PlayMidiFileAsync(fileUrl);
            }
        }
        catch (Exception ex)
        {
            var consoleDisplay = serviceProvider.GetRequiredService<IConsoleDisplay>();
            consoleDisplay.WriteMessage("FATAL", $"Unexpected error: {ex.Message}", ConsoleColor.Red);
        }
        finally
        {
            var consoleDisplay = serviceProvider.GetRequiredService<IConsoleDisplay>();
            consoleDisplay.WriteMessage("SYSTEM", "Press any key to exit...", ConsoleColor.Yellow);
            Console.ReadKey();
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Register services
                services.AddSingleton<IConsoleDisplay, ConsoleDisplayService>();
                services.AddSingleton<IInputHandler, InputHandlerService>();
                services.AddSingleton<ITempoManager, TempoManagerService>();
                services.AddSingleton<INoteProcessor, NoteProcessorService>();

                // Register typed HTTP client
                services.AddHttpClient<IFileDownloader, FileDownloaderService>(client =>
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Edi.MIDIPlayer/1.0");
                    client.Timeout = TimeSpan.FromMinutes(5); // Default timeout
                });

                services.AddSingleton<IMidiPlayerService, MidiPlayerService>();
            });
}
