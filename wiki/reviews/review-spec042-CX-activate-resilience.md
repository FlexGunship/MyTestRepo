# HOLD - spec 041 activation is functionally close, but violates the requested host seam constraint and misses required 4xx test coverage.

## Branch State

| Item | Value |
|---|---|
| Integration branch | `feature/cx-integrate-041` |
| Clean reviewed SHA before review artifact | `cbe71709903fd957c69cbbc3c0658395e1ac50ae` |
| Reviewed upstream | `origin/feature/cc-activate-resilience` |
| Reviewed upstream SHA | `cbe71709903fd957c69cbbc3c0658395e1ac50ae` |
| .NET SDK | `8.0.422` |

## Gates

| Gate | Result | Real Counts / Note |
|---|---:|---|
| `PATH="$HOME/.dotnet:$PATH" dotnet build -c Release` | PASS | 0 warnings, 0 errors |
| `PATH="$HOME/.dotnet:$PATH" dotnet format --verify-no-changes` | PASS | exit 0, no changes |
| `PATH="$HOME/.dotnet:$PATH" dotnet test` | PASS | 116 passed, 0 failed, 0 skipped |
| Can-fail proof | PASS | Temporarily inverted `AnthropicTransientTests.Null_IsNotTransient`; `dotnet test` failed with 1 failed / 115 passed observed (`Assert.True` expected true, actual false), then assertion was reverted and `dotnet test` reran green at 116 passed |
| Clean SHA after revert | PASS | `cbe71709903fd957c69cbbc3c0658395e1ac50ae` |

## Default Fake Run

`PATH="$HOME/.dotnet:$PATH" dotnet run --project src/AmetekWatch.App` exited 0 using `FAKE (deterministic; Pipeline:UseRealApi=false)`, persisted 4 findings to `ametek-watch.db`, produced a 3-item worth-reporting digest, and wrote the configured file digest at `ametek-watch-digest.md`.

## Correctness Checks

| Check | Status | Evidence |
|---|---|---|
| `AnthropicTransient.IsTransient` is conservative by default | ok | `null` returns false at `src/AmetekWatch.Anthropic/AnthropicTransient.cs:44`; unknown/non-API exceptions fall through to false at `src/AmetekWatch.Anthropic/AnthropicTransient.cs:80`. |
| 429 rate limit retries | ok | `AnthropicRateLimitException` returns true at `src/AmetekWatch.Anthropic/AnthropicTransient.cs:56`; test at `tests/AmetekWatch.Anthropic.Tests/AnthropicTransientTests.cs:19`. |
| 529 overloaded and other 5xx retry | ok | `Anthropic5xxException` and API status `>= 500` return true at `src/AmetekWatch.Anthropic/AnthropicTransient.cs:56` and `src/AmetekWatch.Anthropic/AnthropicTransient.cs:61`; tests at `tests/AmetekWatch.Anthropic.Tests/AnthropicTransientTests.cs:31`, `tests/AmetekWatch.Anthropic.Tests/AnthropicTransientTests.cs:43`, and `tests/AmetekWatch.Anthropic.Tests/AnthropicTransientTests.cs:56`. |
| Network/transport retries | ok | `AnthropicIOException`, `HttpRequestException`, `TaskCanceledException`, and `TimeoutException` return true at `src/AmetekWatch.Anthropic/AnthropicTransient.cs:68` and `src/AmetekWatch.Anthropic/AnthropicTransient.cs:75`; tests at `tests/AmetekWatch.Anthropic.Tests/AnthropicTransientTests.cs:92`, `tests/AmetekWatch.Anthropic.Tests/AnthropicTransientTests.cs:100`, `tests/AmetekWatch.Anthropic.Tests/AnthropicTransientTests.cs:106`, and `tests/AmetekWatch.Anthropic.Tests/AnthropicTransientTests.cs:113`. |
| 4xx client errors do not over-match | finding | Predicate logic itself is conservative: only 429 or `>=500` retry at `src/AmetekWatch.Anthropic/AnthropicTransient.cs:61`, so 400/401/403/404/422 are false by status. Tests cover 400 and 404 at `tests/AmetekWatch.Anthropic.Tests/AnthropicTransientTests.cs:67` and `tests/AmetekWatch.Anthropic.Tests/AnthropicTransientTests.cs:79`, but do not explicitly cover unauthorized 401 as requested. |
| Argument/parse errors do not retry | ok | Fallthrough false at `src/AmetekWatch.Anthropic/AnthropicTransient.cs:80`; tests at `tests/AmetekWatch.Anthropic.Tests/AnthropicTransientTests.cs:119`, `tests/AmetekWatch.Anthropic.Tests/AnthropicTransientTests.cs:125`, and `tests/AmetekWatch.Anthropic.Tests/AnthropicTransientTests.cs:132`. |
| Real API selects `RetryPolicy(MaxAttempts, BaseDelay, AnthropicTransient.IsTransient)` | ok | `pipeline.UseRealApi ? new RetryPolicy(pipeline.Retry.MaxAttempts, TimeSpan.FromMilliseconds(pipeline.Retry.BaseDelayMs), AnthropicTransient.IsTransient)` at `src/AmetekWatch.App/SweepComposer.cs:76`. |
| Fake/fallback selects `NoRetryPolicy` | ok | Fake branch returns `new NoRetryPolicy()` at `src/AmetekWatch.App/SweepComposer.cs:81`; test asserts fake composition at `tests/AmetekWatch.Tests/SweepComposerResilienceTests.cs:40`. |
| `SweepRunner` is constructed with retry policy, `Sweep.OnlyReportNew`, and logging triage error callback | ok | Runner construction passes `retryPolicy`, `onTriageError`, and `digestOnlyNew: options.OnlyReportNew` at `src/AmetekWatch.App/SweepComposer.cs:83`. |
| Config binds `Pipeline:Retry` and `Sweep:OnlyReportNew`, default false | ok | `PipelineOptions.Retry` defaults at `src/AmetekWatch.App/PipelineOptions.cs:27`; `RetryOptions` defaults at `src/AmetekWatch.App/PipelineOptions.cs:34`; `SweepOptions.OnlyReportNew` defaults false at `src/AmetekWatch.App/SweepOptions.cs:34`; shipped config at `src/AmetekWatch.App/appsettings.json:6` and `src/AmetekWatch.App/appsettings.json:13`. |
| No live call during composition/default fakes | ok | Composer docs state construction only at `src/AmetekWatch.App/SweepComposer.cs:34`; real client is only created when `UseRealApi` and `ANTHROPIC_API_KEY` are present at `src/AmetekWatch.App/SweepComposer.cs:117`; default config has `UseRealApi=false` at `src/AmetekWatch.App/appsettings.json:12`. |
| No `SweepRunner` / `SweepHost` seam change | finding | `SweepRunner` was not changed in this diff, but `SweepHost` was changed to accept a `SweepRunner? runner` constructor parameter and store it as `_runner` at `src/AmetekWatch.App/SweepHost.cs:25` and `src/AmetekWatch.App/SweepHost.cs:37`. `Program` now passes `c.Runner` into that new seam at `src/AmetekWatch.App/Program.cs:80`. |
| Existing composer/daemon/end-to-end tests pass | ok | Full `dotnet test` passed: 116 passed, 0 failed, 0 skipped. Composer resilience tests cover fake no-retry and new-only wiring at `tests/AmetekWatch.Tests/SweepComposerResilienceTests.cs:40`, `tests/AmetekWatch.Tests/SweepComposerResilienceTests.cs:62`, and `tests/AmetekWatch.Tests/SweepComposerResilienceTests.cs:86`. |

## No-Secret Confirmation

I grepped the diff for secret/token/API-key patterns. No hardcoded Anthropic key or token was found. The only Anthropic key string is the expected environment variable name `ANTHROPIC_API_KEY`.

## HOLD Blockers

1. `src/AmetekWatch.App/SweepHost.cs:37` - The spec review request explicitly required "no SweepRunner/SweepHost seam change", but the constructor now accepts `SweepRunner? runner` and stores it in a new `_runner` field at `src/AmetekWatch.App/SweepHost.cs:25`. `src/AmetekWatch.App/Program.cs:80` depends on that new seam by passing `c.Runner`.
2. `tests/AmetekWatch.Anthropic.Tests/AnthropicTransientTests.cs:67` - The tests cover 400 bad request and 404 generic 4xx, but the requested adversarial check specifically calls out bad-request/unauthorized coverage. There is no explicit 401 unauthorized test, so the no-retry guarantee for that named case is not locked by tests.

VERDICT: HOLD
