# PASS - CX2 review of spec 034 sweep resilience

## Branch state

| Item | Value |
| --- | --- |
| Review branch | `feature/cx2-integrate-034` |
| Clean reviewed branch SHA before review doc | `f0ee63940d79654bc4009064c78a2d20a1ffa2fe` |
| Reviewed upstream SHA | `origin/feature/cc-sweep-resilience` = `f0ee63940d79654bc4009064c78a2d20a1ffa2fe` |
| dotnet version | `8.0.422` |

## Gate table

| Gate | Result | Real counts / evidence |
| --- | --- | --- |
| `PATH="$HOME/.dotnet:$PATH" dotnet build -c Release` | PASS | 0 warnings, 0 errors |
| `PATH="$HOME/.dotnet:$PATH" dotnet format --verify-no-changes` | PASS | clean, exit 0 |
| `PATH="$HOME/.dotnet:$PATH" dotnet test` | PASS | 94 passed, 0 failed, 0 skipped (`AmetekWatch.Tests` 58, Storage 4, Anthropic 30, Web 2) |
| Can-fail confirmation | PASS | Temporarily inverted `RetryPolicy_TransientThenSuccess_ReturnsResult` call-count assertion from expected 3 to expected 2; `dotnet test` failed with expected 2, actual 3 at `tests/AmetekWatch.Tests/RetryPolicyTests.cs:43`; reverted and final `dotnet test` returned 94 passed, 0 failed, 0 skipped. |

## Correctness checks

| Check | Status | Evidence |
| --- | --- | --- |
| `RetryPolicy` retries only when `shouldRetry(ex)` accepts the exception. | OK | The exception filter requires both `attempt < _maxAttempts` and `_shouldRetry(ex)` before catching for retry: `src/AmetekWatch.Core/Resilience/RetryPolicy.cs:54`. The non-retryable test configures `shouldRetry` for `TransientException`, throws `FatalException`, and asserts one call: `tests/AmetekWatch.Tests/RetryPolicyTests.cs:72`, `tests/AmetekWatch.Tests/RetryPolicyTests.cs:78`, `tests/AmetekWatch.Tests/RetryPolicyTests.cs:85`. |
| `RetryPolicy` backs off between retry attempts. | OK | Backoff is computed as `_baseDelay.Ticks * (1L << (attempt - 1))` and passed to the injected delay: `src/AmetekWatch.Core/Resilience/RetryPolicy.cs:58`, `src/AmetekWatch.Core/Resilience/RetryPolicy.cs:59`. |
| `RetryPolicy` rethrows after `maxAttempts`. | OK | Final-attempt exceptions are not caught because the filter requires `attempt < _maxAttempts`: `src/AmetekWatch.Core/Resilience/RetryPolicy.cs:54`. The test uses `maxAttempts: 3`, asserts `TransientException`, and verifies exactly 3 calls: `tests/AmetekWatch.Tests/RetryPolicyTests.cs:51`, `tests/AmetekWatch.Tests/RetryPolicyTests.cs:57`, `tests/AmetekWatch.Tests/RetryPolicyTests.cs:64`. |
| Delay is injected; tests pass a no-op, so no real waiting in the test path. | OK | Constructor accepts `Func<TimeSpan, CancellationToken, Task>? delay` and defaults to `Task.Delay` only when null: `src/AmetekWatch.Core/Resilience/RetryPolicy.cs:26`, `src/AmetekWatch.Core/Resilience/RetryPolicy.cs:41`. Tests define `NoDelay` as `Task.CompletedTask` and pass it into policy construction: `tests/AmetekWatch.Tests/RetryPolicyTests.cs:12`, `tests/AmetekWatch.Tests/RetryPolicyTests.cs:25`, `tests/AmetekWatch.Tests/RetryPolicyTests.cs:51`, `tests/AmetekWatch.Tests/RetryPolicyTests.cs:72`. |
| `NoRetryPolicy` is single-attempt passthrough. | OK | `NoRetryPolicy.ExecuteAsync` returns `op(ct)` directly with no loop or catch: `src/AmetekWatch.Core/Resilience/NoRetryPolicy.cs:10`, `src/AmetekWatch.Core/Resilience/NoRetryPolicy.cs:13`. Test verifies success once and failure once: `tests/AmetekWatch.Tests/RetryPolicyTests.cs:95`, `tests/AmetekWatch.Tests/RetryPolicyTests.cs:101`, `tests/AmetekWatch.Tests/RetryPolicyTests.cs:105`, `tests/AmetekWatch.Tests/RetryPolicyTests.cs:111`. |
| `SweepRunner` is backward-compatible. | OK | New ctor parameters are optional and default to `NoRetryPolicy` plus no-op callback: `src/AmetekWatch.Core/Pipeline/SweepRunner.cs:28`, `src/AmetekWatch.Core/Pipeline/SweepRunner.cs:32`, `src/AmetekWatch.Core/Pipeline/SweepRunner.cs:38`, `src/AmetekWatch.Core/Pipeline/SweepRunner.cs:39`. Existing 3-arg construction remains in App and tests: `src/AmetekWatch.App/SweepHost.cs:52`, `tests/AmetekWatch.Tests/SweepRunnerTests.cs:28`; build and existing tests pass unchanged. |
| Per-finding triage isolation skips only the failing finding. | OK | Each finding's triage is inside try/catch; on exception it invokes `_onTriageError` and continues before persistence: `src/AmetekWatch.Core/Pipeline/SweepRunner.cs:72`, `src/AmetekWatch.Core/Pipeline/SweepRunner.cs:81`, `src/AmetekWatch.Core/Pipeline/SweepRunner.cs:83`, `src/AmetekWatch.Core/Pipeline/SweepRunner.cs:84`, with persistence only after a verdict: `src/AmetekWatch.Core/Pipeline/SweepRunner.cs:87`, `src/AmetekWatch.Core/Pipeline/SweepRunner.cs:88`. The test throws for `UrlB`, asserts `UrlB` is not persisted or digested, asserts A/C persisted and digested, and asserts callback once: `tests/AmetekWatch.Tests/SweepRunnerResilienceTests.cs:38`, `tests/AmetekWatch.Tests/SweepRunnerResilienceTests.cs:47`, `tests/AmetekWatch.Tests/SweepRunnerResilienceTests.cs:52`, `tests/AmetekWatch.Tests/SweepRunnerResilienceTests.cs:55`, `tests/AmetekWatch.Tests/SweepRunnerResilienceTests.cs:58`. |
| Searcher call is wrapped in retry and propagates on final failure. | OK | `RunAsync` calls `_retryPolicy.ExecuteAsync(token => _searcher.SweepAsync(query, token), ct)` before any dedupe/triage: `src/AmetekWatch.Core/Pipeline/SweepRunner.cs:52`, `src/AmetekWatch.Core/Pipeline/SweepRunner.cs:54`, `src/AmetekWatch.Core/Pipeline/SweepRunner.cs:55`. Since `RetryPolicy` does not catch final failures and `NoRetryPolicy` directly returns `op(ct)`, final searcher failure propagates: `src/AmetekWatch.Core/Resilience/RetryPolicy.cs:54`, `src/AmetekWatch.Core/Resilience/NoRetryPolicy.cs:13`. |
| Core-only production scope; App/Anthropic/.sln untouched. | OK | Production resilience code is under `src/AmetekWatch.Core/Resilience/` and `src/AmetekWatch.Core/Pipeline/SweepRunner.cs`. `git diff --name-only origin/main...HEAD -- src/AmetekWatch.App src/AmetekWatch.Anthropic AmetekWatch.sln` returned no paths. The branch also includes expected tests/docs plus `CLAUDE.md` release note, not App/Anthropic/solution changes. |

## HOLD blockers

None.

VERDICT: PASS
