# Report — Spec 020-CC2: Refine searcher category heuristic (social precedence)

**Headline outcome:** Not merged (no self-merge — CX2 integrates). Refined `SearchResultMapper`
category precedence so a known social/opinion source domain wins over a financial-title signal;
4 tests added; **no existing 013 test expectation changed**. No version bump (`<Version>` stays
`0.1.0`). Branch `feature/cc2-searcher-heuristic` pushed; gate green (build 0-warn, format clean,
test 39/39).

## 1. Branch / merge state
- Pre-merge `main` SHA: `e37f99b6288b2d654b0b46587b26a8b3a5219256` (origin/main at branch time).
- Feature branch: `feature/cc2-searcher-heuristic`; working commit: see final summary (tip SHA);
  branch deleted post-merge: n (CX2 integrates).
- Post-merge `main` SHA (pushed): N/A — not merged (cross-model integration pending).
- Merge mechanic: pushed branch; integrator (CX2) merges. No self-merge.

## 2. Changes
| File | Change |
| --- | --- |
| `src/AmetekWatch.Core/Search/SearchResultMapper.cs` | Reordered `Classify` to the refined 4-rule precedence (financial DOMAIN → social DOMAIN/opinion TITLE → financial TITLE → Other); updated the class XML-doc `<remarks>` heuristic list and the inline step comments. Public signature, the explicit commented constant lists (contents unchanged), and the injected-`discoveredAt` purity untouched. |
| `tests/AmetekWatch.Tests/SearcherLogicTests.cs` | Added a "refined social-precedence heuristic (spec 020)" section with 4 hand-computed tests. No existing test edited. |
| `CLAUDE.md` | Appended one dated `### Unreleased` entry (below existing; none edited). |
| `wiki/reports/report-spec020-CC2-searcher-heuristic-refine.md` | This report. |

## 3. Refined precedence (spec Decision 1)
First match wins, documented in `Classify` comments:
1. **`FinancialReport`** — institutional/regulator/IR source **domain** (`sec.gov`, `edgar`,
   `investor.`, `ir.`) is authoritative regardless of title.
2. **`OpinionSocial`** — known social/blogging **domain** (`twitter.com`, `x.com`, `reddit.com`,
   `facebook.com`, `linkedin.com`, `medium.com`) **or** opinion/op-ed/blog **title**. This is the
   behavioral change: a social domain now wins over a financial-title signal.
3. **`FinancialReport`** — financial-report **title** (`earnings`, `10-q`, `10-k`, `annual report`)
   from a non-social, non-IR source.
4. **`Other`** — no recognised signal.

The constant lists (`FinancialDomainSignals`, `FinancialTitleSignals`, `OpinionTitleSignals`,
`SocialDomainSignals`) are unchanged in contents — only the order in which they are consulted
changed (financial-DOMAIN and financial-TITLE checks split into separate rules so the social-DOMAIN
check can sit between them).

## 4. Tests added (spec Decision 2)
All in `tests/AmetekWatch.Tests/SearcherLogicTests.cs`, hand-computed:
- `ToFinding_ClassifiesSocialDomainWithEarningsTitle_AsOpinionSocial` — the flagged 013 edge:
  `linkedin.com` + title "AMETEK earnings reaction…" → **`OpinionSocial`** (rule 2 over rule 3).
- `ToFinding_ClassifiesIrDomainWithOpinionTitle_AsFinancialReport` — `ir.ametek.com` + an
  opinion/blog title → **`FinancialReport`** (rule 1 authoritative domain).
- `ToFinding_ClassifiesPlainNewsEarningsTitle_AsFinancialReport` — `news.example.com` (non-social,
  non-IR) + "AMETEK Q2 earnings" → **`FinancialReport`** (falls through to rule 3).
- `ToFinding_ClassifiesNeutralItem_AsOther_UnderRefinedPrecedence` — no signal → **`Other`** (rule 4).

**Can-fail:** flipped the flagged test's oracle to `FinancialReport`; `dotnet test --filter` →
1 failed (`Assert.Equal() Failure: Values differ`). Reverted; full suite green again.

## 5. Existing 013 tests whose expectations changed
**None.** All eight prior `SearcherLogicTests` cases hold unchanged under the new precedence:
SEC/IR domains hit rule 1; the `news.example.com` earnings-title case (`ToFinding_ClassifiesEarningsTitle_AsFinancialReport`)
now resolves via rule 3 (same result, `FinancialReport`); opinion-title and reddit cases hit rule 2;
neutral → Other; the field-mapping case (`ir.ametek.com` + "AMETEK Q2 earnings") resolves via rule 1
(domain) instead of the old combined check — same expected `FinancialReport`. No edits to any 013 test.

## Gate results
| Command | Result | Counts |
| --- | --- | --- |
| `PATH="$HOME/.dotnet:$PATH" dotnet build -c Release` | ✓ | 0 warning, 0 error |
| `PATH="$HOME/.dotnet:$PATH" dotnet format --verify-no-changes` | ✓ | exit 0 (clean) |
| `PATH="$HOME/.dotnet:$PATH" dotnet test` | ✓ | 39 passed / 0 failed |

- Test count **before**: 35 (AmetekWatch.Tests 29 + Storage 4 + Web 2).
- Test count **after**: 39 (AmetekWatch.Tests **33** + Storage 4 + Web 2) — +4 from this spec.
- Can-fail confirmed (see §4) and reverted.
- Clean gate SHA: the pushed tip of `feature/cc2-searcher-heuristic` (see final summary).
- `dotnet --version`: **8.0.422**.
- Files changed NOT in the spec's files-to-change list: `CLAUDE.md` (versioning ritual) and this
  report — both expected/required by the prompt, not product code.

## Sources beyond the brief / surprises
The CLAUDE.md 015 changelog narrative cites a 36-test total and "3 web", but origin/main's actual
state is 35 (Web.Tests 3→2 was reduced by the later web-on-SQLite spec recorded in the final
Unreleased entry). I report the **real** counts from the runner: 35 → 39. No other surprises.

## Deferred / not done
None. (Windows runtime verification remains project-wide deferred per charter; not in scope here —
this is a pure-logic Core change with no runtime/publish step.)

## Standing flags
None new. Real Anthropic SDK wiring (spec 019) remains the outstanding deferred work project-wide;
untouched here.

## Roles update notice
No role doc edited this session.
