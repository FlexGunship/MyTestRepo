# Spec 040-CX2 — Integrate CC's spec-036 scheduled host

> Self-contained integration spec (no separate prompt). Same shape as [`029-CX`](029-CX-integrate-capstone.md).

## Status
- Doc type: integration (cross-model gate + review). Executes: **CX2**; CX2 issues VERDICT; **CM lands on PASS**.
- Number 040 verified free (highest spec file = 039; 040 = 039 + 1).
- Reviewing: CC's `feature/cc-scheduled-host` (origin tip `c8f792a`). Author CC (Claude) ≠ integrator CX2 (Codex).
- Note: 036 **branched before 034/038 landed**, so its diff vs `main` shows their test files as "deletions" —
  that is a base artifact (they're preserved on merge); confirm they're **not** actually removed by 036's tree.
- Final on-disk: `wiki/reviews/review-spec040-CX2-scheduled-host.md`; on PASS, lands to `main`.

## What to do
1. `git fetch --prune origin`; `git checkout -b feature/cx2-integrate-036 origin/feature/cc-scheduled-host`.
   Prefix every dotnet call: `PATH="$HOME/.dotnet:$PATH" dotnet …`.
2. Gate, each separately, real counts: `dotnet build -c Release`; `dotnet format --verify-no-changes`;
   `dotnet test`. Confirm a test can fail (invert one assertion, observe, revert).
3. **Run both modes:** `Sweep:RunOnce=true` (default) — `PATH="$HOME/.dotnet:$PATH" dotnet run --project
   src/AmetekWatch.App` runs one sweep, persists, writes the digest, exits 0. Then exercise the daemon path
   (`RunOnce=false`) at least via the test (a real `dotnet run` daemon won't exit — the **test** is the proof).
4. Review `src/AmetekWatch.App/` + tests against spec 036, citing `file:line`:
   - `SweepBackgroundService` runs `SweepHost.RunAsync(stoppingToken)` and **stops promptly on cancel**
     (graceful shutdown) — confirmed by a test that runs **≥2 sweeps on a short interval** then cancels.
   - `Program`: `RunOnce=true` → one-shot exit 0 (unchanged); `RunOnce=false` → Generic Host daemon.
   - **`SweepComposer` preserves the 028/032 behaviour** it refactored: pipeline-tier select (real vs fake)
     with the **env-key fallback + warning**, SQLite store, tolerant `Notify` bind, **digest-sink select
     (File/Email/None)** — no behaviour regressed, no live call, **no hardcoded secret** (grep the diff).
   - `LoggingDigestNotifier` is a pure decorator (logs + delegates), doesn't change digest content.
   - `SweepHost`/`SweepRunner` seams and other projects untouched; `.sln` untouched.
5. Write `wiki/reviews/review-spec040-CX2-scheduled-host.md`: PASS/HOLD headline; branch SHAs; gate table
   (real counts + can-fail + clean SHA + `dotnet --version`); both-mode run notes; correctness checks
   (`file:line`); a no-secret confirmation; HOLD blockers or "None."; final line exactly `VERDICT: PASS` or
   `VERDICT: HOLD`. Commit on `feature/cx2-integrate-036`, push. **Do not merge to main.**

## Definition of done
- [ ] Gate + RunOnce exe run; daemon test confirmed; can-fail; review with `file:line`; secret-scan clean; ends `VERDICT`.
- [ ] Branch pushed; tip SHA + verdict reported.

## Deliverable / report-back
The review file; end your final message with the `feature/cx2-integrate-036` tip SHA + verdict.
