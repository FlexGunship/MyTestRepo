# Spec 016-CX — Integrate CC2's spec-015 app sweep host

> Self-contained integration spec (no separate prompt). Same shape as [`010-CX`](010-CX-integrate-dashboard.md).

## Status
- Doc type: integration (cross-model gate + review). Executes: **CX**; CX issues VERDICT; **CM lands on PASS**.
- Number 016 verified free (highest spec file = 015; 016 = 015 + 1).
- Reviewing: CC2's `feature/cc2-app-sweep-host` (origin tip `912e2ae`). Author CC2 (Claude) ≠ integrator CX (Codex).
- Final on-disk: `wiki/reviews/review-spec016-CX-sweep-host.md`; on PASS, the App sweep host lands to `main`.

## What to do
1. `git fetch --prune origin`; `git checkout -b feature/cx-integrate-015 origin/feature/cc2-app-sweep-host`.
   Prefix every dotnet call: `PATH="$HOME/.dotnet:$PATH" dotnet …`.
2. Gate, each separately, real counts: `dotnet build -c Release`; `dotnet format --verify-no-changes`;
   `dotnet test`. Confirm a test can fail (invert one assertion, observe, revert).
3. **Actually run it:** `PATH="$HOME/.dotnet:$PATH" dotnet run --project src/AmetekWatch.App` — capture the
   digest stdout and confirm a SQLite DB file is created (and that `appsettings.json` is found at runtime).
4. Review `src/AmetekWatch.App/` + the new test against spec 015, citing `file:line`: `SweepHost.RunOnceAsync`
   runs one `SweepRunner` sweep and **persists to the injected `IFindingStore`**, returning the worth-reporting
   digest; `RunAsync` loops and is **cancellation-friendly** (honors the token, doesn't spin); config binds
   `SweepOptions` (Subject/IntervalMinutes/RunOnce/DbPath); `Program` constructs `SqliteFindingStore` + the
   **fakes** (no real Anthropic/HTTP dependency anywhere — auth still deferred) and defaults to `RunOnce` so
   the CLI terminates; the test uses a temp-file DB and asserts hand-computed persistence + digest. Be
   adversarial about the cancellation loop and about whether persistence actually hits SQLite (not in-memory).
5. Write `wiki/reviews/review-spec016-CX-sweep-host.md`: PASS/HOLD headline; branch SHAs; gate table (real
   counts + can-fail + clean SHA + `dotnet --version`); the `dotnet run` digest stdout; correctness checks
   (`file:line`); HOLD blockers or "None."; final line exactly `VERDICT: PASS` or `VERDICT: HOLD`. Commit on
   `feature/cx-integrate-015`, push. **Do not merge to main.**

## Definition of done
- [ ] Gate re-run + app run (digest captured); can-fail confirmed; review with `file:line`.
- [ ] Review ends `VERDICT: PASS`/`HOLD`; branch pushed; tip SHA + verdict reported.

## Deliverable / report-back
The review file; end your final message with the `feature/cx-integrate-015` tip SHA + verdict.
