# Report — Spec 013-CC: Searcher query & result-mapping logic (pure, no API)

**Headline outcome:** Built the searcher tier's pure logic behind the existing seam — query
construction + result→`Finding` mapping with a documented, constant-driven category heuristic, plus
11 hand-computed tests. **Not merged** (CX integrates, cross-model; CM lands on PASS — no self-merge).
No version bump (`<Version>` stays `0.1.0`). Gate green on .NET 8.0.422 (build 0 warn / format clean /
test 25/25). Branch `feature/cc-searcher-logic` pushed.

## 1. Branch / merge state
- Pre-merge `main` SHA (branch base): `e70f1efac563b6e941d7b8cccf24b304e4a6913f` (origin/main)
- Feature branch: `feature/cc-searcher-logic`; working commit: see tip SHA at end of message;
  branch deleted post-merge: n (not merged)
- Post-merge `main` SHA (pushed): N/A — not merged (cross-model integrator merges)
- Merge mechanic: pushed branch; **CX** integrates (author ≠ integrator). No self-merge.

## 2. Changes
| File | Description |
|------|-------------|
| `src/AmetekWatch.Core/Search/SearchResultItem.cs` | **New.** Record for one raw search hit: `Url`, `Title`, `Snippet`, `DateTimeOffset? PublishedAt`, `string? SourceDomain`. |
| `src/AmetekWatch.Core/Search/SearchQueryBuilder.cs` | **New.** Static `BuildQueries(SweepQuery)` → ordered, ordinally de-duplicated query strings: general subject query + one per focus area (opinion/social, financial reports). Pure, deterministic; trims subject. |
| `src/AmetekWatch.Core/Search/SearchResultMapper.cs` | **New.** Static `ToFinding(SearchResultItem, DateTimeOffset discoveredAt)` → `Finding`. Documented category heuristic driven by explicit commented constant lists; injects `discoveredAt` (no clock); null-guards the item. |
| `tests/AmetekWatch.Tests/SearcherLogicTests.cs` | **New.** 11 hand-computed tests over the two pure types. Appended to the existing project — no `.csproj`/`.sln` edit. |
| `CLAUDE.md` | Appended one dated `### Unreleased` entry (below existing; none edited). |

## 3. Spec-specific detail

### Decision 1 — the three Search/ types
- **`SearchResultItem`** — exactly the fields the spec named, as a `sealed record`, with XML docs.
- **`SearchQueryBuilder.BuildQueries`** — returns three queries in fixed order for a normal subject:
  1. `"<subject>"` (general)
  2. `"<subject> opinion OR commentary OR social sentiment"` (opinion/social focus area)
  3. `"<subject> earnings OR financial report OR SEC filing"` (financial-report focus area)
  Subject is trimmed; output is de-duplicated first-seen (ordinal `HashSet`) so a degenerate subject
  cannot emit duplicates. Pure/deterministic; null `query` throws `ArgumentNullException`.
- **`SearchResultMapper.ToFinding`** — category heuristic (first match wins), driven by explicit
  commented constant arrays so tests pin the boundaries:
  - `FinancialReport` ⇐ domain contains `sec.gov` / `edgar` / `investor.` / `ir.`, **or** title contains
    `earnings` / `10-q` / `10-k` / `annual report`.
  - `OpinionSocial` ⇐ title contains `op-ed` / `opinion` / `blog`, **or** domain contains a known social
    host (`twitter.com`, `x.com`, `reddit.com`, `facebook.com`, `linkedin.com`, `medium.com`).
  - `Other` ⇐ otherwise.
  Domain signal reads `SourceDomain` when present, else falls back to `Url`. Matching is
  case-insensitive (lower-invariant, ordinal `Contains`). `discoveredAt` is injected and copied through;
  there is **no** `DateTimeOffset.Now` anywhere in the file.

### Decision 3 — tests (11, all hand-computed)
- `BuildQueries_CoversSubjectAndBothFocusAreas_InFixedOrder` — exact expected 3-string list; subject is
  query[0]; one query carries the opinion focus, one the financial focus.
- `BuildQueries_IsDeduplicatedAndDeterministic` — `Distinct().Count() == Count`; two calls equal.
- `BuildQueries_TrimsSubject` — leading/trailing whitespace stripped.
- `ToFinding_ClassifiesSecDomain_AsFinancialReport`, `…InvestorRelationsDomain…`,
  `…EarningsTitle…` — the three FinancialReport paths (SEC domain, IR domain, earnings title).
- `ToFinding_ClassifiesOpinionTitle_AsOpinionSocial`, `…SocialDomain…` — both OpinionSocial paths.
- `ToFinding_ClassifiesNeutralItem_AsOther` — no signal ⇒ Other.
- `ToFinding_MapsAllFields_AndUsesInjectedDiscoveredAt` — every field copied; `DiscoveredAt` equals the
  injected instant (proves no clock).
- `ToFinding_NullItem_Throws` — `ArgumentNullException`.

## Gate results
Each command run **separately** (never chained), prefixed `PATH="$HOME/.dotnet:$PATH"`.

| Gate command | Result | Counts |
|--------------|--------|--------|
| `dotnet build -c Release` | ✓ | 0 Warning(s), 0 Error(s) |
| `dotnet format --verify-no-changes` | ✓ | exit 0 (clean) |
| `dotnet test` | ✓ | 25/25 passed, 0 failed, 0 skipped |

- `dotnet --version`: **8.0.422**
- Test count in `AmetekWatch.Tests`: **before 7** (SweepRunner slice) → **after 18** (7 + 11 searcher).
  Whole-suite total: **before 14** (7 + 4 storage + 3 web) → **after 25**.
- **Can-fail check:** flipped `ToFinding_ClassifiesNeutralItem_AsOther`'s expectation to
  `FinancialReport` → 1 failed / 10 passed; reverted → 11/11 pass.
- Clean SHA at which the gate ran: the pushed tip (see end of message).
- Files changed NOT in the spec's files-to-change list: **None.** (Spec named the three Search/ files,
  the new test file, and the `CLAUDE.md` Unreleased entry — all and only those were touched.)

## Sources beyond the brief / surprises
None. The query suffix wording and the exact constant signal lists are my deterministic choices within
the spec's "explicit constant lists" mandate; they are pinned by the tests.

## Deferred / not done
- The real `web_search` API call, prompt caching, and `ISearcher`/`FakeSearcher` wiring — explicitly out
  of scope (auth deferred to a later spec). Nothing in this spec was deferred.
- Windows `dist/ametek-watch.exe` not produced (artifact, not part of the gate; this spec adds only
  library + test code).

## Standing flags
None new. (Pre-existing project-wide: Anthropic API auth still deferred; Windows runtime verification of
the published exe still deferred — both untouched here.)

## Roles update notice
None — no role doc edited this session.
