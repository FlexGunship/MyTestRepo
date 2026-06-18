# PASS — scheduled host integration review for spec 036.

## Branch State

- Review branch: `feature/cx2-integrate-036`
- Review branch clean source SHA before review artifact: `c8f792a4cc87e498ffe923092b6e4d5a0f266090`
- Reviewed source branch: `origin/feature/cc-scheduled-host`
- Reviewed source SHA: `c8f792a4cc87e498ffe923092b6e4d5a0f266090`
- Dotnet SDK: `8.0.422`
- Merge-base artifact check: `git diff --name-status origin/main..HEAD` shows deletions for spec 034/038 files, but `git diff --name-status origin/main...HEAD` shows no deletions. The 036 change set does not actually delete those files from the merge-base review diff.

## Gates

| Gate | Command | Result | Counts |
| --- | --- | --- | --- |
| Build | `PATH="$HOME/.dotnet:$PATH" dotnet build -c Release` | PASS | 9 projects built/restored; 0 warnings; 0 errors |
| Format | `PATH="$HOME/.dotnet:$PATH" dotnet format --verify-no-changes` | PASS | 0 changed files; exit 0 |
| Tests | `PATH="$HOME/.dotnet:$PATH" dotnet test` | PASS | 91 total: 91 passed, 0 failed, 0 skipped |
| Can-fail | Temporarily inverted `Assert.Equal(4, persisted.Count)` to `Assert.Equal(5, persisted.Count)` in `SweepBackgroundServiceTests`, then ran `PATH="$HOME/.dotnet:$PATH" dotnet test` | PASS | Expected failure observed: `Daemon_RunsAtLeastTwoSweeps_ThenStopsPromptly_OnCancel`, expected 5 actual 4 at `tests/AmetekWatch.Tests/SweepBackgroundServiceTests.cs:79`; reverted and re-ran 91/91 passing |

## Both-Mode Run Notes

- `RunOnce=true` default smoke: `PATH="$HOME/.dotnet:$PATH" dotnet run --project src/AmetekWatch.App` exited 0 after one sweep. Output confirmed fake deterministic pipeline, SQLite store `ametek-watch.db`, `FileDigestNotifier`, 4 persisted findings, and 3 worth-reporting digest items. The generated local DB/digest files were removed after the smoke.
- `RunOnce=false` daemon mode is intentionally long-lived. I did not leave a daemon process running; correctness is covered by the bounded cancellation test cited below.

## Correctness Checks

- OK — `Program` preserves one-shot default behavior: `RunOnce=true` enters `RunOnceAsync`, constructs `SweepHost`, calls `RunOnceAsync`, prints persisted/digest counts, and returns 0 at `src/AmetekWatch.App/Program.cs:25`, `src/AmetekWatch.App/Program.cs:33`, `src/AmetekWatch.App/Program.cs:35`, `src/AmetekWatch.App/Program.cs:37`, and `src/AmetekWatch.App/Program.cs:64`.
- OK — `RunOnce=false` switches to Generic Host daemon mode: `Program` calls `RunDaemon`, registers the composed `SweepHost`, registers `SweepBackgroundService`, and runs the host at `src/AmetekWatch.App/Program.cs:30`, `src/AmetekWatch.App/Program.cs:68`, `src/AmetekWatch.App/Program.cs:76`, `src/AmetekWatch.App/Program.cs:83`, and `src/AmetekWatch.App/Program.cs:92`.
- OK — `SweepBackgroundService` runs `SweepHost.RunAsync(stoppingToken)` directly and handles cancellation as graceful shutdown at `src/AmetekWatch.App/SweepBackgroundService.cs:34`, `src/AmetekWatch.App/SweepBackgroundService.cs:51`, and `src/AmetekWatch.App/SweepBackgroundService.cs:53`.
- OK — prompt stop is tested with at least two sweeps on a zero-minute interval, then `StopAsync` is bounded by a 10-second timeout at `tests/AmetekWatch.Tests/SweepBackgroundServiceTests.cs:53`, `tests/AmetekWatch.Tests/SweepBackgroundServiceTests.cs:57`, `tests/AmetekWatch.Tests/SweepBackgroundServiceTests.cs:65`, `tests/AmetekWatch.Tests/SweepBackgroundServiceTests.cs:70`, and `tests/AmetekWatch.Tests/SweepBackgroundServiceTests.cs:76`.
- OK — `SweepComposer` preserves the 028 pipeline-tier selection: fake when `UseRealApi=false`, real only when `ANTHROPIC_API_KEY` is present, otherwise warning plus fake fallback at `src/AmetekWatch.App/SweepComposer.cs:61`, `src/AmetekWatch.App/SweepComposer.cs:83`, `src/AmetekWatch.App/SweepComposer.cs:89`, `src/AmetekWatch.App/SweepComposer.cs:92`, and `src/AmetekWatch.App/SweepComposer.cs:99`.
- OK — `SweepComposer` preserves SQLite store composition and tolerant Notify bind: DB path resolves from `Storage:DbPath`, store is `SqliteFindingStore`, and partial email bind falls back to null rather than crashing at `src/AmetekWatch.App/SweepComposer.cs:49`, `src/AmetekWatch.App/SweepComposer.cs:54`, `src/AmetekWatch.App/SweepComposer.cs:58`, `src/AmetekWatch.App/SweepComposer.cs:70`, and `src/AmetekWatch.App/SweepComposer.cs:104`.
- OK — digest sink selection remains File/Email/None with null fallback and warnings for invalid/incomplete sink config at `src/AmetekWatch.App/DigestNotifierFactory.cs:44`, `src/AmetekWatch.App/DigestNotifierFactory.cs:49`, `src/AmetekWatch.App/DigestNotifierFactory.cs:51`, `src/AmetekWatch.App/DigestNotifierFactory.cs:66`, `src/AmetekWatch.App/DigestNotifierFactory.cs:68`, and `src/AmetekWatch.App/DigestNotifierFactory.cs:78`.
- OK — no live calls in selection tests: real pipeline type selection uses an `UnusedMessagesClient` that throws if invoked, and the test asserts only runtime types at `tests/AmetekWatch.Tests/PipelineToggleAndDigestTests.cs:35`, `tests/AmetekWatch.Tests/PipelineToggleAndDigestTests.cs:43`, `tests/AmetekWatch.Tests/PipelineToggleAndDigestTests.cs:45`, and `tests/AmetekWatch.Tests/PipelineToggleAndDigestTests.cs:50`.
- OK — digest persistence behavior from 028 remains covered: one fake sweep persists 4 unique findings and writes a real digest file with 3 items at `tests/AmetekWatch.Tests/PipelineToggleAndDigestTests.cs:65`, `tests/AmetekWatch.Tests/PipelineToggleAndDigestTests.cs:73`, `tests/AmetekWatch.Tests/PipelineToggleAndDigestTests.cs:77`, `tests/AmetekWatch.Tests/PipelineToggleAndDigestTests.cs:81`, and `tests/AmetekWatch.Tests/PipelineToggleAndDigestTests.cs:84`.
- OK — 032 sink construction tests cover File, Email without sending, None, invalid sink, and fallback warning paths at `tests/AmetekWatch.Tests/DigestSinkSelectionTests.cs:24`, `tests/AmetekWatch.Tests/DigestSinkSelectionTests.cs:46`, `tests/AmetekWatch.Tests/DigestSinkSelectionTests.cs:51`, `tests/AmetekWatch.Tests/DigestSinkSelectionTests.cs:91`, and `tests/AmetekWatch.Tests/DigestSinkSelectionTests.cs:101`.
- OK — `LoggingDigestNotifier` is a pure decorator: it null-checks/logs count and delegates the same digest list and cancellation token to the inner notifier without modifying content at `src/AmetekWatch.App/LoggingDigestNotifier.cs:24`, `src/AmetekWatch.App/LoggingDigestNotifier.cs:26`, `src/AmetekWatch.App/LoggingDigestNotifier.cs:27`, and `src/AmetekWatch.App/LoggingDigestNotifier.cs:28`.
- OK — `SweepHost` and `SweepRunner` seams are preserved: `SweepHost` still takes `ISearcher`, `ITriageDecider`, `IFindingStore`, and optional `IDigestNotifier`, constructs `SweepRunner` locally for one sweep, notifies with the returned digest, and the spec 036 three-dot diff does not modify `SweepRunner` at `src/AmetekWatch.App/SweepHost.cs:30`, `src/AmetekWatch.App/SweepHost.cs:41`, `src/AmetekWatch.App/SweepHost.cs:52`, and `src/AmetekWatch.App/SweepHost.cs:54`.
- OK — `.sln` untouched: both `git diff -- AmetekWatch.sln` and `git diff origin/main...HEAD -- AmetekWatch.sln` produced no diff.

## No-Secret Confirmation

- `git diff origin/main...HEAD -- src tests | rg -n "(?i)(sk-ant|sk-[A-Za-z0-9]|password\\s*[=:]|secret\\s*[=:]|api[_-]?key\\s*[=:]|bearer\\s+[A-Za-z0-9._-])"` returned no matches.
- The diff references the environment variable name `ANTHROPIC_API_KEY` for presence checks only; no key/token/secret value is printed or committed.

## HOLD Blockers

None.

VERDICT: PASS
