# Spec 042-CX — Integrate CC's spec-041 activate-resilience

> Self-contained integration spec (no separate prompt). Same shape as [`022-CX`](022-CX-integrate-anthropic-triage.md).

## Status
- Doc type: integration (cross-model gate + review). Executes: **CX**; CX issues VERDICT; **CM lands on PASS**.
- Number 042 verified free (highest spec file = 041; 042 = 041 + 1).
- Reviewing: CC's `feature/cc-activate-resilience` (origin tip `cbe7170`). Author CC (Claude) ≠ integrator CX (Codex).
- Final on-disk: `wiki/reviews/review-spec042-CX-activate-resilience.md`; on PASS, lands to `main`.

## What to do
1. `git fetch --prune origin`; `git checkout -b feature/cx-integrate-041 origin/feature/cc-activate-resilience`.
   Prefix every dotnet call: `PATH="$HOME/.dotnet:$PATH" dotnet …`.
2. Gate, each separately, real counts: `dotnet build -c Release`; `dotnet format --verify-no-changes`;
   `dotnet test`. Confirm a test can fail (invert one assertion, observe, revert).
3. `dotnet run` (default fakes) — confirm one sweep, persists, writes digest, exit 0.
4. Review against spec 041, citing `file:line`:
   - `AnthropicTransient.IsTransient` is **conservative + correct**: rate-limit (429), overloaded (529), 5xx,
     and network/transport (`HttpRequestException`/`AnthropicIOException`) → **true**; 4xx client errors
     (400/401/403/404/422) and argument/parse errors → **false**. Be adversarial: a 4xx (e.g. bad-request /
     unauthorized) must **not** be retried; check the predicate doesn't over-match. Tests cover both.
   - `SweepComposer`: `UseRealApi==true` → `RetryPolicy(MaxAttempts, BaseDelay, AnthropicTransient.IsTransient)`;
     `false` → `NoRetryPolicy`. Constructs `SweepRunner` with the retry policy, `digestOnlyNew:
     Sweep.OnlyReportNew`, and a logging `onTriageError`. Config binds `Pipeline:Retry` + `Sweep:OnlyReportNew`
     (default false). No live call, no hardcoded key (grep the diff), no `SweepRunner`/`SweepHost` seam change.
   - Existing `SweepComposer`/daemon/end-to-end tests still pass.
5. Write `wiki/reviews/review-spec042-CX-activate-resilience.md`: PASS/HOLD headline; branch SHAs; gate table
   (real counts + can-fail + clean SHA + `dotnet --version`); the run note; correctness checks (`file:line`);
   a no-secret confirmation; HOLD blockers or "None."; final line exactly `VERDICT: PASS` or `VERDICT: HOLD`.
   Commit on `feature/cx-integrate-041`, push. **Do not merge to main.**

## Definition of done
- [ ] Gate + run; can-fail; adversarial review (esp. 4xx-not-retried) with `file:line`; secret-scan clean; ends `VERDICT`.
- [ ] Branch pushed; tip SHA + verdict reported.

## Deliverable / report-back
The review file; end your final message with the `feature/cx-integrate-041` tip SHA + verdict.
