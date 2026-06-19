# Correct Active Note Tracking Semantics

## Original Goal

Execute Task D from `docs/ai-review-plan.md`: track active notes by MIDI channel plus note number so console and web active-note state reflects MIDI semantics more accurately.

## Background

- Server playback used `HashSet<int>` keyed only by note number.
- Browser UI used `Set` keyed only by note number.
- The same pitch on different channels collapsed into one active note.
- Overlapping note-on events for the same channel/note could be cleared too early by a single note-off.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
|---|---|---|---|---|
| 1 | Confirm current active-note data flow | None | Code search and file reads | Done |
| 2 | Add a small server-side active-note tracker with tests | Task 1 | `dotnet test` | Done |
| 3 | Update playback to use channel+note active counts | Task 2 | `dotnet test`; `dotnet build` | Done |
| 4 | Update browser active-note state to channel+note reference counts | Task 3 | Browser smoke test | Done |
| 5 | Update README/AGENTS/baseline docs | Tasks 3-4 | Documentation review | Done |
| 6 | Restart player test instance | Tasks 2-5 | Local player launch | Done |

## Execution Order

1. Add a focused internal `ActiveNoteTracker`.
2. Cover channel separation and duplicate note-on behavior with tests.
3. Use the tracker in `MidiPlayerService`.
4. Mirror the same key/count behavior in `wwwroot/app.js`.
5. Update documentation and restart the local player with `D:\OneDrive\Code for Fun\dotnet-midi-player\MIDI\红豆.mid`.

## Current Progress

- Status: Completed
- Current step: Done.
- Last updated: 2026-06-19

## Verification Log

| Date/Time | Verification | Result | Notes |
|---|---|---|---|
| 2026-06-19 | Code review | Done | Found note-only active state in `MidiPlayerService` and `wwwroot/app.js`. |
| 2026-06-19 | `dotnet test --configuration Release` | Passed | 30 tests passed, including active-note tracker coverage. |
| 2026-06-19 | `dotnet build --configuration Release` | Passed | 0 warnings, 0 errors. |
| 2026-06-19 | Browser smoke test | Passed | Two channels on note 60 counted as 2; one note-off left the key active; duplicate same-channel note-on stayed active until final note-off. |
| 2026-06-19 | Local player restart | Started | Restarted with `D:\OneDrive\Code for Fun\dotnet-midi-player\MIDI\红豆.mid`; process ID 8132. |

## Issues and Resolutions

| Issue | Trigger | Root Cause | Resolution | Verification |
|---|---|---|---|---|
| None | N/A | N/A | N/A | N/A |

## Follow-ups

- Task E should handle SignalR send observability separately.
