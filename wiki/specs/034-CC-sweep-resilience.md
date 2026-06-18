# Spec 034-CC — Sweep resilience: per-finding triage isolation + retry helper

## Status
- Doc type: implementation (production hardening — survive transient API errors + bad findings)
- Executes: **CC**; pushes `feature/cc-sweep-resilience`; **CX2** integrates (cross-model); CM lands on PASS. No self-merge.
- Number 034 verified free (search `wiki/specs/`; this is highest + 1).
- Paired prompt: prompt-spec034-CC-sweep-resilience.md
- Final on-disk: `src/AmetekWatch.Core/{Resilience/,Pipeline/SweepRunner.cs}` + a test in `tests/AmetekWatch.Tests/`.

## Background
The real pipeline will hit **transient errors** (rate limits, `529 overloaded`, network blips) and occasional
**bad findings** (one item the decider can't parse). Today a single failure crashes the whole sweep — fatal
for a periodic daemon. This adds: (a) a generic **retry helper**, and (b) **per-finding triage isolation** in
`SweepRunner` so one bad finding can't abort the run. Pure/offline-testable; backward-compatible.

## Decisions made
1. **`Core/Resilience/`** (no new project/NuGet):
   - `IRetryPolicy` — `Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> op, CancellationToken ct)`.
   - `RetryPolicy : IRetryPolicy` — exponential backoff: ctor takes `maxAttempts`, a base delay, a
     `Func<Exception,bool> shouldRetry` predicate, and an **injected async delay**
     (`Func<TimeSpan,CancellationToken,Task>`, default `Task.Delay`) **so tests pass a no-op delay** (no real
     waiting). Retries only when `shouldRetry(ex)`; rethrows the last exception after `maxAttempts`.
   - `NoRetryPolicy : IRetryPolicy` — single attempt passthrough (the default).
2. **`SweepRunner` (backward-compatible):** add **optional** ctor params with safe defaults so existing
   3-arg construction keeps working: `IRetryPolicy? retryPolicy = null` (→ `NoRetryPolicy`),
   `Action<Finding, Exception>? onTriageError = null` (→ no-op). Behaviour:
   - Wrap the **searcher** call in `retryPolicy.ExecuteAsync` (transient retry). If it still fails, propagate
     (a sweep can't proceed without results) — that's the caller's concern.
   - **Per-finding triage isolation:** run each `triage.JudgeAsync(finding)` (optionally via the retry policy)
     inside a try/catch; on exception, call `onTriageError(finding, ex)` and **skip that finding** (don't
     persist it, don't abort) — continue with the rest. Persist + digest the successes only.
3. **No App/Anthropic changes here.** Wiring the retry policy into the real pipeline (with an
   Anthropic-transient `shouldRetry` predicate) and logging `onTriageError` is a later small App spec.
4. **Tests** (`tests/AmetekWatch.Tests/`): a `SweepRunner` with a decider that **throws on one specific
   finding** → that finding is **absent** from persisted + digest, the others persisted, `onTriageError`
   invoked once, the sweep returns normally; `RetryPolicy` retries a transient-then-success op (succeeds) and
   gives up after `maxAttempts` (rethrows) using a **no-op delay**; a non-retryable exception is **not**
   retried. Confirm existing `SweepRunner`/end-to-end tests still pass (default = no retry, no-op error).
   Hand-computed; confirm a test can fail then revert.

## Out of scope
- The Anthropic-transient predicate + wiring retry/logging into the App (later spec). Circuit breakers. The live API.

## Definition of done
- [ ] `IRetryPolicy`/`RetryPolicy`/`NoRetryPolicy`; `SweepRunner` per-finding isolation + optional retry (backward-compatible).
- [ ] Tests (isolation + retry + non-retryable); existing tests still green; can-fail confirmed.
- [ ] Gate green (build/format/test, each separately, real counts). Branch pushed; tip SHA reported.

## Deliverable / report-back
See the prompt file.
