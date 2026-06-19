# AI Review And Improvement Plan

## Analysis Date

- 2026-06-19

## Analysis Scope

- Repository structure and long-term context files:
  - `README.md`
  - `AGENTS.md`
  - `.gitignore`
  - `docs/task-template.md`
- Build, package, and CI configuration:
  - `.github/workflows/dotnet.yml`
  - `src/Edi.MIDIPlayer.slnx`
  - `src/Edi.MIDIPlayer/Edi.MIDIPlayer.csproj`
  - `src/Edi.MIDIPlayer/Properties/PublishProfiles/FolderProfile.pubxml`
- Main application and service code:
  - `src/Edi.MIDIPlayer/Program.cs`
  - `src/Edi.MIDIPlayer/Hubs/MidiPlayerHub.cs`
  - `src/Edi.MIDIPlayer/Interfaces/*.cs`
  - `src/Edi.MIDIPlayer/Models/*.cs`
  - `src/Edi.MIDIPlayer/Services/*.cs`
- Web visualizer assets:
  - `src/Edi.MIDIPlayer/wwwroot/index.html`
  - `src/Edi.MIDIPlayer/wwwroot/app.js`
  - `src/Edi.MIDIPlayer/wwwroot/styles.css`
  - `src/wwwroot/app.js`

No build, test, lint, format, restore, package, dependency update, or runtime command was executed during this review because the user explicitly limited this turn to read-only analysis plus this memory file.

## Overall Conclusion

- Overall risk level: medium.
- The current codebase is small and mostly understandable. The display-mode split through interfaces is a good fit for the project size, and the playback flow is easy to trace.
- The highest-priority risks are:
  - MIDI device creation happens before the no-device check, so the intended graceful error path may not run.
  - Packaging/release configuration was inconsistent with the CI `dotnet pack` goal and README note; this was completed by Task 2 on 2026-06-19.
  - Remote MIDI downloads read the full response into memory without a size limit.
  - Browser log rendering uses `innerHTML` with messages that can include user-provided input.
- The best near-term direction is incremental hardening and focused tests, not a broad rewrite.
- Not recommended now:
  - Replacing the minimal web UI with a frontend framework.
  - Cross-platform MIDI output support.
  - Large architecture rewrites or new dependency layers.
  - Blind dependency upgrades without a clear compatibility or security reason.

## User Confirmations

Confirmed by the user on 2026-06-19:

- `Edi.MIDIPlayer.csproj` may explicitly set `IsPackable=true`.
- `GeneratePackageOnBuild` should not be retained unless there is a clear need.
- New tests should use xUnit v3 and Moq.
- Remote MIDI downloads must be less than 10 MB.
- Remote URLs should be restricted to `.mid` / `.midi`.
- `--urls` should be treated as an officially supported option.
- Console exit pause should be opt-in.
- Active note state should distinguish MIDI channels.
- `waterfallCanvas` is not intended for a future feature.
- `src/wwwroot/app.js` may be deleted if confirmed unused.
- `Microsoft.AspNetCore.SignalR.Client` may be removed if confirmed unused.

## Execution Updates

### 2026-06-19: Task 1 Completed

- Implemented the MIDI output device availability check before opening `MidiOut`.
- Added `MidiDeviceWrapper.AvailableDeviceCount` for construction-free device count checks.
- Removed the instance `IMidiDeviceWrapper.NumberOfDevices` member so future code does not need to construct the wrapper just to check availability.
- Updated `README.md` and `AGENTS.md` to reflect the no-device behavior and current testing preference.
- Verification:
  - `dotnet build --configuration Release` from `src/`: passed. At the time it emitted the known packaging warning about `IsPackable=true`; Task 2 later resolved the packaging configuration.
  - Manual no-device playback validation: not performed in this environment.

### 2026-06-19: Task 2 Completed

- Updated `Edi.MIDIPlayer.csproj` to explicitly set `IsPackable=true`.
- Removed `GeneratePackageOnBuild` so regular builds no longer attempt to package automatically.
- Removed the duplicate `PackageId` property.
- Updated `README.md` and `AGENTS.md` packaging notes.
- Verification:
  - `dotnet pack --configuration Release -o nupkg` from `src/`: passed.
  - Generated package: `src/nupkg/Edi.MIDIPlayer.2.0.0.nupkg`.

### 2026-06-19: Task 3 Completed

- Added a task record at `docs/task-harden-remote-download.md`.
- Added remote URL validation so HTTP/HTTPS MIDI sources must end in `.mid` or `.midi`.
- Updated `FileDownloaderService` to stream responses with `ResponseHeadersRead` and reject remote files at or above 10 MB.
- Added explicit timeout handling for downloader `TimeoutException`.
- Updated `README.md` and `AGENTS.md` with remote download constraints.
- Verification:
  - `dotnet build --configuration Release` from `src/`: passed with 0 warnings and 0 errors.

### 2026-06-19: Task 4 Completed

- Added a task record at `docs/task-align-web-urls.md`.
- Updated `AppOptions` to capture the configured `--urls` value.
- Updated web mode so default startup still uses `http://localhost:5000`, while explicit `--urls` values are used for ASP.NET Core binding through host configuration.
- Updated browser launch to open the first configured URL and convert wildcard hosts (`*`, `+`, `0.0.0.0`, `::`) to `localhost`.
- Updated `README.md` and `AGENTS.md` with `--urls` behavior.
- Verification:
  - `dotnet build --configuration Release` from `src/`: passed with 0 warnings and 0 errors.

### 2026-06-19: Task 9 Completed

- Added a task record at `docs/task-extract-program-responsibilities.md`.
- Moved `DisplayMode` and `AppOptions` parsing from nested `Program` types into `src/Edi.MIDIPlayer/AppOptions.cs`.
- Left DI registration in `Program.cs` for now to keep the refactor narrow.
- Updated `README.md` and `AGENTS.md` to document the new command-line options source file.
- Verification:
  - `dotnet build --configuration Release` from `src/`: passed with 0 warnings and 0 errors.

## Issues

| ID | Priority | Type | Location | Description | Impact | Evidence | Suggested Direction |
|---|---|---|---|---|---|---|---|
| R1 | P1 | Stability | `src/Edi.MIDIPlayer/Services/MidiDeviceWrapper.cs`; `src/Edi.MIDIPlayer/Services/MidiPlayerService.cs` | Completed on 2026-06-19: no-device handling now checks available devices before constructing `MidiOut`. | On machines without a MIDI output device, playback should now show the intended "No MIDI output devices available" message instead of failing during wrapper construction. | `MidiPlayerService` checks `MidiDeviceWrapper.AvailableDeviceCount == 0` before `new MidiDeviceWrapper()`; the instance `NumberOfDevices` member was removed. | Keep this path covered by a future test seam or no-device manual validation. |
| R2 | P1 | Release/Packaging | `src/Edi.MIDIPlayer/Edi.MIDIPlayer.csproj`; `.github/workflows/dotnet.yml`; `README.md` | Completed on 2026-06-19: packaging metadata now explicitly enables packing and removes build-time auto-pack behavior. | CI can rely on the explicit `dotnet pack` step instead of ordinary build auto-pack. | `Edi.MIDIPlayer.csproj` now has `IsPackable=true`, keeps `PackAsTool=true`, removes `GeneratePackageOnBuild`, and has a single `PackageId`; `dotnet pack --configuration Release -o nupkg` passed. | Keep generated `.nupkg` files out of source control. |
| R3 | P2 | Stability/Security | `src/Edi.MIDIPlayer/Services/FileDownloaderService.cs`; `src/Edi.MIDIPlayer/Services/MidiPlayerService.cs` | Completed on 2026-06-19: remote downloads now stream responses, reject files at or above 10 MB, and require `.mid` / `.midi` URLs. | Very large or non-MIDI-looking remote URLs should be rejected before consuming excessive memory. | `FileDownloaderService.DownloadAsync` uses `HttpCompletionOption.ResponseHeadersRead` and enforces `MaxDownloadBytes`; `MidiPlayerService` checks remote URL extensions before downloading; build passed. | Add focused tests in Task 10. |
| R4 | P2 | Configuration/Usability | `src/Edi.MIDIPlayer/Program.cs`; `AGENTS.md` | Completed on 2026-06-19: `--urls` is captured and used for web binding and browser launch. | Users passing `--urls` should get matching server binding and browser startup behavior. | `AppOptions` captures `WebUrls`; `RunWebAsync` uses host-configured URLs when present and opens the first configured URL in the browser; build passed. | Add parser tests in Task 10. |
| R5 | P2 | Stability/Maintainability | `src/Edi.MIDIPlayer/Program.cs`; `src/Edi.MIDIPlayer/Services/WebDisplayService.cs`; `src/Edi.MIDIPlayer/Services/WebNoteProcessorService.cs` | Web playback and SignalR notifications are fire-and-forget. | Exceptions can be unobserved or silently ignored; high event volume has no backpressure; message ordering is harder to reason about. | `Program.RunWebAsync` starts playback with `_ = Task.Run(...)`; web display and note processors use `_ = hubContext.Clients.All.SendAsync(...)`. | Introduce an explicit background service or async notification path with logged failures. Keep the first pass small. |
| R6 | P2 | Security | `src/Edi.MIDIPlayer/wwwroot/app.js`; `src/Edi.MIDIPlayer/Services/MidiPlayerService.cs` | Browser event log uses `innerHTML` for messages that can include user-provided strings. | Local self-XSS is possible if a MIDI URL/path or error message contains HTML. Risk is lower if the browser is only controlled by the local user, but this is still easy to harden. | `addLogEntry` interpolates `timestamp`, `type`, and `message` into `entry.innerHTML`; server messages include strings such as `Downloading MIDI file from: {fileUrl}` and exception messages. | Build log rows with DOM nodes and `textContent` instead of `innerHTML`. |
| R7 | P2 | Correctness | `src/Edi.MIDIPlayer/Services/MidiPlayerService.cs`; `src/Edi.MIDIPlayer/wwwroot/app.js` | Active note tracking is keyed only by MIDI note number. | The active note count and key highlighting can be wrong for the same note on multiple channels or overlapping note-on events. | `MidiPlayerService` uses `HashSet<int>` and adds/removes `noteEvent.NoteNumber`; browser `app.js` also uses `Set` of note numbers. | Track active notes by channel plus note number, and decide whether overlapping same-channel notes need reference counting. |
| R8 | P2 | Error Handling | `src/Edi.MIDIPlayer/Services/FileDownloaderService.cs`; `src/Edi.MIDIPlayer/Services/MidiPlayerService.cs` | Download timeout handling is inconsistent. | Timeout errors may be shown as generic execution failures instead of the more specific timeout message. | `FileDownloaderService` throws `TimeoutException`; `MidiPlayerService` only has a specific catch for `TaskCanceledException` with `TimeoutException` inner exception, then falls through to generic `catch (Exception)`. | Catch `TimeoutException` explicitly or avoid converting the cancellation exception. |
| R9 | P2 | CLI Usability | `src/Edi.MIDIPlayer/Program.cs` | Console mode always clears the terminal and waits for a key before exit. | This makes scripted usage awkward and can hang in non-interactive environments. | `RunConsoleAsync` calls `Console.Clear()` and in `finally` writes "Press any key to exit..." then calls `Console.ReadKey()`. | Gate interactive behavior behind `Environment.UserInteractive` or an explicit option, and preserve the current behavior only when appropriate. |
| R10 | P2 | Code Structure | `src/Edi.MIDIPlayer/Program.cs`; `src/Edi.MIDIPlayer/AppOptions.cs` | Completed on 2026-06-19: command-line parsing moved out of `Program.cs`; startup and DI registration remain in `Program.cs`. | Future CLI parsing changes are more localized and easier to test. | `AppOptions.cs` now contains `DisplayMode`, `AppOptions`, and parser helpers; `Program.cs` calls `AppOptions.Parse`; build passed. | Add parser tests in Task 10. |
| R11 | P3 | Maintainability | `src/Edi.MIDIPlayer/Services/NoteProcessorService.cs`; `src/Edi.MIDIPlayer/Services/WebNoteProcessorService.cs` | Note-name and controller-name logic is duplicated. | Small updates can drift between console and web display paths. | Both classes define `NoteNames`, `GetNoteName`, `GetNoteColor`, and `GetControllerName`. | Move shared MIDI display formatting helpers into a small internal static helper or service. |
| R12 | P3 | Architecture | `src/Edi.MIDIPlayer/Interfaces/IConsoleDisplay.cs`; display services | `IConsoleDisplay` is used for both console and web display, but it exposes console-specific members. | Web display implements methods that do not naturally belong to it, making the abstraction less clear. | `WebDisplayService : IConsoleDisplay` returns a lock and `CreateVelocityBar` returns `velocity.ToString()`. | Split display status output from console rendering helpers, after higher-priority fixes. |
| R13 | P3 | Frontend Maintainability | `src/Edi.MIDIPlayer/wwwroot/index.html`; `src/Edi.MIDIPlayer/wwwroot/app.js`; `src/wwwroot/app.js`; `AGENTS.md` | There are likely unused or leftover frontend artifacts. | Future maintainers may spend time looking for missing behavior or accidentally edit the wrong file. | `index.html` contains `<canvas id="waterfallCanvas"></canvas>`, but no CSS/JS reference was found except the element itself; `src/wwwroot/app.js` exists and is empty; AGENTS already lists the empty file as to confirm. | Confirm intent, then remove or implement the placeholder canvas and delete the stray empty file if unused. |
| R14 | P3 | Framework/Dependency Use | `src/Edi.MIDIPlayer/wwwroot/index.html`; `src/Edi.MIDIPlayer/Edi.MIDIPlayer.csproj` | SignalR assets and package references need confirmation. | Version drift or unused dependencies can confuse maintenance and troubleshooting. | Browser loads `microsoft-signalr/9.0.6`; the project references `Microsoft.AspNetCore.SignalR.Client` `10.0.1`, which is not used by the server-side code reviewed. | Confirm whether the package is needed and whether browser JS should track the server/runtime version. |
| R15 | P3 | Performance | `src/Edi.MIDIPlayer/Services/TempoManagerService.cs`; `src/Edi.MIDIPlayer/Services/MidiPlayerService.cs` | Tick-to-time conversion recomputes from the beginning of the tempo map for each event. | MIDI files with many events and many tempo changes can spend unnecessary CPU in timing conversion. For typical files this may not matter. | `PlayEventsAsync` calls `tempoManager.TicksToTimeSpan` for every event; `TicksToTimeSpan` loops over `tempoMap` from index 0 each time. | Only optimize if profiling or real files show this matters. A cumulative tempo map can be added later without changing behavior. |
| R16 | P3 | Readability/Configuration | Multiple files | Several operational values are hard-coded. | Behavior changes require code edits and the intended defaults are not centralized. | Examples include web URL `http://localhost:5000`, browser/playback startup delays `2000`/`3000` ms, MIDI device ID `0`, download timeout `30` seconds, HTTP client timeout `5` minutes, and browser log limit `50`. | Centralize only values that are user-visible or likely to change. Avoid over-configuring internal constants. |

## Phased Improvement Plan

### Task 1: Make MIDI Device Availability Handling Reliable

- Status: completed on 2026-06-19.
- Priority: P1
- Related issues: R1
- Goal: Preserve the intended graceful error message when no MIDI output device is available.
- Change scope:
  - `MidiDeviceWrapper`
  - `MidiPlayerService`
  - Optionally `IMidiDeviceWrapper` or a small factory if needed.
- Excluded:
  - Device selection UI.
  - Configurable device ID.
  - Cross-platform MIDI output.
- Expected result:
  - The app checks `MidiOut.NumberOfDevices` before opening device `0`.
  - No-device machines get a clear error instead of a constructor-time failure.
- Verification:
  - Build the solution.
  - Run console mode on a machine with no MIDI devices or use a test seam/fake wrapper.
  - Run a normal local MIDI file on a machine with a valid MIDI output device.
- Release risk: low.
- Rollback plan:
  - Revert the wrapper/factory change.
- Needs user confirmation: no.
- Questions to confirm:
  - None for the minimal fix.

### Task 2: Confirm And Fix Packaging Configuration

- Status: completed on 2026-06-19.
- Priority: P1
- Related issues: R2
- Goal: Make local and CI packaging behavior match the project's NuGet/global-tool intent.
- Change scope:
  - `src/Edi.MIDIPlayer/Edi.MIDIPlayer.csproj`
  - `.github/workflows/dotnet.yml` only if CI command ordering needs adjustment.
  - `README.md` and `AGENTS.md` if behavior changes.
- Excluded:
  - Versioning automation.
  - Publishing workflow redesign.
  - Dependency upgrades.
- Expected result:
  - `dotnet pack --configuration Release -o nupkg` succeeds from `src/`.
  - Duplicate or confusing package metadata is cleaned up.
- Verification:
  - After user approval, run `dotnet pack --configuration Release -o nupkg` from `src/`.
  - Optionally inspect the generated `.nupkg` contents.
- Release risk: medium, because it affects publishing.
- Rollback plan:
  - Revert project/CI metadata changes.
- Needs user confirmation: no, already confirmed on 2026-06-19.
- Confirmed decisions:
  - Explicitly setting `IsPackable=true` is allowed.
  - Remove `GeneratePackageOnBuild` unless implementation discovers a clear need to keep it.

### Task 3: Harden Remote Download Behavior

- Status: completed on 2026-06-19.
- Priority: P2
- Related issues: R3, R8
- Goal: Prevent oversized remote responses from being loaded into memory and make timeout errors clear.
- Change scope:
  - `IFileDownloader`
  - `FileDownloaderService`
  - `MidiPlayerService` error handling
  - Documentation if new limits become user-visible.
- Excluded:
  - Authentication.
  - Remote host allowlists.
  - MIME-type enforcement unless later evidence shows extension checks are insufficient.
- Expected result:
  - Downloads are limited to less than 10 MB.
  - Remote URLs are restricted to `.mid` / `.midi`.
  - Timeout messages are specific and consistent.
  - HTTP error messages remain clear.
- Verification:
  - Unit tests for timeout and oversize paths if a test project exists or is added.
  - Manual test with a small remote `.mid` URL.
  - Manual or local test endpoint for an oversized response.
- Release risk: low to medium, depending on the selected size limit.
- Rollback plan:
  - Restore previous download implementation.
- Needs user confirmation: no, already confirmed on 2026-06-19.
- Confirmed decisions:
  - Maximum remote MIDI size must be less than 10 MB.
  - Reject remote URLs that do not end in `.mid` or `.midi`.

### Task 4: Align Web URL Binding And Browser Launch

- Status: completed on 2026-06-19.
- Priority: P2
- Related issues: R4, R16
- Goal: Make web mode behavior match documented host options and user expectations.
- Change scope:
  - `Program.RunWebAsync`
  - `AppOptions` if URL parsing needs to expose the chosen launch URL.
  - README/AGENTS command notes.
- Excluded:
  - Full hosting configuration redesign.
  - HTTPS certificate setup.
- Expected result:
  - `--urls` is honored for both binding and browser launch.
  - Default web mode keeps the current local URL behavior unless intentionally changed.
- Verification:
  - Run default web mode and confirm it opens the expected local URL.
  - Run with a custom `--urls` value and confirm binding behavior.
  - Confirm `/midihub` still connects.
- Release risk: medium, because URL behavior is user-visible.
- Rollback plan:
  - Revert to the hard-coded `http://localhost:5000` behavior.
- Needs user confirmation: no, already confirmed on 2026-06-19.
- Confirmed decisions:
  - `--urls` is an officially supported user option.

### Task 5: Remove Browser Log HTML Injection Risk

- Priority: P2
- Related issues: R6
- Goal: Render all event-log content as text rather than HTML.
- Change scope:
  - `src/Edi.MIDIPlayer/wwwroot/app.js`
- Excluded:
  - Visual redesign.
  - SignalR event contract changes.
- Expected result:
  - The event log keeps the same appearance but no longer interpolates unsanitized strings into `innerHTML`.
- Verification:
  - Open the web visualizer.
  - Send or simulate messages containing characters such as `<`, `>`, `"`, and `&`.
  - Confirm the text renders literally and layout remains intact.
- Release risk: low.
- Rollback plan:
  - Revert the JS log rendering change.
- Needs user confirmation: no.
- Questions to confirm:
  - None.

### Task 6: Make Web Notifications Observable

- Priority: P2
- Related issues: R5
- Goal: Avoid silently ignoring SignalR send failures and make web playback startup errors visible.
- Change scope:
  - `Program.RunWebAsync`
  - `WebDisplayService`
  - `WebNoteProcessorService`
  - Possibly interface signatures if moving to async sends.
- Excluded:
  - Large event bus architecture.
  - Persistent message storage.
- Expected result:
  - Startup/playback failures are logged or displayed.
  - SignalR notification failures are observable.
  - Playback behavior remains responsive.
- Verification:
  - Run web mode with and without connected browser clients.
  - Temporarily simulate a send failure in a test or local branch.
  - Confirm no unobserved task failures are expected.
- Release risk: medium, because it may touch interfaces.
- Rollback plan:
  - Revert to fire-and-forget sends if behavior changes unexpectedly.
- Needs user confirmation: no for a minimal logging-focused change; yes if changing public interface shape broadly.
- Questions to confirm:
  - Is it acceptable for playback to continue if visualizer notification fails?

### Task 7: Correct Active Note Tracking Semantics

- Priority: P2
- Related issues: R7
- Goal: Represent active notes accurately across MIDI channels and common overlapping-note cases.
- Change scope:
  - `MidiPlayerService`
  - `src/Edi.MIDIPlayer/wwwroot/app.js`
  - SignalR payloads only if required.
- Excluded:
  - New visualizer features.
  - MIDI synthesis behavior changes.
- Expected result:
  - Same pitch on different channels no longer collapses into one active note.
  - Active note count and key highlighting behavior are documented and predictable.
- Verification:
  - Test with a MIDI file containing the same note on multiple channels.
  - Test ordinary single-channel playback.
  - Confirm final active note count returns to zero for well-formed files.
- Release risk: medium, because display behavior changes.
- Rollback plan:
  - Revert active note representation changes.
- Needs user confirmation: no, already confirmed on 2026-06-19.
- Confirmed decisions:
  - Active note state should distinguish MIDI channels.

### Task 8: Improve Console Mode Non-Interactive Behavior

- Priority: P2
- Related issues: R9
- Goal: Let console mode finish cleanly in scripted/non-interactive contexts.
- Change scope:
  - `Program.RunConsoleAsync`
  - README/AGENTS usage notes if behavior changes.
- Excluded:
  - Redesigning the console visualizer.
  - Removing interactive prompt mode.
- Expected result:
  - Interactive users can still see completion messages.
  - Console exit pause is opt-in.
  - Scripts do not hang on `Console.ReadKey()` by default.
- Verification:
  - Run console mode interactively.
  - Run console mode with redirected input/output or a non-interactive test harness.
- Release risk: low to medium, because users may be used to the pause.
- Rollback plan:
  - Restore unconditional `Console.ReadKey()`.
- Needs user confirmation: no, already confirmed on 2026-06-19.
- Confirmed decisions:
  - Add or use opt-in behavior for pause-on-exit instead of pausing by default.

### Task 9: Extract Small, Stable Program Responsibilities

- Status: completed on 2026-06-19.
- Priority: P2
- Related issues: R10
- Goal: Make future host/CLI changes safer without changing behavior.
- Change scope:
  - Move `AppOptions` parsing to a focused file.
  - Optionally move repeated service-registration code to small extension methods.
- Excluded:
  - New command-line parser dependency.
  - Full application framework restructuring.
- Expected result:
  - `Program.cs` remains the entry point but no longer owns every detail.
  - CLI parsing can be tested directly.
- Verification:
  - Existing commands still parse:
    - default web mode
    - `--web`
    - `--console`
    - `--display`, `--mode`, `--ui`
    - inline `--display=...`
    - host options.
  - Build and test after user approval.
- Release risk: low if behavior remains unchanged.
- Rollback plan:
  - Move code back into `Program.cs`.
- Needs user confirmation: no, once higher-priority behavior fixes are complete.
- Questions to confirm:
  - None.

### Task 10: Add Focused Characterization Tests

- Priority: P2
- Related issues: R1, R3, R4, R7, R8, R9, R10
- Goal: Protect the current behavior before and during incremental refactors.
- Change scope:
  - Add a dedicated xUnit v3 test project with Moq.
  - Start with pure logic and service tests that do not require a real MIDI device.
- Excluded:
  - Browser end-to-end automation unless needed for a UI change.
  - Hardware MIDI integration tests in CI.
- Expected result:
  - CLI parsing, tempo conversion, download error handling, and active-note logic are covered.
- Verification:
  - `dotnet test --configuration Release` from `src/`.
- Release risk: low.
- Rollback plan:
  - Remove the test project if it blocks CI unexpectedly.
- Needs user confirmation: no, already confirmed on 2026-06-19.
- Confirmed decisions:
  - Use xUnit v3 and Moq.

### Task 11: Clean Up Shared Display Helpers And Interfaces

- Priority: P3
- Related issues: R11, R12
- Goal: Reduce duplication and clarify display responsibilities after behavior is protected.
- Change scope:
  - Shared note/controller formatting helper.
  - `IConsoleDisplay` split or rename if still useful after previous tasks.
- Excluded:
  - Display redesign.
  - New abstractions beyond the small helper/interface cleanup.
- Expected result:
  - Console and web display paths share stable MIDI formatting logic.
  - Web display no longer needs console-specific stub behavior.
- Verification:
  - Unit tests for helper formatting.
  - Manual web and console playback smoke tests.
- Release risk: low to medium, because interfaces may move.
- Rollback plan:
  - Revert helper/interface extraction.
- Needs user confirmation: no.
- Questions to confirm:
  - None.

### Task 12: Resolve Frontend And Dependency Loose Ends

- Priority: P3
- Related issues: R13, R14, R16
- Goal: Remove ambiguity in frontend assets and dependency usage.
- Change scope:
  - `src/Edi.MIDIPlayer/wwwroot/index.html`
  - `src/Edi.MIDIPlayer/wwwroot/app.js`
  - `src/wwwroot/app.js`
  - `src/Edi.MIDIPlayer/Edi.MIDIPlayer.csproj`
  - README/AGENTS documentation.
- Excluded:
  - Full visual redesign.
  - Dependency upgrades without a confirmed reason.
- Expected result:
  - `waterfallCanvas` is removed because it is not intended for a future feature.
  - `src/wwwroot/app.js` is removed if confirmed unused by repository search.
  - SignalR JS/package version policy is documented.
  - `Microsoft.AspNetCore.SignalR.Client` is removed if confirmed unused by compile-time/reference search.
  - User-visible constants are centralized only where useful.
- Verification:
  - Web visualizer loads without console errors.
  - `/midihub` connects.
  - Package restore/build after dependency reference changes.
- Release risk: low.
- Rollback plan:
  - Revert individual asset/dependency changes.
- Needs user confirmation: no, as long as unused status is confirmed by search before deletion/removal.
- Confirmed decisions:
  - `waterfallCanvas` has no future purpose.
  - `src/wwwroot/app.js` may be deleted if confirmed unused.
  - `Microsoft.AspNetCore.SignalR.Client` may be removed if confirmed unused.

## Recommended Execution Order

1. Task 1: Make MIDI device availability handling reliable.
2. Task 2: Confirm and fix packaging configuration.
3. Task 10: Add focused characterization tests, at least for logic that will be touched by later tasks.
4. Task 3: Harden remote download behavior.
5. Task 5: Remove browser log HTML injection risk.
6. Task 4: Align web URL binding and browser launch.
7. Task 6: Make web notifications observable.
8. Task 7: Correct active note tracking semantics.
9. Task 8: Improve console mode non-interactive behavior.
10. Task 9: Extract small, stable `Program` responsibilities.
11. Task 11: Clean up shared display helpers and interfaces.
12. Task 12: Resolve frontend and dependency loose ends.

## Temporarily Not Recommended

- Cross-platform MIDI output:
  - The project explicitly exits on non-Windows platforms, and changing this would require product and dependency decisions beyond a maintenance pass.
- Replacing the current static web visualizer with React/Vue/Svelte:
  - The current UI is small and does not justify the build and dependency cost.
- Adding a broad plugin/device architecture:
  - Device selection may be useful later, but the first fix should only make device availability safe.
- Aggressive performance work on tempo conversion:
  - The complexity is not justified until profiling or real files show it matters.
- Broad security controls such as URL allowlists:
  - This is a local CLI tool today. Confirm the actual threat model before adding policies that may block legitimate remote MIDI usage.

## Open Questions For The User

- None currently blocking the planned improvement tasks.
- Before deleting `src/wwwroot/app.js`, confirm it is still unused with repository search.
- Before removing `Microsoft.AspNetCore.SignalR.Client`, confirm it is still unused with repository search and build/test after the change.

## Execution Notes For Future Work

- Respect the repository rule to update `README.md`, `AGENTS.md`, and related docs after any behavior, tooling, architecture, or workflow change.
- Keep changes small and independently verifiable.
- Do not combine refactoring, dependency changes, and behavior fixes in the same task.
- For tasks that touch playback, validate at least local MIDI playback, remote download playback, web visualizer startup, console mode, and no-device behavior when feasible.
- For web UI tasks, verify `/midihub` connects and the browser still handles `ReceiveNoteOn`, `ReceiveNoteOff`, `ReceiveControlChange`, and `ReceiveMessage`.
- Do not run build/test/pack commands without user approval when the active user request forbids side effects.
