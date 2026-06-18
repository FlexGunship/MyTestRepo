# Spec 012-CX2 — Integrate CC's spec-011 triage prompt builder

> Self-contained integration spec (no separate prompt). Same shape as [`009-CX2`](009-CX2-integrate-sqlite.md).

## Status
- Doc type: integration (cross-model gate + review). Executes: **CX2**; CX2 issues VERDICT; **CM lands on PASS**.
- Number 012 verified free (highest spec file = 011; 012 = 011 + 1).
- Reviewing: CC's `feature/cc-triage-prompt` (origin tip `fa6c199`). Author CC (Claude) ≠ integrator CX2 (Codex).
- Final on-disk: `wiki/reviews/review-spec012-CX2-triage.md`; on PASS, the Triage builder lands to `main`.

## What to do
1. `git fetch --prune origin`; `git checkout -b feature/cx2-integrate-011 origin/feature/cc-triage-prompt`.
   Prefix every dotnet call: `PATH="$HOME/.dotnet:$PATH" dotnet …`.
2. Gate, each separately, real counts: `dotnet build -c Release`; `dotnet format --verify-no-changes`;
   `dotnet test`. Confirm a test can fail (invert one assertion, observe, revert).
3. Review `src/AmetekWatch.Core/Triage/` + the new test against spec 011, citing `file:line`:
   `TriagePromptBuilder.BuildSystemPrompt()` returns the rubric and it **encodes the charter weighting**
   (special weight on personal/social opinion pieces **and** reputable-institution financial reports) and
   the three verdict dimensions (important / relevant / worth-reporting); `BuildUserContent(Finding)` is
   deterministic, includes Category/Title/Url/Snippet/PublishedAt and is null-safe on `PublishedAt`;
   **pure** — no I/O, no Anthropic/HTTP dependency, no new NuGet, `ITriageDecider`/`FakeTriageDecider`/
   `App`/`.sln` untouched. Be adversarial about purity and about whether the weighting is actually present.
4. Write `wiki/reviews/review-spec012-CX2-triage.md`: PASS/HOLD headline; branch SHAs; gate table (real
   counts + can-fail + clean SHA + `dotnet --version`); correctness checks (`file:line`); HOLD blockers or
   "None."; final line exactly `VERDICT: PASS` or `VERDICT: HOLD`. Commit on `feature/cx2-integrate-011`,
   push. **Do not merge to main.**

## Definition of done
- [ ] Gate re-run (real counts, can-fail); review with `file:line`; ends `VERDICT: PASS`/`HOLD`; pushed.

## Deliverable / report-back
The review file; end your final message with the `feature/cx2-integrate-011` tip SHA + verdict.
