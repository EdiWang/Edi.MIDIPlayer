using Edi.MIDIPlayer.Hubs;
using Edi.MIDIPlayer.Interfaces;
using Edi.MIDIPlayer.Services;
using System.Runtime.InteropServices;

namespace Edi.MIDIPlayer;

internal class Program
{
    static async Task Main(string[] args)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ERROR: This program is currently Windows only.");
            Console.ResetColor();
            Environment.Exit(1);
        }

        var builder = WebApplication.CreateBuilder(args);

        // Configure services
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

        // Configure middleware
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.MapHub<MidiPlayerHub>("/midihub");

        // Start web server
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
            catch { }
        });

        _ = Task.Run(async () =>
        {
            await Task.Delay(3000);
            var midiPlayer = app.Services.GetRequiredService<IMidiPlayerService>();
            var inputHandler = app.Services.GetRequiredService<IInputHandler>();

            string fileUrl = inputHandler.GetMidiFileUrl(args);
            if (!string.IsNullOrEmpty(fileUrl))
            {
                await midiPlayer.PlayMidiFileAsync(fileUrl);
            }
        });

        await app.RunAsync(url);
    }
}
