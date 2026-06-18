# Prompt — Spec 020-CC2 — Refine searcher category heuristic

You are **CC2**. Execute Spec 020-CC2 (`wiki/specs/020-CC2-searcher-heuristic-refine.md`). Read it first.

## Setup
- `git fetch --prune origin`; branch from origin: `git checkout -b feature/cc2-searcher-heuristic origin/main`.
- Prefix every dotnet call: `PATH="$HOME/.dotnet:$PATH" dotnet …`. Confirm `dotnet --version`.

## Steps
1. Edit `src/AmetekWatch.Core/Search/SearchResultMapper.cs` to the refined precedence in spec Decision 1
   (IR/SEC domain → `FinancialReport`; then social domain **or** opinion/blog title → `OpinionSocial`; then
   financial-title from a non-social/non-IR source → `FinancialReport`; else `Other`). Document the order in
   comments; keep the public signature, the explicit constant lists, and the injected-`discoveredAt` purity.
2. Add the tests from spec Decision 2 (extend `tests/AmetekWatch.Tests/SearcherLogicTests.cs` or add a file).
   Hand-computed; confirm a test can fail then revert. If any existing 013 test's expectation legitimately
   changes under the new precedence, update it and note exactly which in the report.
3. Do **not** touch `SearchQueryBuilder`/`SearchResultItem`, any non-Search source, the Anthropic work
   (019), or the `.sln`.

## The gate
Each separately, real counts: `PATH="$HOME/.dotnet:$PATH" dotnet build -c Release` ; `… dotnet format --verify-no-changes` ; `… dotnet test`.

## Versioning ritual
Internal — no `CHANGELOG`/tag, `<Version>` stays `0.1.0`. Append a dated `CLAUDE.md` → `### Unreleased`
entry (below existing ones; don't edit them).

## Report-back format
Write `wiki/reports/report-spec020-CC2-searcher-heuristic-refine.md` per `wiki/rituals/report-format.md` (all
sections; "None." where N/A), plus a gate table (real counts, test count before/after, can-fail, clean SHA,
`dotnet --version`) and a list of any 013 tests whose expectations changed. Do **not** self-merge; push
`feature/cc2-searcher-heuristic` and end with the tip SHA + a one-line build/format/test summary. Never print
or commit secrets.
