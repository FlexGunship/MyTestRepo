# Report — Spec 024-CC: Anthropic searcher adapter (Sonnet 4.6 + web_search)

**Headline outcome:** Built the real `ISearcher` — `AnthropicSearcher` (Sonnet 4.6 + server-side
`web_search`, 013 query logic, structured JSON hits → `Finding`s) plus the pure `SearchRequestFactory`
and `SearchResponseParser` — in `src/AmetekWatch.Anthropic`, **reusing the existing `IMessagesClient`
seam from 019** (no second client abstraction). Fully unit-tested **offline** (17 new tests, no
network); the live path waits for `ANTHROPIC_API_KEY`. Gate green (build/format/test). **Not merged —
pushed `feature/cc-anthropic-searcher` for CX to integrate; no self-merge.** `<Version>` stays `0.1.0`.

## 1. Branch / merge state
- Pre-merge `main` SHA (branch base): `a265788861821e7e777c894d309c52409a11ae2d` (origin/main).
- Feature branch: `feature/cc-anthropic-searcher`; branched from `origin/main`; not deleted.
- Working commit: single commit on `feature/cc-anthropic-searcher`; tip SHA reported in the dispatch response on push.
- Post-merge `main` SHA: N/A — not merged. Merge mechanic: pushed branch; **CX integrates** (cross-model, author ≠ integrator).

## 2. Changes
| File | Change |
| --- | --- |
| `src/AmetekWatch.Anthropic/SearchRequestFactory.cs` | **New.** Pure `Build(SweepQuery) -> MessageCreateParams`: `Model.ClaudeSonnet4_6`, `Tools = [ new WebSearchTool20260209() ]`, user message rendering the 013 `SearchQueryBuilder.BuildQueries` terms + a "return ONLY a JSON array" instruction, `OutputConfig.Format = JsonOutputFormat` with a JSON-array schema of `{url,title,snippet,publishedAt,sourceDomain}`, `MaxTokens 8192`. XML-doc note: live server-tool `pause_turn` loop not exercised offline. |
| `src/AmetekWatch.Anthropic/SearchResponseParser.cs` | **New.** Pure `Parse(string) -> IReadOnlyList<SearchResultItem>`; empty-array tolerant; throws `FormatException` on malformed JSON, non-array root, non-object element, or a missing/wrong-typed required field; null `publishedAt`/`sourceDomain` round-trip as null. |
| `src/AmetekWatch.Anthropic/AnthropicSearcher.cs` | **New.** `ISearcher` impl; ctor `IMessagesClient` (reused from 019) + `SearchRequestFactory` + `SearchResponseParser` + injected `Func<DateTimeOffset>` clock (no `DateTimeOffset.Now`). `SweepAsync` = build → `CreateMessageTextAsync` → parse → map each via the real 013 `SearchResultMapper.ToFinding`. XML-doc note on the un-exercised live server-tool loop. |
| `tests/AmetekWatch.Anthropic.Tests/SearchRequestFactoryTests.cs` | **New.** 6 facts: Sonnet-4.6 model id; `web_search` tool present (`TryPickWebSearchTool20260209`); every 013 query term appears in the prompt; prompt asks for a JSON array; five-field array schema; null-query guard. |
| `tests/AmetekWatch.Anthropic.Tests/SearchResponseParserTests.cs` | **New.** 6 facts: known array maps every field (incl. nulls); empty array → empty list; garbage throws; object-root (non-array) throws; missing required field throws; null arg throws. |
| `tests/AmetekWatch.Anthropic.Tests/AnthropicSearcherTests.cs` | **New.** 5 facts: hits → findings with **real 013 mapper** categories (sec.gov→FinancialReport, opinion-title→OpinionSocial, neutral→Other); `DiscoveredAt` from the injected clock; empty array → no findings; request is Sonnet-4.6 + carries `web_search`; ctor null guards. |
| `CLAUDE.md` | Appended a dated `### Unreleased` entry below the existing 019 entry (existing entries untouched). |

Reused unchanged: the 019 `IMessagesClient` seam and the test project's `FakeMessagesClient`; the 013
Core types `SearchQueryBuilder`, `SearchResultItem`, `SearchResultMapper`, `SweepQuery`, `Finding`,
`FindingCategory`.

## 3. SDK grounding
Invoked the `/claude-api` skill for exact C# bindings (did not guess): `Model.ClaudeSonnet4_6`
(`claude-sonnet-4-6`), server tool `new WebSearchTool20260209()` (implicitly converts to `ToolUnion`),
`OutputConfig.Format = new JsonOutputFormat { Schema = ... }`. Confirmed against the installed `Anthropic`
12.29.1 assembly that `WebSearchTool20260209` lives in `Anthropic.Models.Messages` and that `ToolUnion`
exposes `TryPickWebSearchTool20260209(out _)` (used to read the tool back in tests).

## 4. Live path NOT exercised + server-tool continuation follow-up
- **No live API call, no key.** The offline build/test never hits the network — the `FakeMessagesClient`
  returns the final JSON array directly. The real `AnthropicMessagesClient` (019) is the single untested
  line and is unchanged here; the live searcher path waits for `ANTHROPIC_API_KEY`.
- **Server-tool continuation loop is a documented follow-up.** A live `web_search` call runs a server-side
  sampling loop that can emit `pause_turn` / `stop_reason` continuations before the final answer. The
  offline build does **not** exercise this; `AnthropicMessagesClient` may need a continuation loop for
  server tools to drive the live searcher to completion. Out of scope for 024 (noted in XML-doc on both
  `SearchRequestFactory` and `AnthropicSearcher`); flagged for a live-hardening spec.

## Gate results
Run separately, prefixed `PATH="$HOME/.dotnet:$PATH"`, on Linux .NET **8.0.422**.

| Gate command | Result | Notes |
| --- | --- | --- |
| `dotnet build -c Release` | ✓ | 0 warnings, 0 errors |
| `dotnet format --verify-no-changes` | ✓ | exit 0, no diffs |
| `dotnet test` | ✓ | **69/69 passed** (was 52) |

- **Test count before → after:** 52 → 69 solution-wide (**+17**). Per assembly:
  `AmetekWatch.Anthropic.Tests` 13 → **30** (+17); `AmetekWatch.Tests` 33; `AmetekWatch.Storage.Tests` 4;
  `AmetekWatch.Web.Tests` 2 — all unchanged.
- **Can-fail confirmed:** flipped the OpinionSocial category oracle in `AnthropicSearcherTests` to `Other`
  → `Failed: 1, Passed: 29` in `AmetekWatch.Anthropic.Tests`; reverted → 30/30 green.
- **SHA the gate ran clean at:** the single commit on `feature/cc-anthropic-searcher` (tip SHA in the dispatch response); working tree clean post-commit.
- **Files changed NOT in the spec's list:** `CLAUDE.md` only — the required versioning-ritual `### Unreleased`
  entry (the prompt explicitly asks for it).

## Sources beyond the brief / surprises
- The spec/prompt-spec files `024-CC-…` and `prompt-spec024-CC-…` were **not in the feature-branch worktree**
  but exist on `origin/main` (`wiki/specs/`). Read them from `origin/main`; content matches the dispatch
  prompt's "Key points" exactly. No deviation.
- The installed `Anthropic` package resolves the `net9.0` ref assembly for inspection but the projects target
  `net8.0` and build clean — no action needed.

## Deferred / not done
- Live `web_search` API call and the server-tool continuation loop — deferred by design (no key; documented
  follow-up, see §4).
- App/DI real-vs-fake `ISearcher` toggle — explicitly out of scope (next spec).
- Prompt-cache verification — out of scope.

## Standing flags
None.

## Roles update notice
No role doc edited this session.

---

**Branch tip & summary:** `feature/cc-anthropic-searcher` — build ✓ (0 warn) / format ✓ /
test ✓ 69/69 (+17). Tip SHA reported in the dispatch response.
