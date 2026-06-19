# Prompt — Spec 045-CC — Real-pipeline integration test

You are **CC**. Execute Spec 045-CC (`wiki/specs/045-CC-real-pipeline-integration-test.md`). Read it first.
**Test-only — no production code change.**

## Setup
- `git fetch --prune origin`; branch from origin: `git checkout -b feature/cc-pipeline-integration-test origin/main`.
- Prefix every dotnet call: `PATH="$HOME/.dotnet:$PATH" dotnet …`. Confirm `dotnet --version`.

## Steps
1. In `tests/AmetekWatch.Anthropic.Tests/`, add a **routing fake `IMessagesClient`** that returns a canned
   **search-results JSON array** for the searcher request (Sonnet 4.6 / has `web_search` tool) and a canned
   **verdict JSON** for the triage request (Opus 4.8) — route by `parameters.Model` and/or the tool. Document
   the verdict rule (fixed, or keyed off user content).
2. Add an integration test: build the **real** `AnthropicSearcher` (routing fake + `SearchRequestFactory` +
   `SearchResponseParser` + a fixed clock) and **real** `AnthropicTriageDecider` (routing fake +
   `TriageRequestFactory` + `TriageVerdictParser`); run via a real `SweepRunner` + `InMemoryFindingStore`.
   Assert (hand-computed): canned hits → `Finding`s with the 013-mapper categories (e.g. SEC/IR → FinancialReport,
   opinion/social → OpinionSocial), each triaged, **all persisted**, digest = worth-reporting subset most-recent
   first. Confirm a test can fail then revert.
3. Do **not** change production code, the existing `FakeMessagesClient`, or the `.sln`. No network, no key.

## The gate
Each separately, real counts: `PATH="$HOME/.dotnet:$PATH" dotnet build -c Release` ; `… dotnet format --verify-no-changes` ; `… dotnet test`.

## Versioning ritual
Internal — no `CHANGELOG`/tag, `<Version>` stays `0.1.0`. Append a dated `CLAUDE.md` → `### Unreleased`
entry (below existing ones; don't edit them).

## Report-back format
Write `wiki/reports/report-spec045-CC-real-pipeline-integration-test.md` per `wiki/rituals/report-format.md`
(all sections; "None." where N/A), plus a gate table (real counts, before/after, can-fail, clean SHA,
`dotnet --version`) and confirmation that no production code changed. Do **not** self-merge; push
`feature/cc-pipeline-integration-test` and end with the tip SHA + a one-line build/format/test summary. Never print or commit secrets.
