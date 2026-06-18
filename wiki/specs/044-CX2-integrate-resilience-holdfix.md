# Spec 044-CX2 — Integrate CC's spec-043 resilience HOLD-fix (lands 041 + fix)

> Self-contained integration spec (no separate prompt). Re-integration after the 042 HOLD; **fresh Codex** (CX2 ≠ 042's CX).

## Status
- Doc type: integration (cross-model gate + review). Executes: **CX2**; CX2 issues VERDICT; **CM lands on PASS**.
- Number 044 verified free (highest spec file = 043; 044 = 043 + 1).
- Reviewing: CC's `feature/cc-resilience-holdfix` (origin tip `9e7992a`) — carries **all of 041** (AnthropicTransient,
  SweepComposer retry/new-only wiring, SweepHost optional-runner seam) **plus** the 043 fix. Author CC (Claude) ≠ integrator CX2 (Codex).
- Context: this resolves the **042 HOLD** ([`../reviews/review-spec042-CX-activate-resilience.md`](../reviews/review-spec042-CX-activate-resilience.md)).
  Spec 043 **blessed** the optional `SweepRunner? runner = null` `SweepHost` seam (it's the App-side composed-runner
  injection point; backward-compatible) — so that is **no longer a blocker**. Verify the two 042 blockers are resolved.
- Final on-disk: `wiki/reviews/review-spec044-CX2-resilience-holdfix.md`; on PASS, lands to `main`.

## What to do
1. `git fetch --prune origin`; `git checkout -b feature/cx2-integrate-043 origin/feature/cc-resilience-holdfix`.
   Prefix every dotnet call: `PATH="$HOME/.dotnet:$PATH" dotnet …`.
2. Gate, each separately, real counts: `dotnet build -c Release`; `dotnet format --verify-no-changes`;
   `dotnet test` (expect 118). Confirm a test can fail (invert one assertion, observe, revert).
3. `dotnet run` (default fakes) — confirm one sweep, persists, writes digest, exit 0.
4. **Confirm the 042 blockers are resolved**, citing `file:line`:
   - **401 + 403** no-retry tests now exist (`AnthropicTransient.IsTransient(<401/403>) == false`).
   - The `SweepHost` optional `SweepRunner? runner = null` seam is **backward-compatible** (null → builds its
     own runner; existing construction still compiles + prior tests pass) — now blessed by 043.
   Then re-verify the 041 substance: `AnthropicTransient` conservative (429/529/5xx/network → true; 4xx +
   argument/parse → false); `SweepComposer` selects `RetryPolicy` (real) vs `NoRetryPolicy` (fake) and passes
   `digestOnlyNew: Sweep.OnlyReportNew` + logging `onTriageError`; config binds `Pipeline:Retry` +
   `Sweep:OnlyReportNew` (default false); **no live call, no hardcoded key** (grep the diff); `.sln` untouched.
5. Write `wiki/reviews/review-spec044-CX2-resilience-holdfix.md`: PASS/HOLD headline; branch SHAs; gate table
   (real counts + can-fail + clean SHA + `dotnet --version`); the run note; "042 blockers resolved" checks
   (`file:line`); correctness re-verification; a no-secret confirmation; HOLD blockers or "None."; final line
   exactly `VERDICT: PASS` or `VERDICT: HOLD`. Commit on `feature/cx2-integrate-043`, push. **Do not merge to main.**

## Definition of done
- [ ] Gate + run; can-fail; 042 blockers confirmed resolved + 041 substance re-verified (`file:line`); secret-scan clean; ends `VERDICT`.
- [ ] Branch pushed; tip SHA + verdict reported.

## Deliverable / report-back
The review file; end your final message with the `feature/cx2-integrate-043` tip SHA + verdict.
