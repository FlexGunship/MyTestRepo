# Spec 027-CX — Integrate CC's spec-024 Anthropic searcher adapter

> Self-contained integration spec (no separate prompt). Same shape as [`022-CX`](022-CX-integrate-anthropic-triage.md).

## Status
- Doc type: integration (cross-model gate + review). Executes: **CX**; CX issues VERDICT; **CM lands on PASS**.
- Number 027 verified free (highest spec file = 026; 027 = 026 + 1).
- Reviewing: CC's `feature/cc-anthropic-searcher` (origin tip `f5c75b8`). Author CC (Claude) ≠ integrator CX (Codex).
- Final on-disk: `wiki/reviews/review-spec027-CX-anthropic-searcher.md`; on PASS, lands to `main`.

## What to do
1. `git fetch --prune origin`; `git checkout -b feature/cx-integrate-024 origin/feature/cc-anthropic-searcher`.
   Prefix every dotnet call: `PATH="$HOME/.dotnet:$PATH" dotnet …`.
2. Gate, each separately, real counts: `dotnet build -c Release`; `dotnet format --verify-no-changes`;
   `dotnet test`. Confirm a test can fail (invert one assertion, observe, revert).
3. Adversarial review of `src/AmetekWatch.Anthropic/{SearchRequestFactory,SearchResponseParser,AnthropicSearcher}.cs`
   + tests against spec 024, citing `file:line`:
   - `SearchRequestFactory` builds a `claude-sonnet-4-6` request with the **`web_search`** tool, the 013
     `SearchQueryBuilder.BuildQueries` terms in the prompt, and a JSON-array structured-output schema of
     `{url,title,snippet,publishedAt,sourceDomain}`.
   - `SearchResponseParser` maps a JSON array → `SearchResultItem`s (empty-tolerant), throws on malformed.
   - `AnthropicSearcher` **reuses the existing `IMessagesClient`** (no second client abstraction), injects a
     clock (**no `DateTimeOffset.Now`**), and maps each item via the **real 013 `SearchResultMapper.ToFinding`**
     (so categories are the 013 heuristic's). Tests use a fake client — **no network**.
   - **No live call / no hardcoded key** (grep the diff — confirm none). The live server-tool `pause_turn`
     continuation is documented-as-deferred (acceptable); note it in your review. `App`/sweep host and other
     projects untouched; `.sln` untouched (existing projects only).
4. Write `wiki/reviews/review-spec027-CX-anthropic-searcher.md`: PASS/HOLD headline; branch SHAs; gate table
   (real counts + can-fail + clean SHA + `dotnet --version`); correctness checks (`file:line`); a no-secret
   confirmation; HOLD blockers or "None."; final line exactly `VERDICT: PASS` or `VERDICT: HOLD`. Commit on
   `feature/cx-integrate-024`, push. **Do not merge to main.**

## Definition of done
- [ ] Gate re-run (real counts, can-fail); adversarial review with `file:line`; secret-scan clean.
- [ ] Review ends `VERDICT: PASS`/`HOLD`; branch pushed; tip SHA + verdict reported.

## Deliverable / report-back
The review file; end your final message with the `feature/cx-integrate-024` tip SHA + verdict.
