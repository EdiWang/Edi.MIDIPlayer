# Make Console Exit Pause Opt-In

## Original Goal

Execute Task C from `docs/ai-review-plan.md`: avoid blocking scripted/non-interactive console use by default, while preserving the old exit pause behind an explicit option.

## Background

- `Program.RunConsoleAsync` always clears the terminal before playback.
- `Program.RunConsoleAsync` always prints `Press any key to exit...` and calls `Console.ReadKey()` in `finally`.
- This can hang scripts and hide useful terminal output.
- The user confirmed that console exit pause should be opt-in.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
|---|---|---|---|---|
| 1 | Confirm current console parsing and exit behavior | None | Code search and file read | Done |
| 2 | Add `--pause-on-exit` parsing and tests | Task 1 | `dotnet test` | Done |
| 3 | Make console pause opt-in and guard terminal clearing | Task 2 | `dotnet test`; `dotnet build`; console smoke commands | Done |
| 4 | Update README, AGENTS, and baseline plan | Task 3 | Documentation review | Done |
| 5 | Restart player test instance | Tasks 2-4 | Local player launch | Done |

## Execution Order

1. Add `PauseOnExit` to `AppOptions`.
2. Parse `--pause-on-exit` without forwarding it to host args or MIDI args.
3. Use the flag in `RunConsoleAsync` and keep `Console.Clear()` only for interactive, non-redirected output.
4. Update docs and validation notes.
5. Restart the local player with `D:\OneDrive\Code for Fun\dotnet-midi-player\MIDI\红豆.mid`.

## Current Progress

- Status: Completed
- Current step: Done.
- Last updated: 2026-06-19

## Verification Log

| Date/Time | Verification | Result | Notes |
|---|---|---|---|
| 2026-06-19 | Code review | Done | `RunConsoleAsync` unconditionally called `Console.Clear()` and `Console.ReadKey()`. |
| 2026-06-19 | Code change | Done | Added `AppOptions.PauseOnExit`, parsed `--pause-on-exit`, and guarded `Console.ReadKey()` behind that flag. |
| 2026-06-19 | Code change | Done | Guarded `Console.Clear()` behind interactive, non-redirected output. |
| 2026-06-19 | `dotnet test --configuration Release` | Passed | 26 tests passed. |
| 2026-06-19 | `dotnet build --configuration Release` | Passed | 0 warnings, 0 errors. |
| 2026-06-19 | Console smoke without `--pause-on-exit` | Passed | Missing-file command returned without `Press any key to exit...`. |
| 2026-06-19 | Console smoke with `--pause-on-exit` under redirected input | Passed | Missing-file command returned without hanging because input was redirected. |
| 2026-06-19 | Local player restart | Started | Restarted with `D:\OneDrive\Code for Fun\dotnet-midi-player\MIDI\红豆.mid`; process ID 2340. |

## Issues and Resolutions

| Issue | Trigger | Root Cause | Resolution | Verification |
|---|---|---|---|---|
| Debug apphost locked during first smoke command | Initial `dotnet run` smoke command while previous player was still running | `dotnet run` defaulted to Debug and tried to rebuild an executable held by the running player process. | Stopped the old player processes and reran smoke commands with `--no-build --configuration Release --no-launch-profile`. | Both smoke commands returned successfully. |

## Follow-ups

- Task D remains the next planned behavior correction after this task.
