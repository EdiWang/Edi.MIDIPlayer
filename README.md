# Edi.MIDIPlayer

Edi.MIDIPlayer is a Windows-only .NET global tool for playing Standard MIDI files through the local MIDI output device while visualizing playback events. By default it starts a local SignalR web visualizer; it can also run with the terminal visualizer used by the earlier CLI experience.

The project is useful for quickly previewing MIDI files, inspecting note/control/program events during playback, and demonstrating MIDI timing and visualization behavior from either local files or remote `.mid` / `.midi` URLs smaller than 10 MB.

> Important: this project currently depends on Windows MIDI output APIs through NAudio and exits on non-Windows platforms.

## Business Logic Overview

The main workflow is:

1. Parse command-line arguments and choose a display mode.
2. Resolve the MIDI source from the first non-option argument or prompt interactively when no file is provided.
3. Load the MIDI file from a local path or download it from an HTTP/HTTPS URL.
4. Read all MIDI tracks, merge events by absolute tick time, and build a tempo map from tempo meta events.
5. Play events in real time through the default MIDI output device.
6. Publish event details to the selected display implementation.

The main business modules are:

- **Application host**: `Program.cs` selects web or console mode, configures dependency injection, and starts playback.
- **Command-line options**: `AppOptions.cs` parses display mode, host options, web URLs, and MIDI file arguments.
- **Playback service**: `MidiPlayerService` loads MIDI content, orders MIDI events, applies tempo timing, sends raw MIDI messages, and coordinates display updates.
- **Tempo management**: `TempoManagerService` converts MIDI ticks into real playback time, including tempo changes.
- **Input and download services**: `InputHandlerService` reads the requested source, and `FileDownloaderService` retrieves remote MIDI files.
- **Display services**: `ConsoleDisplayService` and `WebDisplayService` share the same playback pipeline but render status through terminal output or SignalR messages.
- **Note processors**: `NoteProcessorService` and `WebNoteProcessorService` translate note/control events into display-specific messages through shared MIDI display formatting. Web SignalR send failures are logged.
- **Web visualizer**: `wwwroot/index.html`, `styles.css`, and `app.js` render the browser UI and listen for SignalR events from `/midihub`.

Key concepts:

- **MIDI source**: a local `.mid` / `.midi` file path or HTTP/HTTPS `.mid` / `.midi` URL smaller than 10 MB.
- **Display mode**: `web` is the default; `console` keeps the terminal visualizer available.
- **Tempo map**: a list of tempo changes used to convert MIDI ticks into wall-clock playback delays.
- **Active notes**: runtime channel-plus-note state used for display counts and diagnostics while playback is running.
- **MIDI output device**: the first available Windows MIDI output device is used; playback reports a clear error if no MIDI output device is available.
- **SignalR browser client**: the web UI uses the pinned Microsoft SignalR JavaScript script in `wwwroot/index.html`.

## Run, Build, and Test

Install from NuGet as a global tool:

```powershell
dotnet tool install -g Edi.MIDIPlayer
```

Play a local MIDI file with the default web visualizer:

```powershell
midi-player "path\to\your\file.mid"
```

Use a custom web visualizer URL:

```powershell
midi-player --urls "http://localhost:5050" "path\to\your\file.mid"
```

Play a remote MIDI file:

```powershell
midi-player "https://example.com/path/to/file.mid"
```

Use the terminal visualizer:

```powershell
midi-player --display console "path\to\your\file.mid"
```

Pause before exiting console mode:

```powershell
midi-player --display console --pause-on-exit "path\to\your\file.mid"
```

Equivalent display shortcuts:

```powershell
midi-player --web "path\to\your\file.mid"
midi-player --console "path\to\your\file.mid"
```

Run without a MIDI argument to enter interactive input mode:

```powershell
midi-player
```

Build and validate from the solution directory:

```powershell
cd src
dotnet build --configuration Release
dotnet test --configuration Release
```

Testing note: the solution includes `Edi.MIDIPlayer.Tests`, an xUnit v3 test project with Moq for focused characterization tests. `dotnet test` is the CI validation command and should continue to pass.

Packaging note: the project is packable as a .NET global tool. CI uses the explicit `dotnet pack --configuration Release -o nupkg` step; regular builds do not generate a package automatically.
