# Spec 033-CX — Integrate CC's spec-032 digest-sink wiring

> Self-contained integration spec (no separate prompt). Same shape as [`029-CX`](029-CX-integrate-capstone.md).

## Status
- Doc type: integration (cross-model gate + review). Executes: **CX**; CX issues VERDICT; **CM lands on PASS**.
- Number 033 verified free (highest spec file = 032; 033 = 032 + 1).
- Reviewing: CC's `feature/cc-wire-digest-sink` (origin tip `979ed76`). Author CC (Claude) ≠ integrator CX (Codex).
- Final on-disk: `wiki/reviews/review-spec033-CX-digest-wiring.md`; on PASS, lands to `main`.

## What to do
1. `git fetch --prune origin`; `git checkout -b feature/cx-integrate-032 origin/feature/cc-wire-digest-sink`.
   Prefix every dotnet call: `PATH="$HOME/.dotnet:$PATH" dotnet …`.
2. Gate, each separately, real counts: `dotnet build -c Release`; `dotnet format --verify-no-changes`;
   `dotnet test`. Confirm a test can fail (invert one assertion, observe, revert).
3. **Run the exe (default File sink):** `PATH="$HOME/.dotnet:$PATH" dotnet run --project src/AmetekWatch.App`
   — confirm it still persists to SQLite and **writes the file digest**.
4. Review `src/AmetekWatch.App/` + test against spec 032, citing `file:line`: `DigestNotifierFactory` (or the
   selection helper) resolves `FileDigestNotifier` for `Notify:Sink="File"` (default), `EmailDigestNotifier`
   for `"Email"` (constructed only — **no live SMTP send/network in tests**), and `NullDigestNotifier` for
   `"None"` **and for incomplete/disabled email** (with a clear logged warning, no crash); `Program` uses it;
   `SweepHost`, the notifier impls (025/030), the Anthropic projects, and the `.sln` are **untouched**. The
   028 end-to-end test (default File) still passes. Grep the diff for any hardcoded secret — confirm none.
5. Write `wiki/reviews/review-spec033-CX-digest-wiring.md`: PASS/HOLD headline; branch SHAs; gate table (real
   counts + can-fail + clean SHA + `dotnet --version`); the `dotnet run` digest-file confirmation; correctness
   checks (`file:line`); a no-secret confirmation; HOLD blockers or "None."; final line exactly `VERDICT: PASS`
   or `VERDICT: HOLD`. Commit on `feature/cx-integrate-032`, push. **Do not merge to main.**

## Definition of done
- [ ] Gate re-run + exe run (digest confirmed); can-fail; review with `file:line`; secret-scan clean.
- [ ] Review ends `VERDICT: PASS`/`HOLD`; branch pushed; tip SHA + verdict reported.

## Deliverable / report-back
The review file; end your final message with the `feature/cx-integrate-032` tip SHA + verdict.
