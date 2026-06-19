# AGENTS.md

This document is for AI coding assistants and engineers who maintain Edi.MIDIPlayer. Keep it synchronized with the codebase whenever behavior, tooling, architecture, or development workflow changes.

## Project Technical Stack

- **Primary language**: C# with nullable reference types enabled.
- **Target framework**: .NET `net10.0`.
- **Project type**: SDK-style executable using `Microsoft.NET.Sdk.Web`; intended to be packaged as a .NET global tool with command name `midi-player`.
- **Runtime platform**: Windows only. `Program.Main` exits on non-Windows platforms because playback uses Windows MIDI output through NAudio.
- **Frameworks and dependencies**:
  - ASP.NET Core minimal hosting for the default local web visualizer.
  - SignalR server hub at `/midihub`.
  - NAudio `2.2.1` for MIDI file parsing and MIDI output.
  - Browser UI loads pinned Microsoft SignalR JavaScript from a CDN script tag in `src/Edi.MIDIPlayer/wwwroot/index.html`.
- **Build tool**: .NET SDK / MSBuild.
- **Package management**: NuGet via `Edi.MIDIPlayer.csproj`; no lock file is currently present.
- **Database/cache/message queue**: none found.
- **Testing framework**: xUnit v3 with Moq. The dedicated test project is `src/Edi.MIDIPlayer.Tests`; CI runs `dotnet test`.
- **Formatting/lint/type checking**: To be confirmed. No `.editorconfig`, explicit analyzer configuration, or formatter configuration was found.
- **Deployment and packaging**:
  - GitHub Actions workflow `.github/workflows/dotnet.yml` builds, tests, packs, and pushes the NuGet package on pushes to `master`.
  - The NuGet package includes the root `README.md` and `img/edi-logo-blue.png`.
  - `Edi.MIDIPlayer.csproj` explicitly sets `IsPackable=true` and `PackAsTool=true`.
  - Packages are produced by explicit `dotnet pack` commands; regular builds do not generate packages automatically.
  - `src/Edi.MIDIPlayer/Properties/PublishProfiles/FolderProfile.pubxml` exists for folder publishing.

## Code Architecture

### Repository Layout

```text
.
|-- README.md
|-- AGENTS.md
|-- docs/
|   `-- task-template.md
|-- img/
|   `-- edi-logo-blue.png
|-- .github/
|   `-- workflows/dotnet.yml
`-- src/
    |-- Edi.MIDIPlayer.slnx
    |-- Edi.MIDIPlayer/
    |   |-- Edi.MIDIPlayer.csproj
    |   |-- AppOptions.cs
    |   |-- Program.cs
    |   |-- Hubs/
    |   |-- Interfaces/
    |   |-- Models/
    |   |-- Services/
    |   |-- Properties/PublishProfiles/
    |   `-- wwwroot/
    `-- Edi.MIDIPlayer.Tests/
        |-- Edi.MIDIPlayer.Tests.csproj
        |-- ActiveNoteTrackerTests.cs
        |-- AppOptionsTests.cs
        |-- FileDownloaderServiceTests.cs
        `-- TempoManagerServiceTests.cs
```

### Key Entry Points

- `src/Edi.MIDIPlayer/Program.cs`
  - Checks the OS platform.
  - Starts either the SignalR web visualizer or the console visualizer.
  - Configures dependency injection for the selected display mode.
  - Starts playback after the web host is available in web mode.

- `src/Edi.MIDIPlayer/AppOptions.cs`
  - Parses command-line display options, host options, web URLs, and MIDI file arguments.

- `src/Edi.MIDIPlayer/Edi.MIDIPlayer.csproj`
  - Defines package metadata, target framework, tool packaging, and NuGet dependencies.

- `src/Edi.MIDIPlayer.Tests/Edi.MIDIPlayer.Tests.csproj`
  - Defines focused xUnit v3 + Moq characterization tests.

- `.github/workflows/dotnet.yml`
  - Defines the Release build, test, pack, and NuGet push workflow.

### Runtime Flow

1. `AppOptions.Parse` separates display options, host options, and MIDI file arguments.
2. Web mode starts ASP.NET Core on `http://localhost:5000` by default, or the URL configured with `--urls`, serves `wwwroot`, maps `MidiPlayerHub` to `/midihub`, opens the browser, and starts playback after a short delay.
3. Console mode builds a generic host and runs playback directly in the terminal.
4. `InputHandlerService` returns the first MIDI argument or prompts for one interactively.
5. `MidiPlayerService` loads a local file or downloads a remote MIDI file.
6. MIDI events from all tracks are flattened, sorted by absolute tick, and passed through `TempoManagerService`.
7. Playback checks that at least one MIDI output device exists, then opens device `0`, waits until each event's expected wall-clock time, sends raw MIDI messages through `MidiDeviceWrapper`, and updates the selected note/display processor.
8. Web mode publishes SignalR messages to browser clients; console mode writes directly to the terminal.

### Core Directories

- `Hubs/`
  - `MidiPlayerHub` contains SignalR methods used by the browser visualizer.

- `Interfaces/`
  - Small abstractions for playback, input, downloading, tempo conversion, note processing, display, and MIDI device output.
  - These abstractions make the display implementations swappable while sharing playback logic.

- `Models/`
  - `ActiveNoteTracker` tracks active note state by channel plus note number and uses reference counts for overlapping same-channel notes.
  - `MidiEventInfo` wraps NAudio MIDI events with absolute time.
  - `TempoChange` represents tempo map entries.

- `Services/`
  - `MidiPlayerService` is the main orchestration service.
  - `TempoManagerService` builds tempo maps and converts ticks to time.
  - `FileDownloaderService` downloads remote MIDI files.
  - `InputHandlerService` reads command-line or interactive input.
  - `MidiDeviceWrapper` adapts NAudio `MidiOut`.
  - `ConsoleDisplayService` and `NoteProcessorService` render terminal output.
  - `WebDisplayService` and `WebNoteProcessorService` publish SignalR events and log send failures through `SignalRSendObserver`.

- `wwwroot/`
  - Static assets for the web visualizer.
  - `index.html` defines the page structure.
  - `styles.css` defines the visual design.
  - `app.js` connects to SignalR, renders piano keys, tracks active notes, and appends event log entries.

## Development Conventions

- Use English for Markdown documentation, code comments, developer-facing logs, test names, and new identifiers unless the content is product copy, localization data, or an existing non-English asset.
- Keep the web and console display paths behind the existing display/note processor abstractions instead of duplicating playback logic.
- Prefer dependency injection for services that participate in the playback pipeline.
- Preserve Windows-only behavior unless a cross-platform MIDI output strategy is intentionally designed and documented.
- Keep command-line parsing in `AppOptions` consistent with existing aliases:
  - `--display`, `--mode`, `--ui`
  - `--web`, `--console`
  - `--pause-on-exit`
  - display values such as `web`, `browser`, `signalr`, `console`, `terminal`, and `cli`
- Host options currently recognized by the parser include `--urls`, `--environment`, `--contentRoot`, `--webroot`, and `--applicationName`.
- `--urls` is an officially supported web-mode option. `Program.RunWebAsync` uses it for ASP.NET Core binding and opens the browser at the first configured URL, converting wildcard hosts such as `*`, `+`, `0.0.0.0`, and `::` to `localhost` for browser launch.
- Local file validation happens before playback unless the source is HTTP/HTTPS.
- Remote downloads are limited to HTTP/HTTPS URLs ending in `.mid` or `.midi`, must be smaller than 10 MB, and use `HttpClient` with a configured user agent and timeouts. `MidiPlayerService` currently requests a 30-second download timeout.
- Active note display state is tracked by channel plus note number. Overlapping same-channel note-on events use reference counts so one note-off does not clear a still-active note.
- Web-mode background startup failures and SignalR send failures are logged with `ILogger`.
- The default web URL is `http://localhost:5000` when `--urls` is not provided.
- Console mode does not pause before exit by default; use `--pause-on-exit` to preserve the older interactive pause behavior.
- Console mode clears the terminal only for interactive, non-redirected output.
- The first MIDI output device (`deviceId = 0`) is used by `MidiDeviceWrapper` after `MidiPlayerService` checks that at least one output device is available.
- Avoid adding broad architectural abstractions unless they clearly reduce duplication or match the existing service/interface pattern.

## Common Development Commands

Run these from `src/` unless otherwise noted:

```powershell
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

Run locally from source:

```powershell
dotnet run --project Edi.MIDIPlayer -- "path\to\file.mid"
dotnet run --project Edi.MIDIPlayer -- --display console "path\to\file.mid"
dotnet run --project Edi.MIDIPlayer -- --display console --pause-on-exit "path\to\file.mid"
dotnet run --project Edi.MIDIPlayer -- "https://example.com/file.mid"
```

Packaging command:

```powershell
dotnet pack --configuration Release -o nupkg
```

Install the packaged tool locally after a package is produced:

```powershell
dotnet tool install -g Edi.MIDIPlayer --add-source .\nupkg
```

## Configuration Notes

- No custom application configuration file was found.
- No custom environment variables were found.
- ASP.NET Core host configuration can still be influenced through standard host options and environment variables supported by ASP.NET Core.
- NuGet package metadata lives in `src/Edi.MIDIPlayer/Edi.MIDIPlayer.csproj`.
- The app hosts a SignalR server hub and browser client; it does not reference the .NET SignalR client package.
- The browser SignalR JavaScript version is pinned in `src/Edi.MIDIPlayer/wwwroot/index.html`. Change it only with an explicit compatibility/security reason and verify the web visualizer plus `/midihub` connection afterward.
- CI publishing requires the `NUGET_API_KEY` GitHub Actions secret.

## Testing and Validation Notes

- The repository has a dedicated xUnit v3 + Moq test project at `src/Edi.MIDIPlayer.Tests`.
- Existing tests cover `AppOptions` parsing, console pause option parsing, active note tracking, SignalR send observation, remote download size/timeout behavior, and tempo conversion.
- `dotnet test --configuration Release` should remain green because the CI workflow depends on it.
- For playback changes, validate at least:
  - local `.mid` playback,
  - remote HTTP/HTTPS MIDI download,
  - rejection for remote URLs that do not end in `.mid` or `.midi`,
  - rejection for remote MIDI files at or above 10 MB,
  - default web visualizer startup,
  - console display mode,
  - behavior when no MIDI output device is available, if feasible.
- For web UI changes, verify that `/midihub` connects and that `ReceiveNoteOn`, `ReceiveNoteOff`, `ReceiveControlChange`, and `ReceiveMessage` still match `app.js`.
- For event-log UI changes, verify that HTML-like text such as `<script>alert(1)</script>` renders literally rather than as markup.

## AI Coding Assistant Rules

### Complex Task Breakdown

When an AI assistant performs a complex task, it must first break the work into incremental subtasks.

Each subtask should be:

- independently implementable,
- independently buildable or runnable,
- independently testable or verifiable,
- independently committable or revertible,
- clear about dependencies on other subtasks.

For complex tasks, create an independent Markdown record under `./docs/` to preserve task context while work is in progress. Record:

- original task goal,
- subtask list,
- subtask execution order,
- subtask dependencies,
- completion status for each phase,
- completed verification steps,
- issues encountered and how they were handled,
- follow-up work.

Use this naming format:

```text
./docs/task-<short-task-name>.md
```

Examples:

```text
./docs/task-refactor-midi-playback.md
./docs/task-add-visualizer-controls.md
```

Update the task document throughout execution so another assistant can recover the goal, context, and next step after conversation compaction or interruption.

### Documentation Synchronization

After every development, fix, refactor, or configuration change, the AI assistant must review:

- `README.md`,
- `AGENTS.md`,
- related documents under `./docs/`.

If the project behavior, architecture, commands, tooling, or operational guidance changed, update the relevant documentation in the same task.

### Troubleshooting / Lessons Learned

When the AI assistant encounters an error, successfully fixes it, and receives user confirmation, add the lesson here using this structure:

```markdown
### Issue title

- Symptom:
- Trigger:
- Root cause:
- Fix:
- Verification:
- Prevention:
```

No confirmed lessons have been recorded yet.

### User Communication

If requirements are unclear and cannot be safely inferred from the codebase, ask the user instead of guessing. Clearly list what needs confirmation, then update the relevant documentation after the user responds.

## To Be Confirmed

- Whether a repository-wide formatter, `.editorconfig`, or analyzer policy should be added.
- Whether the default MIDI output device should remain fixed to device `0` or become configurable.
