# Add Focused Characterization Tests

## Original Goal

Execute Task A from `docs/ai-review-plan.md`: add focused xUnit v3 + Moq tests for behavior that can be verified without real MIDI hardware.

## Background

- The user confirmed xUnit v3 and Moq.
- Completed tasks changed command-line parsing, remote downloads, URL validation, packaging, and MIDI device checks.
- The repository currently has no dedicated test project, while CI runs `dotnet test`.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
|---|---|---|---|---|
| 1 | Confirm package versions and project structure | None | NuGet/package docs and local files | Done |
| 2 | Add xUnit v3 + Moq test project to solution | Task 1 | `dotnet test` | Done |
| 3 | Add focused tests for `AppOptions`, downloader, and tempo conversion | Task 2 | `dotnet test` | Done |
| 4 | Update README, AGENTS, and baseline plan | Task 3 | Documentation review | Done |
| 5 | Run build/test validation and restart player test instance | Tasks 2-4 | `dotnet test`; `dotnet build`; local player launch | Done |

## Execution Order

1. Add the test project and project reference.
2. Add internals visibility only if needed for focused tests.
3. Write small tests around parsing, download limits/timeouts, and tempo conversion.
4. Update documentation.
5. Run validation and restart the local player with `D:\OneDrive\Code for Fun\dotnet-midi-player\MIDI\红豆.mid`.

## Current Progress

- Status: Completed
- Current step: Done.
- Last updated: 2026-06-19

## Verification Log

| Date/Time | Verification | Result | Notes |
|---|---|---|---|
| 2026-06-19 | Package check | Done | `xunit.v3` 3.2.2 and Moq 4.20.72 selected. |
| 2026-06-19 | `dotnet test --configuration Release` | Failed, fixed | Initial run failed before `Microsoft.NET.Test.Sdk` was added. |
| 2026-06-19 | `dotnet test --configuration Release` | Failed, fixed | Test assembly built, but VSTest discovered no tests until `xunit.runner.visualstudio` was added. |
| 2026-06-19 | `dotnet test --configuration Release` | Passed | 25 tests passed. |
| 2026-06-19 | `dotnet build --configuration Release` | Passed | 0 warnings, 0 errors. |
| 2026-06-19 | Local player restart | Started | Restarted with `D:\OneDrive\Code for Fun\dotnet-midi-player\MIDI\红豆.mid`; process ID 23536. |

## Issues and Resolutions

| Issue | Trigger | Root Cause | Resolution | Verification |
|---|---|---|---|---|
| Missing testhost package | First `dotnet test` after creating the test project | The project referenced `xunit.v3` but did not yet reference `Microsoft.NET.Test.Sdk`. | Added `Microsoft.NET.Test.Sdk` 18.6.0. | A later `dotnet test` reached test discovery. |
| No tests discovered | `dotnet test` after adding `Microsoft.NET.Test.Sdk` | The VSTest adapter was not referenced. | Added `xunit.runner.visualstudio` 3.1.5. | `dotnet test --configuration Release` discovered and passed 25 tests. |

## Follow-ups

- Add active-note behavior tests when Task D introduces a testable seam.
