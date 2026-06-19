using Edi.MIDIPlayer.Hubs;
using Edi.MIDIPlayer.Interfaces;
using Edi.MIDIPlayer.Services;
using Microsoft.Extensions.Hosting;
using System.Runtime.InteropServices;
using System.Text;

namespace Edi.MIDIPlayer;

internal class Program
{
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

        var url = "http://localhost:5000";
        Console.WriteLine($"Starting MIDI Player Web Visualizer at {url}");
        Console.WriteLine("Opening browser...");

        _ = Task.Run(async () =>
        {
            await Task.Delay(2000);
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
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

        await app.RunAsync(url);
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
              midi-player [--display web|console] <midi-file-or-url>
              midi-player --web <midi-file-or-url>
              midi-player --console <midi-file-or-url>

            Display modes:
              web      Start the SignalR web visualizer. This is the default.
              console  Use the terminal visualizer from v1.
            """);
    }

    private enum DisplayMode
    {
        Web,
        Console
    }

    private sealed record AppOptions(DisplayMode DisplayMode, string[] MidiArgs, string[] HostArgs, bool ShowHelp)
    {
        public static AppOptions Parse(string[] args)
        {
            var displayMode = DisplayMode.Web;
            var midiArgs = new List<string>();
            var hostArgs = new List<string>();
            var showHelp = false;

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                if (IsHelpOption(arg))
                {
                    showHelp = true;
                    continue;
                }

                if (TryReadInlineMode(arg, out var inlineMode))
                {
                    displayMode = ParseDisplayMode(inlineMode);
                    continue;
                }

                if (IsModeOption(arg))
                {
                    if (i + 1 >= args.Length)
                    {
                        throw new ArgumentException($"{arg} requires a value: web or console.");
                    }

                    displayMode = ParseDisplayMode(args[++i]);
                    continue;
                }

                if (arg.Equals("--web", StringComparison.OrdinalIgnoreCase))
                {
                    displayMode = DisplayMode.Web;
                    continue;
                }

                if (arg.Equals("--console", StringComparison.OrdinalIgnoreCase))
                {
                    displayMode = DisplayMode.Console;
                    continue;
                }

                if (IsKnownHostOptionWithValue(arg))
                {
                    hostArgs.Add(arg);
                    if (!arg.Contains('=') && i + 1 < args.Length)
                    {
                        hostArgs.Add(args[++i]);
                    }
                    continue;
                }

                hostArgs.Add(arg);

                if (!arg.StartsWith('-'))
                {
                    midiArgs.Add(arg);
                }
            }

            return new AppOptions(displayMode, [.. midiArgs], [.. hostArgs], showHelp);
        }

        private static bool IsHelpOption(string arg) =>
            arg.Equals("-h", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals("/?", StringComparison.OrdinalIgnoreCase);

        private static bool IsModeOption(string arg) =>
            arg.Equals("--display", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals("--mode", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals("--ui", StringComparison.OrdinalIgnoreCase);

        private static bool TryReadInlineMode(string arg, out string value)
        {
            foreach (var option in new[] { "--display=", "--mode=", "--ui=" })
            {
                if (arg.StartsWith(option, StringComparison.OrdinalIgnoreCase))
                {
                    value = arg[option.Length..];
                    return true;
                }
            }

            value = string.Empty;
            return false;
        }

        private static bool IsKnownHostOptionWithValue(string arg)
        {
            var option = arg.Split('=', 2)[0];
            return option.Equals("--urls", StringComparison.OrdinalIgnoreCase) ||
                   option.Equals("--environment", StringComparison.OrdinalIgnoreCase) ||
                   option.Equals("--contentRoot", StringComparison.OrdinalIgnoreCase) ||
                   option.Equals("--webroot", StringComparison.OrdinalIgnoreCase) ||
                   option.Equals("--applicationName", StringComparison.OrdinalIgnoreCase);
        }

        private static DisplayMode ParseDisplayMode(string value) =>
            value.ToLowerInvariant() switch
            {
                "web" or "browser" or "signalr" => DisplayMode.Web,
                "console" or "terminal" or "cli" or "cmd" or "commandline" or "terminal-ui" => DisplayMode.Console,
                _ => throw new ArgumentException($"Unknown display mode '{value}'. Use web or console.")
            };
    }
}
