# Spec 020-CC2 — Refine searcher category heuristic (social precedence)

## Status
- Doc type: implementation (pure refinement of 013's category mapper)
- Executes: **CC2**; pushes `feature/cc2-searcher-heuristic`; **CX2** integrates (cross-model); CM lands on PASS. No self-merge.
- Number 020 verified free (highest spec file = 019; 020 = 019 + 1).
- Paired prompt: prompt-spec020-CC2-searcher-heuristic-refine.md
- Final on-disk: `src/AmetekWatch.Core/Search/SearchResultMapper.cs` (edited) + a test added to `tests/AmetekWatch.Tests/`.

## Background
CX's 014 review noted a documented edge in `SearchResultMapper` (013): a social post whose **title**
contains a financial signal (e.g. a LinkedIn post titled "AMETEK earnings reaction") classifies as
`FinancialReport` because the financial-**title** check precedes the social-**domain** check. For a tool
that gives **special weight to personal/social opinion pieces**, a known social source should win — that
post is opinion/social commentary *about* earnings, not an institutional financial report.

## Decisions made
1. **Reorder/refine the heuristic** so a **known social/opinion source domain** classifies as
   `OpinionSocial` even when the title carries a financial signal. Precedence (first match wins),
   documented in comments:
   1. **`FinancialReport`** when the **source domain** is an institutional financial/IR/regulator domain
      (SEC/EDGAR, investor-relations, recognized financial press) — domain signal is authoritative.
   2. **`OpinionSocial`** when the source domain is a known social/opinion domain **or** the title signals
      op-ed/opinion/blog/personal commentary.
   3. **`FinancialReport`** when the **title** names earnings/10-Q/10-K/annual report (financial title from a
      non-social, non-IR source — e.g. a news article reporting results).
   4. else **`Other`**.
   Keep the constant lists explicit and commented. Do not change `SearchQueryBuilder`, `SearchResultItem`,
   the mapper's public signature, or the injected-`discoveredAt` purity.
2. **Tests** — add cases to `tests/AmetekWatch.Tests/` (new file or extend `SearcherLogicTests.cs`): the
   flagged case (social domain + earnings-signal title → **`OpinionSocial`**); an IR/SEC domain with any
   title → `FinancialReport`; a plain news article titled "AMETEK Q2 earnings" (non-social, non-IR) →
   `FinancialReport`; a neutral item → `Other`. Hand-computed; confirm a test can fail then revert. Update
   any existing 013 test whose expectation legitimately changes under the new precedence (and say so).

## Out of scope
- Any non-Search source; the Anthropic adapters (019); `.sln`/project changes.

## Definition of done
- [ ] `SearchResultMapper` precedence refined + documented; signature/purity unchanged.
- [ ] Tests cover the flagged case + regressions; can-fail confirmed.
- [ ] Gate green (build/format/test, each separately, real counts). Branch pushed; tip SHA reported.

## Deliverable / report-back
See the prompt file.
