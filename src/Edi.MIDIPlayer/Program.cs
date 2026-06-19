using Edi.MIDIPlayer.Hubs;
using Edi.MIDIPlayer.Interfaces;
using Edi.MIDIPlayer.Services;
using Microsoft.Extensions.Hosting;
using System.Runtime.InteropServices;
using System.Text;

namespace Edi.MIDIPlayer;

internal class Program
{
    private const string DefaultWebUrl = "http://localhost:5000";

    static async Task Main(string[] args)
    {
        try
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: This program is currently Windows only.");
                Console.WriteLine("The MIDI player requires Windows-specific audio subsystems to function properly.");
                Console.ResetColor();
                Environment.Exit(1);
            }

            var options = AppOptions.Parse(args);
            if (options.ShowHelp)
            {
                WriteUsage();
                return;
            }

            if (options.DisplayMode == DisplayMode.Console)
            {
                await RunConsoleAsync(options);
                return;
            }

            await RunWebAsync(options);
        }
        catch (ArgumentException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: {ex.Message}");
            Console.ResetColor();
            WriteUsage();
            Environment.Exit(1);
        }
    }

    private static async Task RunWebAsync(AppOptions options)
    {
        var builder = WebApplication.CreateBuilder(options.HostArgs);

        builder.Services.AddSignalR();
        builder.Services.AddSingleton<IConsoleDisplay, WebDisplayService>();
        builder.Services.AddSingleton<IInputHandler, InputHandlerService>();
        builder.Services.AddSingleton<ITempoManager, TempoManagerService>();
        builder.Services.AddSingleton<INoteProcessor, WebNoteProcessorService>();

        builder.Services.AddHttpClient<IFileDownloader, FileDownloaderService>(client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "Edi.MIDIPlayer/2.0");
            client.Timeout = TimeSpan.FromMinutes(5);
        });

        builder.Services.AddSingleton<IMidiPlayerService, MidiPlayerService>();

        var app = builder.Build();

        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.MapHub<MidiPlayerHub>("/midihub");

        var bindingUrls = options.WebUrls ?? DefaultWebUrl;
        var launchUrl = GetBrowserLaunchUrl(bindingUrls);

        Console.WriteLine($"Starting MIDI Player Web Visualizer at {bindingUrls}");
        Console.WriteLine($"Opening browser at {launchUrl}...");

        _ = Task.Run(async () =>
        {
            await Task.Delay(2000);
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = launchUrl,
                    UseShellExecute = true
                });
            }
            catch
            {
            }
        });

        _ = Task.Run(async () =>
        {
            await Task.Delay(3000);
            await PlayRequestedMidiFileAsync(app.Services, options.MidiArgs, validateLocalPath: true);
        });

        if (options.WebUrls is null)
        {
            await app.RunAsync(DefaultWebUrl);
        }
        else
        {
            await app.RunAsync();
        }
    }

    private static string GetBrowserLaunchUrl(string urls)
    {
        var firstUrl = urls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(firstUrl))
        {
            return DefaultWebUrl;
        }

        if (!Uri.TryCreate(firstUrl, UriKind.Absolute, out var uri))
        {
            return firstUrl;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return firstUrl;
        }

        var uriBuilder = new UriBuilder(uri);
        if (uri.Host is "*" or "+" or "0.0.0.0" or "::")
        {
            uriBuilder.Host = "localhost";
        }

        return uriBuilder.Uri.ToString().TrimEnd('/');
    }

    private static async Task RunConsoleAsync(AppOptions options)
    {
        using var host = CreateConsoleHostBuilder(options.HostArgs).Build();
        var serviceProvider = host.Services;

        try
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Clear();

            await PlayRequestedMidiFileAsync(serviceProvider, options.MidiArgs, validateLocalPath: true);
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

    private static IHostBuilder CreateConsoleHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<IConsoleDisplay, ConsoleDisplayService>();
                services.AddSingleton<IInputHandler, InputHandlerService>();
                services.AddSingleton<ITempoManager, TempoManagerService>();
                services.AddSingleton<INoteProcessor, NoteProcessorService>();

                services.AddHttpClient<IFileDownloader, FileDownloaderService>(client =>
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Edi.MIDIPlayer/2.0");
                    client.Timeout = TimeSpan.FromMinutes(5);
                });

                services.AddSingleton<IMidiPlayerService, MidiPlayerService>();
            });

    private static async Task PlayRequestedMidiFileAsync(
        IServiceProvider serviceProvider,
        string[] midiArgs,
        bool validateLocalPath)
    {
        var consoleDisplay = serviceProvider.GetRequiredService<IConsoleDisplay>();
        var inputHandler = serviceProvider.GetRequiredService<IInputHandler>();
        var midiPlayer = serviceProvider.GetRequiredService<IMidiPlayerService>();

        consoleDisplay.DisplayHackerBanner();

        var fileUrl = inputHandler.GetMidiFileUrl(midiArgs);
        if (string.IsNullOrEmpty(fileUrl))
        {
            consoleDisplay.WriteMessage("ERROR", "MIDI file path or URL not provided", ConsoleColor.Red);
            return;
        }

        if (validateLocalPath &&
            !(Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri) &&
              (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)) &&
            !File.Exists(fileUrl))
        {
            consoleDisplay.WriteMessage("ERROR", "MIDI file not found or invalid path", ConsoleColor.Red);
            return;
        }

        await midiPlayer.PlayMidiFileAsync(fileUrl);
    }

    private static void WriteUsage()
    {
        Console.WriteLine("""
            Usage:
              midi-player [--display web|console] [--urls http://localhost:5000] <midi-file-or-url>
              midi-player --web <midi-file-or-url>
              midi-player --console <midi-file-or-url>

            Display modes:
              web      Start the SignalR web visualizer. This is the default.
              console  Use the terminal visualizer from v1.
            """);
    }

}
