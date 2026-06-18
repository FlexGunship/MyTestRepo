# Spec 010-CX — Integrate CC2's spec-008 web dashboard

> Self-contained integration spec (no separate prompt). Same shape as [`004-CX`](004-CX-integrate-vertical-slice.md).

## Status
- Doc type: integration (cross-model gate + review)
- Executes: **CX**; CX issues VERDICT; **CM lands on PASS**. CX does not merge.
- Number 010 verified free (highest spec file = 009; 010 = 009 + 1).
- Reviewing: CC2's `feature/cc2-web-dashboard` (origin tip `cf8add4`). Author CC2 (Claude) ≠ integrator CX (Codex).
- Final on-disk: `wiki/reviews/review-spec010-CX-dashboard.md`; on PASS, the Web project lands to `main`.

## What to do
1. `git fetch --prune origin`; branch `git checkout -b feature/cx-integrate-008 origin/feature/cc2-web-dashboard`
   (you cannot check out CC2's branch directly). Prefix every dotnet call: `PATH="$HOME/.dotnet:$PATH" dotnet …`.
2. Re-run the gate, each separately, real counts: `dotnet build -c Release`; `dotnet format --verify-no-changes`;
   `dotnet test`. Confirm a test can fail (invert one assertion, observe, revert).
3. Correctness review of `src/AmetekWatch.Web` + `tests/AmetekWatch.Web.Tests` against spec 008, citing
   `file:line`: `GET /api/findings` returns the store's `TriagedFinding`s most-recent first; `GET /` serves
   an HTML table; the store is seeded via one fake Core sweep (`FakeSearcher`+`FakeTriageDecider`+
   `SweepRunner`+`InMemoryFindingStore`); **no dependency on the SQLite work (007)**; binds localhost; the
   `WebApplicationFactory<Program>` test really exercises the endpoint and asserts hand-computed values;
   `AmetekWatch.App` and the slice/Storage tests are untouched. Be adversarial about whether the test
   actually hits the running app vs. a stub.
4. Write `wiki/reviews/review-spec010-CX-dashboard.md`: PASS/HOLD headline; branch SHAs; gate table (real
   counts + can-fail + clean SHA + `dotnet --version`); correctness checks (`file:line`); HOLD blockers or
   "None."; final line exactly `VERDICT: PASS` or `VERDICT: HOLD`. Commit on `feature/cx-integrate-008`,
   push. **Do not merge to main.**

## Definition of done
- [ ] Gate re-run (real counts, can-fail confirmed); correctness review with `file:line`.
- [ ] Review written ending `VERDICT: PASS`/`HOLD`; branch pushed; tip SHA + verdict reported.

## Deliverable / report-back
The review file above; end your final message with the `feature/cx-integrate-008` tip SHA + verdict.
