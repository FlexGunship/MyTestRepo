# Spec 014-CX — Integrate CC's spec-013 searcher logic

> Self-contained integration spec (no separate prompt). Same shape as [`010-CX`](010-CX-integrate-dashboard.md).

## Status
- Doc type: integration (cross-model gate + review). Executes: **CX**; CX issues VERDICT; **CM lands on PASS**.
- Number 014 verified free (highest spec file = 013; 014 = 013 + 1).
- Reviewing: CC's `feature/cc-searcher-logic` (origin tip `d05eb2f`). Author CC (Claude) ≠ integrator CX (Codex).
- Final on-disk: `wiki/reviews/review-spec014-CX-searcher.md`; on PASS, the Search logic lands to `main`.

## What to do
1. `git fetch --prune origin`; `git checkout -b feature/cx-integrate-013 origin/feature/cc-searcher-logic`.
   Prefix every dotnet call: `PATH="$HOME/.dotnet:$PATH" dotnet …`.
2. Gate, each separately, real counts: `dotnet build -c Release`; `dotnet format --verify-no-changes`;
   `dotnet test`. Confirm a test can fail (invert one assertion, observe, revert).
3. Review `src/AmetekWatch.Core/Search/` + the new test against spec 013, citing `file:line`:
   `SearchQueryBuilder.BuildQueries(SweepQuery)` returns deterministic, de-duplicated queries covering the
   subject and **both focus areas** (opinion/social + reputable financial reports);
   `SearchResultMapper.ToFinding(item, discoveredAt)` classifies via the **documented constant-list
   heuristic** (SEC/IR/earnings → `FinancialReport`; op-ed/opinion/social → `OpinionSocial`; else `Other`),
   maps all fields, and uses the **injected** `discoveredAt` (no `DateTimeOffset.Now` anywhere in Search);
   **pure** — no I/O, no Anthropic/HTTP dependency, no new NuGet; `ISearcher`/`FakeSearcher`/`App`/`.sln`
   untouched. Be adversarial: try to find a category example the heuristic misclassifies, and confirm the
   test's expectations are hand-computed (not echoing the implementation).
4. Write `wiki/reviews/review-spec014-CX-searcher.md`: PASS/HOLD headline; branch SHAs; gate table (real
   counts + can-fail + clean SHA + `dotnet --version`); correctness checks (`file:line`); HOLD blockers or
   "None."; final line exactly `VERDICT: PASS` or `VERDICT: HOLD`. Commit on `feature/cx-integrate-013`,
   push. **Do not merge to main.**

## Definition of done
- [ ] Gate re-run (real counts, can-fail); review with `file:line`; ends `VERDICT: PASS`/`HOLD`; pushed.

## Deliverable / report-back
The review file; end your final message with the `feature/cx-integrate-013` tip SHA + verdict.
