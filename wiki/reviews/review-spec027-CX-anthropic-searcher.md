# PASS — Spec 027-CX integration review for CC spec-024 Anthropic searcher adapter

## Branch State
- Integration branch: `feature/cx-integrate-024`
- Code-under-review / clean-gate SHA on `feature/cx-integrate-024`: `f5c75b8a34ba4164ba107d713799e361c714612c`
- Reviewed source branch SHA, `origin/feature/cc-anthropic-searcher`: `f5c75b8a34ba4164ba107d713799e361c714612c`
- Dotnet SDK: `8.0.422`
- Scope check: `.sln`, app host, sweep host, web/storage/core projects, and other test projects are untouched in the reviewed diff. Diff adds only Anthropic searcher files/tests plus CC docs/report.

## Gate Table
| Gate | Command | Result | Real Counts |
| --- | --- | --- | --- |
| Build | `PATH="$HOME/.dotnet:$PATH" dotnet build -c Release` | PASS | 9 projects built; 0 warnings; 0 errors |
| Format | `PATH="$HOME/.dotnet:$PATH" dotnet format --verify-no-changes` | PASS | exit 0; no formatting changes |
| Test | `PATH="$HOME/.dotnet:$PATH" dotnet test` | PASS | 69 total; 69 passed; 0 failed; 0 skipped |
| Can-fail | Temporarily inverted `SearchRequestFactoryTests.Build_PinsSonnet46ModelId`, ran `PATH="$HOME/.dotnet:$PATH" dotnet test`, reverted, reran tests | PASS | Inverted run failed as expected: 1 failed, 68 passed total (`AmetekWatch.Anthropic.Tests`: 1 failed, 29 passed). Reverted run: 69 passed, 0 failed |

## Correctness Checks
| Check | Status | Evidence |
| --- | --- | --- |
| Real SDK request pins Sonnet 4.6 | OK | `SearchRequestFactory.Build` sets `Model = Model.ClaudeSonnet4_6` at `src/AmetekWatch.Anthropic/SearchRequestFactory.cs:44-47`; test asserts `claude-sonnet-4-6` at `tests/AmetekWatch.Anthropic.Tests/SearchRequestFactoryTests.cs:18-23`. |
| Request includes server-side `web_search` tool | OK | `Tools = new List<ToolUnion> { new WebSearchTool20260209() }` at `src/AmetekWatch.Anthropic/SearchRequestFactory.cs:44-49`; test reads it back with `TryPickWebSearchTool20260209` at `tests/AmetekWatch.Anthropic.Tests/SearchRequestFactoryTests.cs:26-32`. |
| Prompt carries 013 `SearchQueryBuilder.BuildQueries` terms | OK | Request factory loops over `SearchQueryBuilder.BuildQueries(query)` into the user prompt at `src/AmetekWatch.Anthropic/SearchRequestFactory.cs:68-77`; 013 builder emits subject, opinion/social, and financial query terms at `src/AmetekWatch.Core/Search/SearchQueryBuilder.cs:27-39`; test asserts every generated query appears at `tests/AmetekWatch.Anthropic.Tests/SearchRequestFactoryTests.cs:35-48`. |
| Structured output schema is JSON array of `{url,title,snippet,publishedAt,sourceDomain}` | OK | `OutputConfig.Format = new JsonOutputFormat { Schema = BuildSchema() }` at `src/AmetekWatch.Anthropic/SearchRequestFactory.cs:57-60`; schema type is array and item properties define all five fields at `src/AmetekWatch.Anthropic/SearchRequestFactory.cs:95-111`; test covers array type and five fields at `tests/AmetekWatch.Anthropic.Tests/SearchRequestFactoryTests.cs:63-76`. |
| Parser maps JSON array to `SearchResultItem` and tolerates empty arrays | OK | Parser requires array root, iterates objects, and constructs `SearchResultItem` at `src/AmetekWatch.Anthropic/SearchResponseParser.cs:37-58`; known-array and empty-array tests at `tests/AmetekWatch.Anthropic.Tests/SearchResponseParserTests.cs:31-56`. |
| Parser throws on malformed/wrong-shaped required data | OK | Invalid JSON, non-array root, non-object element, missing string fields, wrong nullable types, and bad dates throw `FormatException` at `src/AmetekWatch.Anthropic/SearchResponseParser.cs:26-40`, `src/AmetekWatch.Anthropic/SearchResponseParser.cs:45-55`, and `src/AmetekWatch.Anthropic/SearchResponseParser.cs:61-109`; tests cover garbage, object root, and missing required title at `tests/AmetekWatch.Anthropic.Tests/SearchResponseParserTests.cs:59-76`. |
| `AnthropicSearcher` reuses existing `IMessagesClient`; no second client abstraction | OK | Constructor takes `IMessagesClient` directly at `src/AmetekWatch.Anthropic/AnthropicSearcher.cs:25-41`; the existing interface is `src/AmetekWatch.Anthropic/IMessagesClient.cs:13-19`; the call path uses `_client.CreateMessageTextAsync` at `src/AmetekWatch.Anthropic/AnthropicSearcher.cs:58-60`. |
| Clock is injected; no `DateTimeOffset.Now`/`UtcNow` in the searcher | OK | Constructor requires `Func<DateTimeOffset> clock` at `src/AmetekWatch.Anthropic/AnthropicSearcher.cs:37-51`; sweep captures `_clock()` once at `src/AmetekWatch.Anthropic/AnthropicSearcher.cs:62-68`; test fixes `FixedNow` and asserts `DiscoveredAt` at `tests/AmetekWatch.Anthropic.Tests/AnthropicSearcherTests.cs:15-16` and `tests/AmetekWatch.Anthropic.Tests/AnthropicSearcherTests.cs:70-76`. |
| Items map through real 013 `SearchResultMapper.ToFinding`; categories are the 013 heuristic | OK | Searcher calls `SearchResultMapper.ToFinding(item, discoveredAt)` at `src/AmetekWatch.Anthropic/AnthropicSearcher.cs:64-68`; mapper heuristic is defined in `src/AmetekWatch.Core/Search/SearchResultMapper.cs:78-123`; tests exercise FinancialReport, OpinionSocial, and Other categories at `tests/AmetekWatch.Anthropic.Tests/AnthropicSearcherTests.cs:54-67`. |
| Tests use fake client; no network | OK | Searcher tests construct `FakeMessagesClient` at `tests/AmetekWatch.Anthropic.Tests/AnthropicSearcherTests.cs:48-52` and `tests/AmetekWatch.Anthropic.Tests/AnthropicSearcherTests.cs:91-93`; fake records the request and returns canned JSON only at `tests/AmetekWatch.Anthropic.Tests/FakeMessagesClient.cs:11-23`. |
| Live `pause_turn` continuation is documented as deferred | OK | Deferred server-tool continuation is documented in `src/AmetekWatch.Anthropic/SearchRequestFactory.cs:23-28` and `src/AmetekWatch.Anthropic/AnthropicSearcher.cs:17-22`; acceptable for this review. |

## No-Secret / No-Live Confirmation
- Diff scan found no hardcoded Anthropic token pattern, no `x-api-key` assignment, no direct Anthropic API URL, and no new `HttpClient` live-call path.
- Benign matches reviewed: docs mention the env-var name `ANTHROPIC_API_KEY`; tests contain fixture `https://...` URLs and prompt/tool assertions; the existing `AnthropicMessagesClient` remains the single SDK wrapper and is reused, not duplicated.
- No secret/token was printed or committed.

## HOLD Blockers
None.

VERDICT: PASS
