# Make Web Notifications Observable

## Original Goal

Execute Task E from `docs/ai-review-plan.md`: make web playback startup and SignalR notification failures observable without redesigning the event pipeline.

## Background

- `Program.RunWebAsync` starts browser launch and playback with unobserved background `Task.Run` calls.
- `WebDisplayService` and `WebNoteProcessorService` discard SignalR `SendAsync` tasks.
- Exceptions from those paths can be missed, which makes web-mode failures hard to diagnose.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
|---|---|---|---|---|
| 1 | Confirm fire-and-forget paths | None | Code search and file reads | Done |
| 2 | Add a small SignalR send observer with tests | Task 1 | `dotnet test` | Done |
| 3 | Use the observer in web display and note processors | Task 2 | `dotnet test`; `dotnet build` | Done |
| 4 | Wrap web-mode background startup tasks with logging | Task 3 | `dotnet test`; `dotnet build`; web smoke test | Done |
| 5 | Update README/AGENTS/baseline docs | Tasks 3-4 | Documentation review | Done |
| 6 | Restart player test instance | Tasks 2-5 | Local player launch | Done |

## Execution Order

1. Add a focused helper that awaits SignalR send tasks and logs failures.
2. Add tests for successful and failed send observation.
3. Inject loggers into web display/note services and observe all SignalR sends.
4. Wrap web-mode browser launch and playback startup tasks with a logging helper.
5. Update documentation and restart the local player with `D:\OneDrive\Code for Fun\dotnet-midi-player\MIDI\红豆.mid`.

## Current Progress

- Status: Completed
- Current step: Done.
- Last updated: 2026-06-19

## Verification Log

| Date/Time | Verification | Result | Notes |
|---|---|---|---|
| 2026-06-19 | Code review | Done | Found discarded `Task.Run` and SignalR `SendAsync` tasks in web paths. |
| 2026-06-19 | `dotnet test --configuration Release` | Failed, fixed | Release apphost was locked by the running player process. |
| 2026-06-19 | `dotnet test --configuration Release` | Passed | 32 tests passed, including SignalR send observer coverage. |
| 2026-06-19 | `dotnet build --configuration Release` | Passed | 0 warnings, 0 errors. |
| 2026-06-19 | Web smoke test | Passed | Temporary web visualizer loaded on `http://127.0.0.1:5057`; `/midihub` connection reached `Connected`. |
| 2026-06-19 | Local player restart | Started | Restarted with `D:\OneDrive\Code for Fun\dotnet-midi-player\MIDI\红豆.mid`; process ID 10412. |

## Issues and Resolutions

| Issue | Trigger | Root Cause | Resolution | Verification |
|---|---|---|---|---|
| Release apphost locked during first validation | First `dotnet test --configuration Release` while previous player was running | The previous local player was started from the Release output and held `Edi.MIDIPlayer.exe`. | Stopped the player process tree and reran validation. | `dotnet test --configuration Release` passed with 32 tests. |

## Follow-ups

- Task G should address display interface cleanup separately if still needed.
