# Extract Program Responsibilities

## Original Goal

Execute Task 9 from `docs/ai-review-plan.md`: make future host and CLI changes safer by moving stable command-line parsing responsibilities out of `Program.cs` without changing runtime behavior.

## Background

- `Program.cs` currently owns OS checks, web/console startup, DI registration, playback kickoff, browser launch, usage output, and command-line parsing.
- The planned first step is intentionally small: move `AppOptions` and `DisplayMode` parsing to a focused source file.
- No new command-line parser dependency should be introduced.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
|---|---|---|---|---|
| 1 | Record task context and inspect current `Program.cs` | None | Read files | Done |
| 2 | Move `AppOptions` and `DisplayMode` into a focused file | Task 1 | Build | Done |
| 3 | Update references and documentation | Task 2 | Documentation review | Done |
| 4 | Run build validation and restart player test instance | Tasks 2-3 | `dotnet build --configuration Release`; local player launch | Done |

## Execution Order

1. Record task context.
2. Create a focused command-line options source file.
3. Remove the nested parser from `Program.cs`.
4. Update docs that mention `Program.AppOptions`.
5. Build and restart the local test player with `D:\OneDrive\Code for Fun\dotnet-midi-player\MIDI\红豆.mid`.

## Current Progress

- Status: Completed
- Current step: Done.
- Last updated: 2026-06-19

## Verification Log

| Date/Time | Verification | Result | Notes |
|---|---|---|---|
| 2026-06-19 | Initial code review | Done | `AppOptions` and `DisplayMode` are nested in `Program.cs`. |
| 2026-06-19 | Code change | Done | Added `AppOptions.cs` and removed nested `AppOptions` / `DisplayMode` from `Program.cs`. |
| 2026-06-19 | `dotnet build --configuration Release` from `src/` | Passed | 0 warnings, 0 errors. |

## Issues and Resolutions

| Issue | Trigger | Root Cause | Resolution | Verification |
|---|---|---|---|---|
| No parser tests yet | Task 9 moved option parsing | Repository currently has no dedicated test project | Use build validation now; add parser characterization tests in Task 10 | Pending Task 10 |

## Follow-ups

- Add parser characterization tests in the planned xUnit v3 test task.
- Consider extracting DI registration separately only after behavior is protected by tests.
