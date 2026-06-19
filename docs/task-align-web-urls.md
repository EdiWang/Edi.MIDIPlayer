# Align Web URL Binding And Browser Launch

## Original Goal

Execute Task 4 from `docs/ai-review-plan.md`: make `--urls` an officially supported option so the ASP.NET Core web visualizer binds to the configured URL and the browser opens the matching address.

## Background

- `Program.AppOptions` already recognizes `--urls` as a host option.
- Before this task, `Program.RunWebAsync` hard-coded `http://localhost:5000` for both browser launch and `app.RunAsync`.
- User confirmed on 2026-06-19 that `--urls` should be officially supported.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
|---|---|---|---|---|
| 1 | Record task context and inspect current parsing/startup flow | None | Read `Program.cs` and docs | Done |
| 2 | Capture `--urls` value in `AppOptions` | Task 1 | Build | Done |
| 3 | Use configured URLs for web binding and browser launch | Task 2 | Build | Done |
| 4 | Update README, AGENTS, and AI review plan | Task 3 | Documentation review | Done |
| 5 | Run build validation and restart player test instance | Tasks 2-4 | `dotnet build --configuration Release`; local player launch | Done |

## Execution Order

1. Record task context.
2. Update argument parsing to preserve `--urls` as a first-class web option.
3. Update web startup to bind/open the configured URL while preserving the default URL.
4. Update documentation.
5. Build and restart the local test player with `D:\OneDrive\Code for Fun\dotnet-midi-player\MIDI\红豆.mid`.

## Current Progress

- Status: Completed
- Current step: Done.
- Last updated: 2026-06-19

## Verification Log

| Date/Time | Verification | Result | Notes |
|---|---|---|---|
| 2026-06-19 | Initial code review | Done | `--urls` is recognized but `RunWebAsync` hard-codes `http://localhost:5000`. |
| 2026-06-19 | Code change | Done | `AppOptions` captures `--urls`; web mode uses host configuration for binding and opens the browser at the first configured URL. |
| 2026-06-19 | `dotnet build --configuration Release` from `src/` | Passed | 0 warnings, 0 errors. |

## Issues and Resolutions

| Issue | Trigger | Root Cause | Resolution | Verification |
|---|---|---|---|---|
| Command-line parsing not covered by tests yet | Task 4 changed option parsing | Repository currently has no dedicated test project | Use build validation now; add parser tests in Task 10 | Pending Task 10 |

## Follow-ups

- Add command-line parsing tests in the planned xUnit v3 test task.
