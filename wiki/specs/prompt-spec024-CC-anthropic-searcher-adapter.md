# Prompt — Spec 024-CC — Anthropic searcher adapter (Sonnet 4.6 + web_search)

You are **CC**. Execute Spec 024-CC (`wiki/specs/024-CC-anthropic-searcher-adapter.md`). Read it first.

**SDK grounding:** **invoke the `/claude-api` skill** for exact C# bindings (the `web_search` server tool
type `WebSearchTool20260209`, `OutputConfig.Format`/`JsonOutputFormat`, `Model.ClaudeSonnet4_6`) — do not
guess. Model id is `claude-sonnet-4-6`. **Reuse the existing `IMessagesClient`** seam from 019.

## Setup
- `git fetch --prune origin`; branch from origin: `git checkout -b feature/cc-anthropic-searcher origin/main`.
- Prefix every dotnet call: `PATH="$HOME/.dotnet:$PATH" dotnet …`. Confirm `dotnet --version`.

## Steps
1. In `src/AmetekWatch.Anthropic/` add `SearchRequestFactory` (pure: Sonnet 4.6 + `web_search` tool + the
   013 `SearchQueryBuilder.BuildQueries` terms in the user message + a JSON-array structured-output schema
   of `{url,title,snippet,publishedAt,sourceDomain}`), `SearchResponseParser` (pure: JSON array →
   `IReadOnlyList<SearchResultItem>`, empty-tolerant, throws on garbage), and `AnthropicSearcher : ISearcher`
   (reuses `IMessagesClient`; injects a `DateTimeOffset` provider — **no `DateTimeOffset.Now`**; maps items
   via the real 013 `SearchResultMapper.ToFinding`). Add a short XML-doc note that the live server-tool loop
   (`pause_turn`) is not exercised offline.
2. Add tests to `tests/AmetekWatch.Anthropic.Tests/` per spec Decision 3 (fake `IMessagesClient`, hand-computed,
   **no network**); confirm a test can fail then revert.
3. Do **not** add a second client abstraction, make a live call, hardcode a key, edit the `.sln`, or touch
   other projects / the sweep host.

## The gate
Each separately, real counts: `PATH="$HOME/.dotnet:$PATH" dotnet build -c Release` ; `… dotnet format --verify-no-changes` ; `… dotnet test`.

## Versioning ritual
Internal — no `CHANGELOG`/tag, `<Version>` stays `0.1.0`. Append a dated `CLAUDE.md` → `### Unreleased`
entry (below existing ones; don't edit them).

## Report-back format
Write `wiki/reports/report-spec024-CC-anthropic-searcher-adapter.md` per `wiki/rituals/report-format.md`
(all sections; "None." where N/A), plus a gate table (real counts, before/after, can-fail, clean SHA,
`dotnet --version`), a note that the **live path is not exercised** (no key) and that the server-tool
continuation loop is a documented follow-up. Do **not** self-merge; push `feature/cc-anthropic-searcher`
and end with the tip SHA + a one-line build/format/test summary. Never print or commit secrets/keys.
