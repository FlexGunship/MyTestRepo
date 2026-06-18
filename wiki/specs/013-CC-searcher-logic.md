# Spec 013-CC — Searcher query & result-mapping logic (pure, no API)

## Status
- Doc type: implementation (the searcher tier's pure logic, behind the seam)
- Executes: **CC**; pushes `feature/cc-searcher-logic`; **CX** integrates (cross-model); CM lands on PASS. No self-merge.
- Number 013 verified free (highest spec file = 012; 013 = 012 + 1).
- Paired prompt: prompt-spec013-CC-searcher-logic.md
- Final on-disk: new files under `src/AmetekWatch.Core/Search/` + a new test file in the existing
  `tests/AmetekWatch.Tests/` (no new project / no `.sln` change).

## Background
The Sonnet-4.6 searcher will call the server-side `web_search` tool and turn results into `Finding`s.
The API call is deferred (auth last), but the **query construction and result→Finding mapping are pure,
testable logic** we can build now and feed to the SDK-backed `ISearcher` later.

## Decisions made
1. **New folder `src/AmetekWatch.Core/Search/`** (no new project / NuGet):
   - `SearchResultItem.cs` — a record for one raw search hit the real searcher will yield:
     `string Url`, `string Title`, `string Snippet`, `DateTimeOffset? PublishedAt`, `string? SourceDomain`.
   - `SearchQueryBuilder.cs` — `BuildQueries(SweepQuery)` → an ordered, de-duplicated list of query strings
     covering the subject plus the **two focus areas**: opinion/social sentiment and reputable financial
     reports (e.g. a general query, an opinion/sentiment query, and a financial-report/earnings query).
     Deterministic; pure.
   - `SearchResultMapper.cs` — `ToFinding(SearchResultItem, DateTimeOffset discoveredAt)` → `Finding`, with
     a **documented category heuristic**: `FinancialReport` when the source/title signals an institutional
     financial report (e.g. SEC/EDGAR or investor-relations domains, or title contains earnings / 10-Q /
     10-K / annual report); `OpinionSocial` when it signals opinion/social (op-ed/opinion/blog or known
     social domains); else `Other`. Keep the heuristic small, commented, and **driven by explicit
     constant lists** so tests pin it. `discoveredAt` is injected (no `DateTimeOffset.Now` inside — keep it
     pure/testable).
2. **No API call, no HTTP/Anthropic dependency, no change to `ISearcher`/`FakeSearcher`/`App`/`.sln`.**
3. **Tests** — add `tests/AmetekWatch.Tests/SearcherLogicTests.cs` to the **existing** `AmetekWatch.Tests`
   project (no `.csproj`/`.sln` edit). Assert: `BuildQueries` includes the subject and a query for each
   focus area, and is de-duplicated/deterministic; `ToFinding` classifies a SEC/IR/earnings example as
   `FinancialReport`, an opinion/social example as `OpinionSocial`, a neutral example as `Other`, maps all
   fields, and uses the injected `discoveredAt`. Hand-computed; confirm a test can fail then revert.

## Out of scope
- The real `web_search` call, prompt caching, `ISearcher` wiring (later). Triage (011). The `.sln`/App.

## Definition of done
- [ ] `src/AmetekWatch.Core/Search/{SearchResultItem,SearchQueryBuilder,SearchResultMapper}.cs` (pure).
- [ ] `tests/AmetekWatch.Tests/SearcherLogicTests.cs` with the assertions above (can-fail confirmed).
- [ ] Gate green (build/format/test, each separately, real counts). Branch pushed; tip SHA reported.

## Deliverable / report-back
See the prompt file.
