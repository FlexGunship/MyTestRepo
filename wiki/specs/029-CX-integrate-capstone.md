# Spec 029-CX — Integrate CC's spec-028 pipeline-toggle capstone

> Self-contained integration spec (no separate prompt). Same shape as [`016-CX`](016-CX-integrate-sweep-host.md).

## Status
- Doc type: integration (cross-model gate + review). Executes: **CX**; CX issues VERDICT; **CM lands on PASS**.
- Number 029 verified free (highest spec file = 028; 029 = 028 + 1).
- Reviewing: CC's `feature/cc-pipeline-toggle` (origin tip `1e1732f`). Author CC (Claude) ≠ integrator CX (Codex).
- Final on-disk: `wiki/reviews/review-spec029-CX-capstone.md`; on PASS, lands to `main`.

## What to do
1. `git fetch --prune origin`; `git checkout -b feature/cx-integrate-028 origin/feature/cc-pipeline-toggle`.
   Prefix every dotnet call: `PATH="$HOME/.dotnet:$PATH" dotnet …`.
2. Gate, each separately, real counts: `dotnet build -c Release`; `dotnet format --verify-no-changes`;
   `dotnet test`. Confirm a test can fail (invert one assertion, observe, revert).
3. **Run the exe (default fakes):** `PATH="$HOME/.dotnet:$PATH" dotnet run --project src/AmetekWatch.App` —
   capture stdout; confirm it logs the active pipeline (fake), persists to SQLite, **and writes the digest
   file** (`Notify:DigestPath`); confirm the digest file exists with content.
4. Review `src/AmetekWatch.App/` + test against spec 028, citing `file:line`:
   - `PipelineFactory` returns the **real** `AnthropicSearcher`/`AnthropicTriageDecider` when `UseRealApi=true`
     and the **fakes** when false, and **invokes nothing** (no network in selection).
   - `Program` binds `Pipeline`/`Notify`; on `UseRealApi=true` **with `ANTHROPIC_API_KEY` unset it prints a
     clear warning and falls back to fakes** (does not crash, does not silently pretend real); logs the active
     pipeline. **Grep the diff for any hardcoded key — confirm none.**
   - The digest is delivered through `IDigestNotifier` after persistence (`FileDigestNotifier` when `DigestPath`
     set, else `NullDigestNotifier`); `RunOnce` remains the default so the CLI terminates.
   - The selection test asserts resolved types **without invoking them** (no key/network); the end-to-end test
     (fakes + temp DB + temp DigestPath) asserts SQLite persistence + the digest file written.
   - No live API call anywhere in the test path; `.sln` untouched.
5. Write `wiki/reviews/review-spec029-CX-capstone.md`: PASS/HOLD headline; branch SHAs; gate table (real
   counts + can-fail + clean SHA + `dotnet --version`); the `dotnet run` stdout + digest-file confirmation;
   correctness checks (`file:line`); a no-secret confirmation; HOLD blockers or "None."; final line exactly
   `VERDICT: PASS` or `VERDICT: HOLD`. Commit on `feature/cx-integrate-028`, push. **Do not merge to main.**

## Definition of done
- [ ] Gate re-run + exe run (digest file confirmed); can-fail; adversarial review with `file:line`; secret-scan clean.
- [ ] Review ends `VERDICT: PASS`/`HOLD`; branch pushed; tip SHA + verdict reported.

## Deliverable / report-back
The review file; end your final message with the `feature/cx-integrate-028` tip SHA + verdict.
