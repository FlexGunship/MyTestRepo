# Spec 026-CX2 — Integrate CC2's spec-025 digest notifier

> Self-contained integration spec (no separate prompt). Same shape as [`009-CX2`](009-CX2-integrate-sqlite.md).

## Status
- Doc type: integration (cross-model gate + review). Executes: **CX2**; CX2 issues VERDICT; **CM lands on PASS**.
- Number 026 verified free (highest spec file = 025; 026 = 025 + 1).
- Reviewing: CC2's `feature/cc2-digest-notifier` (origin tip `d7c674b`). Author CC2 (Claude) ≠ integrator CX2 (Codex).
- Final on-disk: `wiki/reviews/review-spec026-CX2-digest-notifier.md`; on PASS, lands to `main`.

## What to do
1. `git fetch --prune origin`; `git checkout -b feature/cx2-integrate-025 origin/feature/cc2-digest-notifier`.
   Prefix every dotnet call: `PATH="$HOME/.dotnet:$PATH" dotnet …`.
2. Gate, each separately, real counts: `dotnet build -c Release`; `dotnet format --verify-no-changes`;
   `dotnet test`. Confirm a test can fail (invert one assertion, observe, revert).
3. Review `src/AmetekWatch.Core/Notify/` + the test against spec 025, citing `file:line`: `IDigestNotifier`
   shape; `FileDigestNotifier` writes a **friendly Markdown** digest (heading w/ subject+date, worth-reporting
   count, per finding Category/Title/Url + rationale) using **friendly names only** (no internal type/field
   names leak into the rendered text), overwrites each run, and uses an **injected** timestamp (no
   `DateTimeOffset.Now`); empty digest renders a clean "nothing to report"; `NullDigestNotifier` is a no-op.
   Tests use a temp file with hand-computed content. Confirm it does **not** wire into App/SweepHost, add
   email/SMTP, touch the Anthropic projects, or edit the `.sln`. Be adversarial about friendly-name leakage
   and the injected-timestamp purity.
4. Write `wiki/reviews/review-spec026-CX2-digest-notifier.md`: PASS/HOLD headline; branch SHAs; gate table
   (real counts + can-fail + clean SHA + `dotnet --version`); correctness checks (`file:line`); HOLD blockers
   or "None."; final line exactly `VERDICT: PASS` or `VERDICT: HOLD`. Commit on `feature/cx2-integrate-025`,
   push. **Do not merge to main.**

## Definition of done
- [ ] Gate re-run (real counts, can-fail); review with `file:line`; ends `VERDICT: PASS`/`HOLD`; pushed.

## Deliverable / report-back
The review file; end your final message with the `feature/cx2-integrate-025` tip SHA + verdict.
