# AI Review And Improvement Plan

## Baseline Date

- 2026-06-19

## Scope

This file is the current improvement baseline after Tasks 1, 2, 3, 4, and 9 were completed. It replaces the original long-form review plan as the active planning document.

Reviewed scope remains:

- `README.md`
- `AGENTS.md`
- `.github/workflows/dotnet.yml`
- `src/Edi.MIDIPlayer.slnx`
- `src/Edi.MIDIPlayer/Edi.MIDIPlayer.csproj`
- `src/Edi.MIDIPlayer/Program.cs`
- `src/Edi.MIDIPlayer/AppOptions.cs`
- `src/Edi.MIDIPlayer/Hubs/`
- `src/Edi.MIDIPlayer/Interfaces/`
- `src/Edi.MIDIPlayer/Models/`
- `src/Edi.MIDIPlayer/Services/`
- `src/Edi.MIDIPlayer/wwwroot/`
- `src/wwwroot/app.js`
- `docs/`

## Completed Baseline

The following original tasks are complete and removed from the active task list:

| Original Task | Status | Notes |
|---|---|---|
| Task 1: Make MIDI device availability handling reliable | Completed | `MidiPlayerService` checks device availability before constructing `MidiOut`. |
| Task 2: Confirm and fix packaging configuration | Completed | `IsPackable=true`; `GeneratePackageOnBuild` removed; duplicate `PackageId` removed; `dotnet pack` passed. |
| Task 3: Harden remote download behavior | Completed | Remote URLs must end in `.mid` / `.midi`; downloads are streamed and must be smaller than 10 MB; timeout handling improved. |
| Task 4: Align web URL binding and browser launch | Completed | `--urls` is captured by `AppOptions` and used for web binding/browser launch. |
| Task 9: Extract small, stable `Program` responsibilities | Completed | `DisplayMode` and `AppOptions` moved to `src/Edi.MIDIPlayer/AppOptions.cs`. |

Detailed execution records:

- `docs/task-harden-remote-download.md`
- `docs/task-align-web-urls.md`
- `docs/task-extract-program-responsibilities.md`

## Current Overall Conclusion

- Overall risk level: medium-low.
- The most valuable next step is to add focused characterization tests, because several behavior changes and a small refactor are now in place without automated coverage.
- The highest remaining product/code risks are:
  - Browser log rendering still uses `innerHTML`.
  - Web SignalR notifications are still fire-and-forget.
  - Active note state still does not distinguish MIDI channels.
  - Console mode still pauses on exit by default.
- Broad architecture rewrites, frontend framework adoption, and cross-platform MIDI output are still not recommended.

## User Confirmations

Confirmed by the user on 2026-06-19:

- Tests should use xUnit v3 and Moq.
- Remote MIDI downloads must be smaller than 10 MB.
- Remote URLs should be restricted to `.mid` / `.midi`.
- `--urls` is an officially supported option.
- Console exit pause should be opt-in.
- Active note state should distinguish MIDI channels.
- `waterfallCanvas` is not intended for a future feature.
- `src/wwwroot/app.js` may be deleted if confirmed unused.
- `Microsoft.AspNetCore.SignalR.Client` may be removed if confirmed unused.

## Remaining Issues

| ID | Priority | Type | Location | Issue | Impact | Evidence | Suggested Direction |
|---|---|---|---|---|---|---|---|
| R5 | P2 | Stability/Maintainability | `Program.cs`, `WebDisplayService.cs`, `WebNoteProcessorService.cs` | Web playback startup and SignalR notifications are fire-and-forget. | Exceptions can be unobserved or silently ignored; event ordering/backpressure is harder to reason about. | `Program.RunWebAsync` uses `_ = Task.Run(...)`; web display/note processors use `_ = hubContext.Clients.All.SendAsync(...)`. | Add observable failure handling with the smallest interface changes possible. |
| R6 | P2 | Security | `src/Edi.MIDIPlayer/wwwroot/app.js` | Browser event log uses `innerHTML` for message rendering. | Local self-XSS is possible from user-provided URLs/paths or exception messages. | `addLogEntry` interpolates `timestamp`, `type`, and `message` into `entry.innerHTML`. | Build log entries with DOM nodes and `textContent`. |
| R7 | P2 | Correctness | `MidiPlayerService.cs`, `wwwroot/app.js` | Active notes are keyed only by MIDI note number. | Same pitch on different channels or overlapping note-on events can display incorrect active counts/highlighting. | Server uses `HashSet<int>`; browser uses `Set` of note numbers. | Track active state by channel plus note; consider reference counts for overlapping same-channel notes. |
| R9 | P2 | CLI Usability | `Program.cs`, `AppOptions.cs` | Console mode always clears the terminal and waits for a key before exit. | Scripts and non-interactive usage can hang or lose terminal output. | `RunConsoleAsync` calls `Console.Clear()` and always calls `Console.ReadKey()` in `finally`. | Add an opt-in pause flag and avoid clearing/pausing in non-interactive flows. |
| R11 | P3 | Maintainability | `NoteProcessorService.cs`, `WebNoteProcessorService.cs` | Note-name and controller-name logic is duplicated. | Display formatting changes can drift between console and web paths. | Both classes define note-name and controller-name helpers. | Move shared MIDI display formatting into a small internal helper. |
| R12 | P3 | Architecture | `IConsoleDisplay.cs`, display services | `IConsoleDisplay` is used for both console and web but exposes console-specific members. | Web display implements methods that do not naturally belong to it. | `WebDisplayService` returns a lock and uses a stub-like `CreateVelocityBar`. | Split status output from console rendering helpers after tests are in place. |
| R13 | P3 | Frontend Maintainability | `wwwroot/index.html`, `src/wwwroot/app.js` | Unused frontend artifacts remain. | Maintainers can edit the wrong file or expect unimplemented behavior. | `waterfallCanvas` has no future purpose; `src/wwwroot/app.js` is empty and should be deleted if still unused. | Remove confirmed unused artifacts and update docs. |
| R14 | P3 | Dependency/Framework Use | `Edi.MIDIPlayer.csproj`, `index.html` | SignalR dependency/version usage remains unclear. | Unused dependencies and version drift can confuse maintenance. | Browser loads SignalR JS from CDN; project references `Microsoft.AspNetCore.SignalR.Client`. | Confirm current references, remove unused package if safe, and document JS version policy. |
| R15 | P3 | Performance | `TempoManagerService.cs`, `MidiPlayerService.cs` | Tick-to-time conversion scans the tempo map from the beginning for each event. | Large MIDI files with many tempo changes could spend unnecessary CPU. | `TicksToTimeSpan` loops from index 0 per event. | Defer until profiling or real-world files show a problem. |
| R16 | P3 | Readability/Configuration | Multiple files | Some operational values remain hard-coded. | Behavior changes still require code edits. | Startup delays, MIDI device ID, download timeout, browser log limit, and similar values are scattered. | Centralize only user-visible or likely-to-change values; avoid over-configuring internals. |

## Remaining Improvement Plan

### Task A: Add Focused Characterization Tests

- Previous task: Task 10.
- Priority: P2.
- Related issues: R5, R6, R7, R9, R11, R12, R13, R14; regression coverage for completed Tasks 1, 3, 4, and 9.
- Goal: Establish automated coverage before more behavior changes.
- Change scope:
  - Add a dedicated xUnit v3 test project with Moq.
  - Cover pure logic first:
    - `AppOptions.Parse` display modes, `--urls`, host arguments, MIDI args, help, and invalid values.
    - remote URL validation and download limit behavior using mocked/fake `HttpMessageHandler` or focused seams.
    - tempo conversion behavior for stable sample inputs.
  - Add thin tests around display-independent active-note logic only if a seam is introduced during Task D.
- Not included:
  - Hardware MIDI output tests in CI.
  - Browser end-to-end automation.
  - Large refactors just for testing.
- Expected result:
  - `dotnet test --configuration Release` runs meaningful tests.
  - Future tasks can change behavior with less risk.
- Verification:
  - `dotnet test --configuration Release` from `src/`.
  - `dotnet build --configuration Release`.
- Release risk: low.
- Rollback plan:
  - Remove the test project if it unexpectedly blocks CI.
- Needs user confirmation: no.

### Task B: Remove Browser Log HTML Injection Risk

- Previous task: Task 5.
- Priority: P2.
- Related issues: R6.
- Goal: Render browser event log entries as text, not HTML.
- Change scope:
  - `src/Edi.MIDIPlayer/wwwroot/app.js`.
- Not included:
  - Visual redesign.
  - SignalR event contract changes.
- Expected result:
  - Log appearance remains effectively the same.
  - Messages containing `<`, `>`, `"`, and `&` render literally.
- Verification:
  - Build.
  - Run web visualizer.
  - Simulate or trigger messages containing HTML-like characters.
- Release risk: low.
- Rollback plan:
  - Revert the JS rendering change.
- Needs user confirmation: no.

### Task C: Make Console Exit Pause Opt-In

- Previous task: Task 8.
- Priority: P2.
- Related issues: R9.
- Goal: Avoid blocking scripted/non-interactive console use by default.
- Change scope:
  - `AppOptions.cs`
  - `Program.RunConsoleAsync`
  - README/AGENTS usage docs.
- Not included:
  - Console visualizer redesign.
  - Removing interactive MIDI path prompt.
- Expected result:
  - Console mode does not pause by default.
  - A clear opt-in option, such as `--pause-on-exit`, preserves the old pause behavior when requested.
  - Terminal clearing is reviewed and either kept only for interactive usage or made opt-in if needed.
- Verification:
  - Parser tests from Task A.
  - `dotnet run --project Edi.MIDIPlayer -- --display console ...` with and without pause option.
- Release risk: low to medium.
- Rollback plan:
  - Restore unconditional pause if users depend on it.
- Needs user confirmation: no; opt-in behavior was already confirmed.

### Task D: Correct Active Note Tracking Semantics

- Previous task: Task 7.
- Priority: P2.
- Related issues: R7.
- Goal: Track active notes by channel and note so web/console counts reflect MIDI semantics more accurately.
- Change scope:
  - `MidiPlayerService`
  - `src/Edi.MIDIPlayer/wwwroot/app.js`
  - SignalR payloads only if needed.
- Not included:
  - New visualizer features.
  - MIDI synthesis changes.
- Expected result:
  - Same pitch on different channels no longer collapses into one active note.
  - Overlapping same-channel note behavior is explicit, ideally with reference counts.
- Verification:
  - Tests around the extracted active-note logic if introduced.
  - Manual playback with a MIDI file containing same note across channels.
  - Web and console smoke tests.
- Release risk: medium.
- Rollback plan:
  - Revert active note representation changes.
- Needs user confirmation: no.

### Task E: Make Web Notifications Observable

- Previous task: Task 6.
- Priority: P2.
- Related issues: R5.
- Goal: Stop silently ignoring web playback startup and SignalR send failures.
- Change scope:
  - `Program.RunWebAsync`
  - `WebDisplayService`
  - `WebNoteProcessorService`
  - Possibly display/note interfaces if async sends are adopted.
- Not included:
  - Persistent event storage.
  - A large event bus architecture.
- Expected result:
  - Playback startup failures are logged or displayed.
  - SignalR send failures are observable.
  - Playback continues when visualizer notification failure is non-fatal.
- Verification:
  - Build.
  - Web mode with and without browser clients.
  - A controlled send-failure test or temporary local failure path if practical.
- Release risk: medium.
- Rollback plan:
  - Revert async/observability changes.
- Needs user confirmation: no for minimal logging; yes if broad interface changes are proposed.

### Task F: Resolve Frontend And Dependency Loose Ends

- Previous task: Task 12.
- Priority: P3.
- Related issues: R13, R14, R16.
- Goal: Remove confirmed unused assets/dependencies and document remaining version choices.
- Change scope:
  - `src/Edi.MIDIPlayer/wwwroot/index.html`
  - `src/Edi.MIDIPlayer/wwwroot/app.js`
  - `src/wwwroot/app.js`
  - `src/Edi.MIDIPlayer/Edi.MIDIPlayer.csproj`
  - README/AGENTS.
- Not included:
  - Full visual redesign.
  - Dependency upgrades without a confirmed reason.
- Expected result:
  - `waterfallCanvas` removed.
  - `src/wwwroot/app.js` removed if still unused.
  - `Microsoft.AspNetCore.SignalR.Client` removed if still unused by source/build.
  - SignalR browser JS version policy documented.
- Verification:
  - Repository search before deleting/removing.
  - Build.
  - Web visualizer smoke test and `/midihub` connection.
- Release risk: low.
- Rollback plan:
  - Revert individual asset/dependency changes.
- Needs user confirmation: no, as long as unused status is reconfirmed by search.

### Task G: Clean Up Shared Display Helpers And Interfaces

- Previous task: Task 11.
- Priority: P3.
- Related issues: R11, R12.
- Goal: Reduce formatting duplication and clarify display interfaces.
- Change scope:
  - Shared note/controller formatting helper.
  - Possible split of `IConsoleDisplay` into status display and console rendering helpers.
- Not included:
  - Display redesign.
  - Broad framework changes.
- Expected result:
  - Console and web display paths share note/controller naming.
  - Web display no longer needs console-specific members.
- Verification:
  - Unit tests for formatting helpers.
  - Web and console smoke tests.
- Release risk: low to medium.
- Rollback plan:
  - Revert helper/interface extraction.
- Needs user confirmation: no.

### Task H: Revisit Tempo Conversion Performance Only If Needed

- Previous issue: R15.
- Priority: P3/deferred.
- Related issues: R15.
- Goal: Avoid unnecessary complexity unless real MIDI files show timing conversion as a bottleneck.
- Change scope:
  - `TempoManagerService`
  - tests for tempo conversion.
- Expected result:
  - If needed, use cumulative tempo segments or a cursor-based conversion.
- Verification:
  - Profiling or benchmark using representative large MIDI files.
- Release risk: medium if done without strong evidence.
- Needs user confirmation: yes, only after evidence exists.

## Recommended Execution Order

1. Task A: Add focused characterization tests.
2. Task B: Remove browser log HTML injection risk.
3. Task C: Make console exit pause opt-in.
4. Task D: Correct active note tracking semantics.
5. Task E: Make web notifications observable.
6. Task F: Resolve frontend and dependency loose ends.
7. Task G: Clean up shared display helpers and interfaces.
8. Task H: Revisit tempo conversion performance only if evidence appears.

## Next Recommended Task

Start with **Task A: Add Focused Characterization Tests**.

Reasoning:

- Completed Tasks 1, 3, 4, and 9 changed important behavior without automated tests.
- `AppOptions` is now isolated, so CLI parsing is cheap to test.
- Download limits and URL validation can be protected without hardware MIDI.
- Tests will reduce risk for the next behavior changes: console pause, active note tracking, and web notification handling.

## Temporarily Not Recommended

- Cross-platform MIDI output:
  - The project explicitly exits on non-Windows platforms; changing this would require product and dependency decisions beyond a maintenance pass.
- Replacing the current static web visualizer with React/Vue/Svelte:
  - The current UI is small and does not justify the build and dependency cost.
- Adding a broad plugin/device architecture:
  - Device selection may be useful later, but there is no current requirement.
- Aggressive tempo conversion optimization:
  - Defer until profiling or real files show it matters.
- Broad remote URL allowlists:
  - Current `.mid` / `.midi` plus size restrictions are enough for the current local-tool threat model unless requirements change.

## Open Questions

- No questions currently block the remaining plan.
- Before deleting `src/wwwroot/app.js`, confirm it is still unused with repository search.
- Before removing `Microsoft.AspNetCore.SignalR.Client`, confirm it is still unused with repository search and build/test after the change.

## Execution Notes For Future Work

- After every behavior, tooling, architecture, or workflow change, update `README.md`, `AGENTS.md`, and relevant files under `docs/`.
- Keep changes small and independently buildable/testable.
- Do not combine refactoring, dependency changes, security fixes, and feature behavior changes in one task.
- After each task, start the player for local testing with:

```powershell
dotnet run --project "Edi.MIDIPlayer" -- "D:\OneDrive\Code for Fun\dotnet-midi-player\MIDI\红豆.mid"
```

- For playback changes, validate local MIDI playback, remote download playback, web visualizer startup, console display mode, and no-device behavior when feasible.
- For web UI changes, verify `/midihub` connects and the browser still handles `ReceiveNoteOn`, `ReceiveNoteOff`, `ReceiveControlChange`, and `ReceiveMessage`.
