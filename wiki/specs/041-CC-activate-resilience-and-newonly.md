# Spec 041-CC — Activate retry + new-only in the live pipeline

## Status
- Doc type: implementation (turn on the 034 retry + 038 new-only features in the real pipeline via config)
- Executes: **CC**; pushes `feature/cc-activate-resilience`; **CX** integrates (cross-model); CM lands on PASS. No self-merge.
- Number 041 verified free (search `wiki/specs/`; this is highest + 1).
- Paired prompt: prompt-spec041-CC-activate-resilience-and-newonly.md
- Final on-disk: `src/AmetekWatch.Anthropic/AnthropicTransient.cs` + `src/AmetekWatch.App/` (`SweepComposer` + options + appsettings) + tests.

## Background
034 added a `RetryPolicy` + per-finding isolation and 038 added an opt-in new-only digest — but both are
**off by default** and **not wired** into the running app (`SweepComposer` from 036 constructs `SweepRunner`
without them). This activates them by config so the real daemon actually retries transient API errors and
only reports genuinely-new findings.

## Decisions made
1. **`AmetekWatch.Anthropic/AnthropicTransient.cs`** — a pure `static bool IsTransient(Exception ex)` for the
   retry `shouldRetry` predicate. **Consult `/claude-api`** for the SDK's C# exception types; treat as transient:
   rate-limit (HTTP 429), overloaded (529), 5xx server errors, and network/timeout (`HttpRequestException`,
   `TaskCanceledException`/`TimeoutException`). Non-API/argument/parse errors → **not** transient. Document the
   predicate; keep it conservative (retry only clearly-transient cases). Unit-test the cases you **can**
   construct (e.g. `HttpRequestException` → true; `ArgumentException`/`FormatException` → false).
2. **Config:** `Pipeline:Retry:{ MaxAttempts (default 3), BaseDelayMs (default 500) }`; `Sweep:OnlyReportNew`
   (bool, **default false** so the CLI/tests stay deterministic). Bind to options records.
3. **`SweepComposer` (036):** when `UseRealApi==true`, build `new RetryPolicy(MaxAttempts, BaseDelay,
   AnthropicTransient.IsTransient)`; else `NoRetryPolicy`. Construct `SweepRunner` with that retry policy,
   `digestOnlyNew: Sweep.OnlyReportNew`, and an `onTriageError` that **logs** the skipped finding (via the
   composer's logger). Don't change the `SweepRunner`/`SweepHost` seams or the notifier impls.
4. **Tests** (`tests/AmetekWatch.Tests/` + `tests/AmetekWatch.Anthropic.Tests/`): `AnthropicTransient`
   predicate cases (constructible ones); a `SweepComposer` test that with `UseRealApi=false` resolves a
   `NoRetryPolicy` and honors `OnlyReportNew` (assert via behaviour or the constructed `SweepRunner`'s effect —
   keep it offline, no network/key). The existing `SweepComposer`/daemon/end-to-end tests must still pass.
   Hand-computed; confirm a test can fail then revert.

## Out of scope
- Live API/SMTP. Changing `SweepRunner`/`SweepHost` seams. Circuit breakers. Per-call retry on the
  *searcher* beyond what `SweepRunner` already wraps (034).

## Definition of done
- [ ] `AnthropicTransient.IsTransient`; config (`Pipeline:Retry`, `Sweep:OnlyReportNew`); `SweepComposer` passes
      retry + `digestOnlyNew` + `onTriageError` into `SweepRunner` (real → RetryPolicy, fake → NoRetry).
- [ ] `dotnet run` (default fakes, OnlyReportNew=false) still runs one sweep, persists, writes digest, exit 0.
- [ ] Tests (predicate + composer wiring); existing tests still green; can-fail confirmed.
- [ ] Gate green (build/format/test, each separately, real counts). Branch pushed; tip SHA reported.

## Deliverable / report-back
See the prompt file.
