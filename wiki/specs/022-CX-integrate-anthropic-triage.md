# Spec 022-CX — Integrate CC's spec-019 Anthropic triage adapter

> Self-contained integration spec (no separate prompt). Same shape as [`010-CX`](010-CX-integrate-dashboard.md).

## Status
- Doc type: integration (cross-model gate + review). Executes: **CX**; CX issues VERDICT; **CM lands on PASS**.
- Number 022 verified free (highest spec file = 021; 022 = 021 + 1).
- Reviewing: CC's `feature/cc-anthropic-triage` (origin tip `89e0a96`). Author CC (Claude) ≠ integrator CX (Codex).
- Final on-disk: `wiki/reviews/review-spec022-CX-anthropic-triage.md`; on PASS, `AmetekWatch.Anthropic` lands to `main`.

## What to do
1. `git fetch --prune origin`; `git checkout -b feature/cx-integrate-019 origin/feature/cc-anthropic-triage`.
   Prefix every dotnet call: `PATH="$HOME/.dotnet:$PATH" dotnet …`. (NuGet `Anthropic` 12.29.1 restores from nuget.org.)
2. Gate, each separately, real counts: `dotnet build -c Release`; `dotnet format --verify-no-changes`;
   `dotnet test`. Confirm a test can fail (invert one assertion, observe, revert).
3. Review `src/AmetekWatch.Anthropic/` + tests against spec 019, citing `file:line`. **This is the first
   real-SDK code — be adversarial:**
   - `TriageRequestFactory` builds a `claude-opus-4-8` request whose **system block is the 011 rubric AND
     carries cache control** (prompt caching), whose user content == `TriagePromptBuilder.BuildUserContent`,
     and whose structured-output schema has exactly `{important, relevant, worthReporting, rationale}`
     (booleans + string), all required.
   - `TriageVerdictParser` maps the structured JSON to `TriageVerdict` and throws clearly on malformed JSON.
   - `AnthropicTriageDecider` (via `FakeMessagesClient`) returns the expected verdict from canned JSON; the
     fake is used in tests — **no network**.
   - **`AnthropicMessagesClient` (the live wrapper) is NOT unit-tested** (expected) but must compile, read
     `ANTHROPIC_API_KEY` from the environment (not hardcoded), and **never log/print/commit the key**. Grep
     the diff for any literal key / token; confirm none.
   - No live API call anywhere in the test path; `AmetekWatch.App`, the sweep host, and other projects are untouched.
4. Write `wiki/reviews/review-spec022-CX-anthropic-triage.md`: PASS/HOLD headline; branch SHAs; gate table
   (real counts + can-fail + clean SHA + `dotnet --version` + the resolved `Anthropic` NuGet version);
   correctness checks (`file:line`); a line confirming **no secret/key is present** in the diff; HOLD blockers
   or "None."; final line exactly `VERDICT: PASS` or `VERDICT: HOLD`. Commit on `feature/cx-integrate-019`,
   push. **Do not merge to main.**

## Definition of done
- [ ] Gate re-run (real counts, can-fail); adversarial review with `file:line`; secret-scan clean.
- [ ] Review ends `VERDICT: PASS`/`HOLD`; branch pushed; tip SHA + verdict reported.

## Deliverable / report-back
The review file; end your final message with the `feature/cx-integrate-019` tip SHA + verdict.
