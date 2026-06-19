# AI Review And Improvement Plan

## Baseline Date

- 2026-06-19

## Scope

This file is the current improvement baseline after Tasks 1, 2, 3, 4, 9, A, B, C, D, E, F, and G were completed. It replaces the original long-form review plan as the active planning document.

Reviewed scope remains:

- `README.md`
- `AGENTS.md`
- `.github/workflows/dotnet.yml`
- `src/Edi.MIDIPlayer.slnx`
- `src/Edi.MIDIPlayer/Edi.MIDIPlayer.csproj`
- `src/Edi.MIDIPlayer/Program.cs`
- `src/Edi.MIDIPlayer/AppOptions.cs`
- `src/Edi.MIDIPlayer/Properties/AssemblyInfo.cs`
- `src/Edi.MIDIPlayer.Tests/`
- `src/Edi.MIDIPlayer/Hubs/`
- `src/Edi.MIDIPlayer/Interfaces/`
- `src/Edi.MIDIPlayer/Models/`
- `src/Edi.MIDIPlayer/Services/`
- `src/Edi.MIDIPlayer/wwwroot/`
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
| Task A: Add focused characterization tests | Completed | Added `Edi.MIDIPlayer.Tests` with xUnit v3 + Moq coverage for CLI parsing, remote download limits/timeouts, and tempo conversion. |
| Task B: Remove browser log HTML injection risk | Completed | Browser event log entries now use DOM nodes with `textContent` instead of `innerHTML`. |
| Task C: Make console exit pause opt-in | Completed | Console mode no longer waits for a key by default; `--pause-on-exit` preserves the old pause behavior. |
| Task D: Correct active note tracking semantics | Completed | Active notes are now tracked by channel plus note number with reference counts in both playback and browser state. |
| Task E: Make web notifications observable | Completed | Web startup background tasks and SignalR sends now log failures through `ILogger`; send observation is covered by tests. |
| Task F: Resolve frontend and dependency loose ends | Completed | Removed unused `waterfallCanvas`, empty `src/wwwroot/app.js`, and unused .NET SignalR client package; documented browser SignalR JS policy. |
| Task G: Clean up shared display helpers and interfaces | Completed | Added shared MIDI display formatting and split `IDisplayService` from console-only display helpers. |

Detailed execution records:

- `docs/task-harden-remote-download.md`
- `docs/task-align-web-urls.md`
- `docs/task-extract-program-responsibilities.md`
- `docs/task-add-characterization-tests.md`
- `docs/task-remove-browser-log-html-injection.md`
- `docs/task-console-exit-pause-opt-in.md`
- `docs/task-correct-active-note-tracking.md`
- `docs/task-make-web-notifications-observable.md`
- `docs/task-resolve-frontend-dependency-loose-ends.md`
- `docs/task-clean-up-display-helpers-interfaces.md`

## Current Overall Conclusion

- Overall risk level: medium-low.
- The most valuable next step is to leave tempo conversion optimization deferred until there is evidence from profiling or representative large MIDI files.
- The highest remaining product/code risks are:
  - No remaining P0/P1/P2 issues are listed in the current baseline.
  - Remaining P3 items are evidence-dependent or configuration cleanup.
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
| R15 | P3 | Performance | `TempoManagerService.cs`, `MidiPlayerService.cs` | Tick-to-time conversion scans the tempo map from the beginning for each event. | Large MIDI files with many tempo changes could spend unnecessary CPU. | `TicksToTimeSpan` loops from index 0 per event. | Defer until profiling or real-world files show a problem. |
| R16 | P3 | Readability/Configuration | Multiple files | Some operational values remain hard-coded. | Behavior changes still require code edits. | Startup delays, MIDI device ID, download timeout, browser log limit, and similar values are scattered. | Centralize only user-visible or likely-to-change values; avoid over-configuring internals. |

## Remaining Improvement Plan

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

1. Task H: Revisit tempo conversion performance only if evidence appears.

## Next Recommended Task

Do not start Task H yet unless representative MIDI files or profiling show tempo conversion is a real bottleneck.

Reasoning:

- Task A now protects CLI parsing, download limits/timeouts, and tempo conversion with 25 passing tests.
- Task B removed the browser log `innerHTML` path.
- Task C made console exit pause opt-in and added parser coverage for the new flag.
- Task D fixed channel-aware active note state in both server and browser paths.
- Task E added logging for web background tasks and SignalR send failures.
- Task F removed confirmed unused frontend/dependency loose ends and documented the remaining browser SignalR JavaScript policy.
- Task G removed the main display helper duplication and clarified display interfaces.
- Task H is evidence-dependent and should stay deferred until there is a measurable performance reason.

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
