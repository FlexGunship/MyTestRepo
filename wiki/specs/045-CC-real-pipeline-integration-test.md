# Spec 045-CC — Real-pipeline integration test (assembled, offline)

## Status
- Doc type: implementation (test only — verify the assembled real pipeline end-to-end, offline)
- Executes: **CC**; pushes `feature/cc-pipeline-integration-test`; **CX** integrates (cross-model); CM lands on PASS. No self-merge.
- Number 045 verified free (search `wiki/specs/`; this is highest + 1).
- Paired prompt: prompt-spec045-CC-real-pipeline-integration-test.md
- Final on-disk: `tests/AmetekWatch.Anthropic.Tests/` (a routing fake + an integration test). **No production code change.**

## Background
Each real adapter (`AnthropicSearcher` 024, `AnthropicTriageDecider` 019) is unit-tested **in isolation**,
but nothing verifies them **wired together** through `SweepRunner` → store → digest. A wiring bug (e.g. the
searcher's `Finding`s not flowing into triage, or category/verdict mismatches) would slip past the isolated
tests. This adds an **offline integration test** of the assembled real pipeline — high value, zero new
production code, no network/key.

## Decisions made
1. **Routing fake `IMessagesClient`** (test helper) — unlike the existing single-response `FakeMessagesClient`,
   it **routes by request**: inspect `parameters.Model` (Sonnet 4.6 → searcher; Opus 4.8 → triage) and/or the
   presence of the `web_search` tool, returning a canned **search-results JSON array** for the searcher request
   and a canned **verdict JSON** for each triage request. (If per-finding distinct verdicts are wanted, key off
   the user content; otherwise a fixed verdict rule is fine — document it.)
2. **Assemble the real pipeline:** construct the **real** `AnthropicSearcher` (routing fake +
   `SearchRequestFactory` + `SearchResponseParser` + a fixed `() => DateTimeOffset` clock) and the **real**
   `AnthropicTriageDecider` (routing fake + `TriageRequestFactory` + `TriageVerdictParser`). Run them through a
   real `SweepRunner` (those two + an `InMemoryFindingStore`).
3. **Assert end-to-end** with hand-computed oracles: the canned search hits become `Finding`s with the
   **013-mapper categories** (e.g. an SEC/IR hit → `FinancialReport`, an opinion/social hit → `OpinionSocial`),
   each is triaged via the canned verdict(s), **all persisted**, and the returned digest = the worth-reporting
   subset, ordered most-recent first. Confirm a test can fail then revert.
4. **No production code change.** Test-only; reuse the real factories/parsers/adapters. No `.sln` change
   (existing `AmetekWatch.Anthropic.Tests` project). No network, no key.

## Out of scope
- Changing the production adapters or the existing `FakeMessagesClient`. Live API. The `pause_turn` loop.

## Definition of done
- [ ] A routing fake `IMessagesClient` + an integration test assembling real `AnthropicSearcher` +
      `AnthropicTriageDecider` through `SweepRunner`, asserting hits → categorized findings → triaged →
      persisted → digest (hand-computed). Can-fail confirmed.
- [ ] Gate green (build/format/test, each separately, real counts). Existing tests still green. Branch pushed; tip SHA reported.

## Deliverable / report-back
See the prompt file.
