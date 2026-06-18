# Prompt — Spec 013-CC — Searcher query & result-mapping logic

You are **CC**. Execute Spec 013-CC (`wiki/specs/013-CC-searcher-logic.md`). Read it first.

## Setup
- `git fetch --prune origin`; branch from origin: `git checkout -b feature/cc-searcher-logic origin/main`.
- Prefix every dotnet call: `PATH="$HOME/.dotnet:$PATH" dotnet …`. Confirm `dotnet --version`.

## Steps
1. Create `src/AmetekWatch.Core/Search/SearchResultItem.cs`, `SearchQueryBuilder.cs`,
   `SearchResultMapper.cs` per spec Decision 1 — pure C#, no I/O, no `DateTimeOffset.Now` (inject
   `discoveredAt`), no new NuGet/project. Category heuristic driven by explicit, commented constant lists.
2. Add `tests/AmetekWatch.Tests/SearcherLogicTests.cs` to the **existing** `AmetekWatch.Tests` project (do
   NOT edit its `.csproj` or the `.sln`). Tests per spec Decision 3, hand-computed; confirm one can fail then revert.
3. Do **not** call any API, add HTTP/Anthropic deps, or modify `ISearcher`/`FakeSearcher`/`App`/other
   projects/the `.sln`.

## The gate
Each separately, real counts: `PATH="$HOME/.dotnet:$PATH" dotnet build -c Release` ; `… dotnet format --verify-no-changes` ; `… dotnet test`.

## Versioning ritual
Internal — no `CHANGELOG`/tag, `<Version>` stays `0.1.0`. Append a dated `CLAUDE.md` → `### Unreleased`
entry (below existing ones; don't edit them).

## Report-back format
Write `wiki/reports/report-spec013-CC-searcher-logic.md` per `wiki/rituals/report-format.md` (all
sections; "None." where N/A), plus a gate table (real counts, test count before/after, can-fail check,
clean SHA, `dotnet --version`). Do **not** self-merge; push `feature/cc-searcher-logic` and end your
message with the tip SHA + a one-line build/format/test summary. Never print or commit secrets.
