# Spec 024-CC — Anthropic searcher adapter (Sonnet 4.6 + web_search), offline-buildable

## Status
- Doc type: implementation (the real `ISearcher`, built+unit-tested offline; live call gated on a key)
- Executes: **CC**; pushes `feature/cc-anthropic-searcher`; **CX** integrates (cross-model); CM lands on PASS. No self-merge.
- Number 024 verified free (highest spec file = 023*; 024 = highest + 1). *(If 023 isn't taken, this is still the next free number — search `wiki/specs/` to confirm.)*
- Paired prompt: prompt-spec024-CC-anthropic-searcher-adapter.md
- Final on-disk: new files in `src/AmetekWatch.Anthropic/` + tests in `tests/AmetekWatch.Anthropic.Tests/` (no `.sln` change — existing projects).

## Background
019 built the real `ITriageDecider` behind the `IMessagesClient` seam. This builds the real **`ISearcher`**
the same way: Sonnet 4.6 with the server-side **`web_search`** tool, using the 013 query logic, returning a
structured JSON list of hits that the 013 `SearchResultMapper` turns into `Finding`s. Built + unit-tested
fully **offline** (fake `IMessagesClient`); only the live network call waits for `ANTHROPIC_API_KEY`.
**Reuse the existing `IMessagesClient` seam** from 019 — do not add a second client abstraction.

## Decisions made
1. **In `src/AmetekWatch.Anthropic/`** (verify SDK bindings via the **`/claude-api`** skill):
   - `SearchRequestFactory` (pure) — `Build(SweepQuery) -> MessageCreateParams`:
     `Model = Model.ClaudeSonnet4_6` (`claude-sonnet-4-6`); `Tools = [ new WebSearchTool20260209() ]`
     (optionally `MaxUses`); the user message instructs the model to search for the **013
     `SearchQueryBuilder.BuildQueries(query)`** terms and **return ONLY** a JSON array of hits, each
     `{ url, title, snippet, publishedAt (ISO-8601 or null), sourceDomain }`; constrain the final answer
     with `OutputConfig.Format = JsonOutputFormat` (a JSON-array schema of those item fields). `MaxTokens`
     generous (web results are large). Pure — no API call.
   - `SearchResponseParser` (pure) — `Parse(string json) -> IReadOnlyList<SearchResultItem>` (013's record);
     tolerant of an empty array; throws clearly on malformed JSON.
   - `AnthropicSearcher : ISearcher` — ctor `IMessagesClient` + `SearchRequestFactory` + `SearchResponseParser`
     + an injected `DateTimeOffset discoveredAt` provider (a `Func<DateTimeOffset>` or clock — **no
     `DateTimeOffset.Now` inside**). `SweepAsync` = build → `client.CreateMessageTextAsync` → parse items →
     map each via **`SearchResultMapper.ToFinding(item, discoveredAt)`** (013) → `IReadOnlyList<Finding>`.
2. **Live web_search note (documented, not tested offline):** the live server-tool loop may emit
   `pause_turn` / `stop_reason` continuations; the **offline build does not exercise this** (the fake
   returns the final JSON directly). Note in the report that the live `AnthropicMessagesClient` may need a
   continuation loop for server tools — a follow-up live-hardening concern, out of scope here.
3. **Tests** (`tests/AmetekWatch.Anthropic.Tests`): factory asserts the Sonnet-4.6 model, the `web_search`
   tool is present, the 013 query terms appear in the prompt, and the item schema; parser maps a known JSON
   array (incl. empty) to `SearchResultItem`s and throws on garbage; `AnthropicSearcher` via a
   `FakeMessagesClient` returning a canned JSON array yields the expected `Finding`s (categories from the
   **real 013 mapper**), using a fixed injected `discoveredAt`. Hand-computed; confirm a test can fail then revert.

## Out of scope
- No live API call in the gate (no key). The live server-tool continuation loop (note only). The App/DI
  real-vs-fake toggle (next spec). Prompt-cache verification.

## Definition of done
- [ ] `SearchRequestFactory` + `SearchResponseParser` + `AnthropicSearcher` in `AmetekWatch.Anthropic`,
      reusing the existing `IMessagesClient`; offline tests green; can-fail confirmed.
- [ ] Gate green (build/format/test, each separately, real counts). Branch pushed; tip SHA reported.

## Deliverable / report-back
See the prompt file.
