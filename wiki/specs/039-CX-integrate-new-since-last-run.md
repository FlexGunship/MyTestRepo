# Spec 039-CX — Integrate CC2's spec-038 new-since-last-run digest

> Self-contained integration spec (no separate prompt). Same shape as [`027-CX`](027-CX-integrate-anthropic-searcher.md).

## Status
- Doc type: integration (cross-model gate + review). Executes: **CX**; CX issues VERDICT; **CM lands on PASS**.
- Number 039 verified free (highest spec file = 038; 039 = 038 + 1).
- Reviewing: CC2's `feature/cc2-new-since-last-run` (origin tip `186b716`). Author CC2 (Claude) ≠ integrator CX (Codex).
- Final on-disk: `wiki/reviews/review-spec039-CX-new-since-last-run.md`; on PASS, lands to `main`.

## What to do
1. `git fetch --prune origin`; `git checkout -b feature/cx-integrate-038 origin/feature/cc2-new-since-last-run`.
   Prefix every dotnet call: `PATH="$HOME/.dotnet:$PATH" dotnet …`.
2. Gate, each separately, real counts: `dotnet build -c Release`; `dotnet format --verify-no-changes`;
   `dotnet test`. Confirm a test can fail (invert one assertion, observe, revert).
3. Review `src/AmetekWatch.Core/Pipeline/SweepRunner.cs` + the test against spec 038, citing `file:line`:
   - `digestOnlyNew` is **optional, default `false`** (backward-compatible; existing 3-arg + 034 construction
     still compiles and prior tests pass **unchanged**).
   - Newness uses a **pre-sweep snapshot** of `GetAllAsync()` `Url`s (**no `IFindingStore` interface change**);
     a finding is new iff its `Url` wasn't already present; **all** triaged findings are still **persisted**
     (upsert) regardless; ordering unchanged.
   - With `digestOnlyNew=true`, an already-known worth-reporting `Url` is **excluded from the digest** but
     **still persisted**, and genuinely-new worth-reporting findings are **included** — confirmed by a test;
     with `false`, the digest is unchanged. Be adversarial about the snapshot timing (must be taken **before**
     this sweep's saves, or everything looks "not new").
   - Core-only; App/Anthropic/.sln untouched.
4. Write `wiki/reviews/review-spec039-CX-new-since-last-run.md`: PASS/HOLD headline; branch SHAs; gate table
   (real counts + can-fail + clean SHA + `dotnet --version`); correctness checks (`file:line`); HOLD blockers
   or "None."; final line exactly `VERDICT: PASS` or `VERDICT: HOLD`. Commit on `feature/cx-integrate-038`,
   push. **Do not merge to main.**

## Definition of done
- [ ] Gate re-run (real counts, can-fail); review with `file:line`; backward-compat + snapshot-timing confirmed; ends `VERDICT`.
- [ ] Branch pushed; tip SHA + verdict reported.

## Deliverable / report-back
The review file; end your final message with the `feature/cx-integrate-038` tip SHA + verdict.
