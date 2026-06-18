# Prompt — Spec 034-CC — Sweep resilience

You are **CC**. Execute Spec 034-CC (`wiki/specs/034-CC-sweep-resilience.md`). Read it first.

## Setup
- `git fetch --prune origin`; branch from origin: `git checkout -b feature/cc-sweep-resilience origin/main`.
- Prefix every dotnet call: `PATH="$HOME/.dotnet:$PATH" dotnet …`. Confirm `dotnet --version`.

## Steps
1. Add `Core/Resilience/`: `IRetryPolicy`, `RetryPolicy` (exponential backoff; ctor `maxAttempts`, base delay,
   `Func<Exception,bool> shouldRetry`, **injected** `Func<TimeSpan,CancellationToken,Task>` delay defaulting to
   `Task.Delay` — tests pass a no-op delay), and `NoRetryPolicy` (single-attempt passthrough, the default).
2. Update `SweepRunner` **backward-compatibly** — add optional ctor params `IRetryPolicy? retryPolicy = null`
   (→ `NoRetryPolicy`) and `Action<Finding,Exception>? onTriageError = null` (→ no-op). Wrap the searcher call
   in the retry policy; isolate each finding's triage in try/catch (optionally via retry) — on failure call
   `onTriageError` and **skip** that finding (don't persist/abort), continue with the rest. Persist + digest successes only.
3. Add tests to `tests/AmetekWatch.Tests/` per spec Decision 4 (decider-throws-on-one-finding isolation;
   RetryPolicy transient-then-success + give-up + non-retryable, **no-op delay**). Confirm existing
   `SweepRunner`/end-to-end tests still pass with the defaults. Confirm a test can fail then revert.
4. Do **not** change the App/Anthropic projects or the `.sln`. Keep it Core + tests.

## The gate
Each separately, real counts: `PATH="$HOME/.dotnet:$PATH" dotnet build -c Release` ; `… dotnet format --verify-no-changes` ; `… dotnet test`.

## Versioning ritual
Internal — no `CHANGELOG`/tag, `<Version>` stays `0.1.0`. Append a dated `CLAUDE.md` → `### Unreleased`
entry (below existing ones; don't edit them).

## Report-back format
Write `wiki/reports/report-spec034-CC-sweep-resilience.md` per `wiki/rituals/report-format.md` (all sections;
"None." where N/A), plus a gate table (real counts, before/after, can-fail, clean SHA, `dotnet --version`) and
confirmation that the existing `SweepRunner`/end-to-end tests still pass unchanged. Do **not** self-merge; push
`feature/cc-sweep-resilience` and end with the tip SHA + a one-line build/format/test summary. Never print or commit secrets.
