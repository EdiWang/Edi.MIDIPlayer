# Clean Up Shared Display Helpers And Interfaces

## Original Goal

Execute Task G from `docs/ai-review-plan.md`: reduce duplicated MIDI display formatting and clarify display interfaces without redesigning the visualizers.

## Background

- `NoteProcessorService` and `WebNoteProcessorService` both contained note-name and controller-name helper logic.
- `IConsoleDisplay` was used as the general display abstraction even in web mode.
- `WebDisplayService` had to implement console-only members such as `GetConsoleLock` and `CreateVelocityBar`.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
|---|---|---|---|---|
| 1 | Confirm formatting duplication and interface coupling | None | Code search and file reads | Done |
| 2 | Add shared MIDI display formatter with tests | Task 1 | `dotnet test` | Done |
| 3 | Split shared display status interface from console rendering helpers | Task 2 | `dotnet test`; `dotnet build` | Done |
| 4 | Update console/web processors to use shared formatter | Tasks 2-3 | `dotnet test`; `dotnet build` | Done |
| 5 | Update README/AGENTS/baseline docs | Tasks 3-4 | Documentation review | Done |
| 6 | Restart player test instance | Tasks 2-5 | Local player launch | Done |

## Execution Order

1. Add `MidiDisplayFormatter` and focused tests for note names, note colors, and controller names.
2. Introduce `IDisplayService` for shared display operations and keep `IConsoleDisplay` for console-only helpers.
3. Update service constructors and DI registrations.
4. Update docs and restart the local player with `D:\OneDrive\Code for Fun\dotnet-midi-player\MIDI\红豆.mid`.

## Current Progress

- Status: Completed
- Current step: Done.
- Last updated: 2026-06-19

## Verification Log

| Date/Time | Verification | Result | Notes |
|---|---|---|---|
| 2026-06-19 | Code review | Done | Found duplicated note/controller formatting and web implementation of console-only display members. |
| 2026-06-19 | `dotnet test --configuration Release` | Passed | 45 tests passed, including MIDI display formatter coverage. |
| 2026-06-19 | `dotnet build --configuration Release` | Passed | 0 warnings, 0 errors. |
| 2026-06-19 | Console smoke test | Passed | Console mode with a missing MIDI path returned the expected validation error. |
| 2026-06-19 | Web smoke test | Passed | Temporary web visualizer loaded; SignalR JS available, `/midihub` connected, 88 piano keys rendered. |
| 2026-06-19 | Local player restart | Started | Restarted with `D:\OneDrive\Code for Fun\dotnet-midi-player\MIDI\红豆.mid`; process ID 16676. |

## Issues and Resolutions

| Issue | Trigger | Root Cause | Resolution | Verification |
|---|---|---|---|---|
| `TempoManagerService.cs` was not UTF-8 clean for `apply_patch` | Attempted identifier replacement in `TempoManagerService.cs` | Existing file contains a non-UTF-8 byte sequence in the tempo message text. | Used a byte-preserving Latin-1 replacement for ASCII identifiers only, leaving other content unchanged. | `dotnet test --configuration Release` and `dotnet build --configuration Release` passed. |

## Follow-ups

- Task H remains deferred until tempo conversion performance evidence exists.
