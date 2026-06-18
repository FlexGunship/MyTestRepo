# Spec 035-CX2 — Integrate CC's spec-034 sweep resilience

> Self-contained integration spec (no separate prompt). Same shape as [`009-CX2`](009-CX2-integrate-sqlite.md).

## Status
- Doc type: integration (cross-model gate + review). Executes: **CX2**; CX2 issues VERDICT; **CM lands on PASS**.
- Number 035 verified free (highest spec file = 034; 035 = 034 + 1).
- Reviewing: CC's `feature/cc-sweep-resilience` (origin tip `f0ee639`). Author CC (Claude) ≠ integrator CX2 (Codex).
- Final on-disk: `wiki/reviews/review-spec035-CX2-resilience.md`; on PASS, lands to `main`.

## What to do
1. `git fetch --prune origin`; `git checkout -b feature/cx2-integrate-034 origin/feature/cc-sweep-resilience`.
   Prefix every dotnet call: `PATH="$HOME/.dotnet:$PATH" dotnet …`.
2. Gate, each separately, real counts: `dotnet build -c Release`; `dotnet format --verify-no-changes`;
   `dotnet test`. Confirm a test can fail (invert one assertion, observe, revert).
3. Review `src/AmetekWatch.Core/Resilience/` + `SweepRunner.cs` + tests against spec 034, citing `file:line`:
   - `RetryPolicy` retries only when `shouldRetry(ex)`, backs off, rethrows after `maxAttempts`, and uses an
     **injected delay** (tests pass a no-op — confirm **no real waiting** in the test path); `NoRetryPolicy`
     is single-attempt.
   - `SweepRunner` is **backward-compatible** (new ctor params are optional with safe defaults; the existing
     3-arg construction in App/Web/tests still compiles and the prior tests pass **unchanged**).
   - **Per-finding triage isolation:** a finding whose triage throws is **skipped** (not persisted, not in the
     digest), `onTriageError` is invoked, and the sweep **completes** with the other findings — confirmed by a
     test. The searcher call is wrapped in the retry policy and propagates on final failure.
   - Core-only; App/Anthropic projects and `.sln` untouched.
4. Write `wiki/reviews/review-spec035-CX2-resilience.md`: PASS/HOLD headline; branch SHAs; gate table (real
   counts + can-fail + clean SHA + `dotnet --version`); correctness checks (`file:line`); HOLD blockers or
   "None."; final line exactly `VERDICT: PASS` or `VERDICT: HOLD`. Commit on `feature/cx2-integrate-034`, push.
   **Do not merge to main.**

## Definition of done
- [ ] Gate re-run (real counts, can-fail); review with `file:line`; backward-compat + isolation confirmed; ends `VERDICT`.
- [ ] Branch pushed; tip SHA + verdict reported.

## Deliverable / report-back
The review file; end your final message with the `feature/cx2-integrate-034` tip SHA + verdict.
