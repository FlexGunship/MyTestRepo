# Spec 018-CX2 — Integrate CC's spec-017 dashboard-reads-SQLite

> Self-contained integration spec (no separate prompt). Same shape as [`009-CX2`](009-CX2-integrate-sqlite.md).

## Status
- Doc type: integration (cross-model gate + review). Executes: **CX2**; CX2 issues VERDICT; **CM lands on PASS**.
- Number 018 verified free (highest spec file = 017; 018 = 017 + 1).
- Reviewing: CC's `feature/cc-dashboard-sqlite` (origin tip `bc54bd4`). Author CC (Claude) ≠ integrator CX2 (Codex).
- Final on-disk: `wiki/reviews/review-spec018-CX2-dashboard-sqlite.md`; on PASS, lands to `main`.

## What to do
1. `git fetch --prune origin`; `git checkout -b feature/cx2-integrate-017 origin/feature/cc-dashboard-sqlite`.
   Prefix every dotnet call: `PATH="$HOME/.dotnet:$PATH" dotnet …`.
2. Gate, each separately, real counts: `dotnet build -c Release`; `dotnet format --verify-no-changes`;
   `dotnet test`. Confirm a test can fail (invert one assertion, observe, revert).
3. Review `src/AmetekWatch.Web/` + the updated tests against spec 017, citing `file:line`: Web reads
   `Storage:DbPath` (default `ametek-watch.db`) and serves findings from `SqliteFindingStore` (the
   in-memory fake seeding is **gone**); a **fresh/empty DB returns `[]`** (no crash — verify there is a test
   for this); `GET /api/findings` returns seeded findings most-recent-first; endpoints/localhost/read-only
   unchanged; the test overrides `Storage:DbPath` to a temp DB seeded via `SqliteFindingStore` with
   hand-computed expectations; no Anthropic/HTTP dependency; sweep host (015) and non-Web source untouched.
   Be adversarial about the empty-DB path and that the test really points at the temp DB (not a stale file).
4. Write `wiki/reviews/review-spec018-CX2-dashboard-sqlite.md`: PASS/HOLD headline; branch SHAs; gate table
   (real counts + can-fail + clean SHA + `dotnet --version`); correctness checks (`file:line`); HOLD blockers
   or "None."; final line exactly `VERDICT: PASS` or `VERDICT: HOLD`. Commit on `feature/cx2-integrate-017`,
   push. **Do not merge to main.**

## Definition of done
- [ ] Gate re-run (real counts, can-fail); review with `file:line`; ends `VERDICT: PASS`/`HOLD`; pushed.

## Deliverable / report-back
The review file; end your final message with the `feature/cx2-integrate-017` tip SHA + verdict.
