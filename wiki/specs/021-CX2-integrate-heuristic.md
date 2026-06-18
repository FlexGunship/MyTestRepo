# Spec 021-CX2 — Integrate CC2's spec-020 searcher heuristic refinement

> Self-contained integration spec (no separate prompt). Same shape as [`009-CX2`](009-CX2-integrate-sqlite.md).

## Status
- Doc type: integration (cross-model gate + review). Executes: **CX2**; CX2 issues VERDICT; **CM lands on PASS**.
- Number 021 verified free (highest spec file = 020; 021 = 020 + 1).
- Reviewing: CC2's `feature/cc2-searcher-heuristic` (origin tip `17776b6`). Author CC2 (Claude) ≠ integrator CX2 (Codex).
- Final on-disk: `wiki/reviews/review-spec021-CX2-heuristic.md`; on PASS, lands to `main`.

## What to do
1. `git fetch --prune origin`; `git checkout -b feature/cx2-integrate-020 origin/feature/cc2-searcher-heuristic`.
   Prefix every dotnet call: `PATH="$HOME/.dotnet:$PATH" dotnet …`.
2. Gate, each separately, real counts: `dotnet build -c Release`; `dotnet format --verify-no-changes`;
   `dotnet test`. Confirm a test can fail (invert one assertion, observe, revert).
3. Review `src/AmetekWatch.Core/Search/SearchResultMapper.cs` + the tests against spec 020, citing
   `file:line`: the precedence is **IR/SEC domain → FinancialReport; then social domain OR opinion title →
   OpinionSocial; then financial title (non-social/non-IR) → FinancialReport; else Other**; the **flagged
   case** (social domain + earnings-signal title → `OpinionSocial`) is covered by a test and actually
   produces `OpinionSocial`; a plain news article titled "AMETEK Q2 earnings" still → `FinancialReport`;
   IR/SEC domain still → `FinancialReport`; the public signature, the explicit constant lists, and the
   injected-`discoveredAt` purity are unchanged. Verify any **changed 013 test** the report names was a
   legitimate consequence of the new precedence (not a weakened assertion). Be adversarial: try a couple of
   your own borderline inputs mentally and confirm the documented order yields the right bucket.
4. Write `wiki/reviews/review-spec021-CX2-heuristic.md`: PASS/HOLD headline; branch SHAs; gate table (real
   counts + can-fail + clean SHA + `dotnet --version`); correctness checks (`file:line`); HOLD blockers or
   "None."; final line exactly `VERDICT: PASS` or `VERDICT: HOLD`. Commit on `feature/cx2-integrate-020`,
   push. **Do not merge to main.**

## Definition of done
- [ ] Gate re-run (real counts, can-fail); review with `file:line`; ends `VERDICT: PASS`/`HOLD`; pushed.

## Deliverable / report-back
The review file; end your final message with the `feature/cx2-integrate-020` tip SHA + verdict.
