# Spec 011-CC — Triage prompt & rubric builder (pure, no API)

## Status
- Doc type: implementation (the decider tier's prompt construction — pure logic, behind the seam)
- Executes: **CC**; pushes `feature/cc-triage-prompt`; **CX2** integrates (cross-model); CM lands on PASS. No self-merge.
- Number 011 verified free (highest spec file = 010; 011 = 010 + 1).
- Paired prompt: prompt-spec011-CC-triage-prompt-builder.md
- Final on-disk: new files under `src/AmetekWatch.Core/Triage/` + a new test file in the existing
  `tests/AmetekWatch.Tests/` project (no new project / no `.sln` change, to avoid merge churn).

## Background
The Opus-4.8 decider judges each finding for importance, relevance, and whether it's worth reporting —
the charter's core "judgment is the product." The real Anthropic call is deferred (auth last), but the
**prompt construction is pure, testable logic** we can build now and feed to the SDK-backed
`ITriageDecider` later. This builds the system prompt (the rubric) and the per-finding user content.

## Decisions made
1. **New folder `src/AmetekWatch.Core/Triage/`** (no new project, no new NuGet — keep it in Core):
   - `TriageRubric.cs` — the **system-prompt text** as a constant/builder. It must encode the charter's
     weighting: general AMETEK awareness with **special weight on personal/social opinion pieces** and
     **financial reports from reputable institutions**; define what "important", "relevant", and "worth
     reporting" mean; instruct the model to return a structured verdict (importance/relevance/
     worth-reporting booleans + a short rationale). Friendly, source-neutral wording.
   - `TriagePromptBuilder.cs` — `BuildSystemPrompt()` → the rubric; `BuildUserContent(Finding)` → a
     deterministic string containing the finding's `Category`, `Title`, `Url`, `Snippet`, and
     `PublishedAt` (handle null), clearly labeled. Pure functions, no I/O.
2. **Do not call any API, add any HTTP/Anthropic dependency, or modify `ITriageDecider`/`FakeTriageDecider`.**
   This is prompt *construction* only; wiring is a later spec.
3. **Tests** — add a new file `tests/AmetekWatch.Tests/TriagePromptBuilderTests.cs` to the **existing**
   `AmetekWatch.Tests` project (SDK-style globbing picks it up — **no `.csproj` or `.sln` edit**, which
   keeps this unit free of merge conflicts with the in-flight dashboard work). Assert: the system prompt
   contains the opinion/social + reputable-financial weighting and the three verdict dimensions;
   `BuildUserContent` includes each finding field with the right values and handles null `PublishedAt`;
   output is deterministic for a given `Finding`. Hand-computed expectations; confirm a test can fail then revert.

## Out of scope
- The real Opus call, prompt caching, structured-output schema wiring (later). Verdict parsing (later
  unit). The searcher. Anything touching `AmetekWatch.App` or other specs' files.

## Definition of done
- [ ] `src/AmetekWatch.Core/Triage/{TriageRubric,TriagePromptBuilder}.cs` implemented (pure).
- [ ] `tests/AmetekWatch.Tests/TriagePromptBuilderTests.cs` added (existing project; no `.sln` change) with the assertions above (can-fail confirmed).
- [ ] Gate green (build/format/test, each separately, real counts). Branch pushed; tip SHA reported.

## Deliverable / report-back
See the prompt file.
