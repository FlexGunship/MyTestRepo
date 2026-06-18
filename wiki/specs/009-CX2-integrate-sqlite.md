# Spec 009-CX2 — Integrate CC's spec-007 SQLite store

> Self-contained integration spec (no separate prompt). Same shape as the successful
> [`004-CX`](004-CX-integrate-vertical-slice.md) integration.

## Status
- Doc type: integration (cross-model gate + review)
- Executes: **CX2**; CX2 issues VERDICT; **CM lands on PASS**. CX2 does not merge.
- Number 009 verified free (highest spec file = 008; 009 = 008 + 1).
- Reviewing: CC's `feature/cc-sqlite-store` (origin tip `ac719b5`). Author CC (Claude) ≠ integrator CX2 (Codex).
- Final on-disk: `wiki/reviews/review-spec009-CX2-sqlite.md`; on PASS, the Storage project lands to `main`.

## What to do
1. `git fetch --prune origin`; branch `git checkout -b feature/cx2-integrate-007 origin/feature/cc-sqlite-store`
   (you cannot check out CC's branch directly). Prefix every dotnet call: `PATH="$HOME/.dotnet:$PATH" dotnet …`.
2. Re-run the gate, each separately, real counts: `dotnet build -c Release`; `dotnet format --verify-no-changes`;
   `dotnet test`. Independently confirm a test can fail (invert one assertion, observe, revert).
3. Correctness review of `src/AmetekWatch.Storage` + its tests against spec 007, citing `file:line`:
   `SqliteFindingStore : IFindingStore` creates schema on init; `SaveAsync` **upserts by `Url`** (one row
   per URL, latest wins); `GetAllAsync` orders most-recent `discovered_at` first; `DateTimeOffset` and
   nullable `PublishedAt` round-trip faithfully (no silent truncation/precision loss — check the actual
   round-trip values); category stored/read as the enum name; no change to `AmetekWatch.App` or the slice
   test project. Be adversarial about date and upsert correctness specifically.
4. Write `wiki/reviews/review-spec009-CX2-sqlite.md`: PASS/HOLD headline; branch SHAs; gate table (real
   counts + can-fail + clean SHA + `dotnet --version`); correctness checks (`file:line`); HOLD blockers or
   "None."; final line exactly `VERDICT: PASS` or `VERDICT: HOLD`. Commit on `feature/cx2-integrate-007`,
   push. **Do not merge to main.**

## Definition of done
- [ ] Gate re-run (real counts, can-fail confirmed); correctness review with `file:line`.
- [ ] Review written ending `VERDICT: PASS`/`HOLD`; branch pushed; tip SHA + verdict reported.

## Deliverable / report-back
The review file above; end your final message with the `feature/cx2-integrate-007` tip SHA + verdict.
