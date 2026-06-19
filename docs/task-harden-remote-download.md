# Harden Remote MIDI Download

## Original Goal

Execute Task 3 from `docs/ai-review-plan.md`: restrict remote MIDI downloads to `.mid` / `.midi`, enforce a download size below 10 MB, and make timeout and size errors clear.

## Background

- `MidiPlayerService` accepts local files and HTTP/HTTPS URLs.
- `FileDownloaderService` currently downloads the whole response into memory.
- User confirmed on 2026-06-19:
  - Remote MIDI downloads must be less than 10 MB.
  - Remote URLs should be restricted to `.mid` / `.midi`.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
|---|---|---|---|---|
| 1 | Add task record and confirm current code path | None | Read relevant files | Done |
| 2 | Enforce remote extension and download size | Task 1 | Build | Done |
| 3 | Improve timeout and validation error messages | Task 2 | Build | Done |
| 4 | Update README, AGENTS, and AI review plan | Task 2 | Documentation review | Done |
| 5 | Run build validation and restart player test instance | Tasks 2-4 | `dotnet build --configuration Release`; local player launch | Done |

## Execution Order

1. Record task context.
2. Implement the smallest code changes in downloader/playback service.
3. Update project documentation.
4. Build.
5. Restart local player with `D:\OneDrive\Code for Fun\dotnet-midi-player\MIDI\红豆.mid`.

## Current Progress

- Status: Completed
- Current step: Done.
- Last updated: 2026-06-19

## Verification Log

| Date/Time | Verification | Result | Notes |
|---|---|---|---|
| 2026-06-19 | Initial code review | Done | Found `ReadAsByteArrayAsync` and no remote extension validation. |
| 2026-06-19 | Code change | Done | Added `.mid` / `.midi` remote URL validation and streaming size enforcement below 10 MB. |
| 2026-06-19 | `dotnet build --configuration Release` from `src/` | Passed | 0 warnings, 0 errors. |

## Issues and Resolutions

| Issue | Trigger | Root Cause | Resolution | Verification |
|---|---|---|---|---|
| No tests yet | Task 3 needed validation but test project is planned for Task 10 | Repository currently has no dedicated test project | Use build validation now; add focused tests in Task 10 | Pending Task 10 |

## Follow-ups

- Add xUnit v3 and Moq tests in the planned test task.
- Consider MIME/content sniffing only if extension validation proves insufficient.
