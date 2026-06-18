PASS: spec-020 searcher category heuristic refinement preserves financial source authority while giving social domains precedence over financial-title signals.

## Branch state

- Integration branch: feature/cx2-integrate-020
- Clean reviewed branch SHA before this review file: 17776b6d67b992e9a4bc0910cd06b7464012d2ce
- Reviewed origin/feature/cc2-searcher-heuristic SHA: 17776b6d67b992e9a4bc0910cd06b7464012d2ce
- dotnet --version: 8.0.422

## Gates

| Gate | Command | Result | Counts |
| --- | --- | --- | --- |
| Build | `PATH="$HOME/.dotnet:$PATH" dotnet build -c Release` | PASS | 7 projects built; 0 warnings; 0 errors |
| Format | `PATH="$HOME/.dotnet:$PATH" dotnet format --verify-no-changes` | PASS | exit 0; 0 files changed |
| Test | `PATH="$HOME/.dotnet:$PATH" dotnet test` | PASS | 39 passed; 0 failed; 0 skipped |
| Can-fail probe | Temporarily inverted one mapper assertion in `ToFinding_ClassifiesOpinionTitle_AsOpinionSocial`, ran `PATH="$HOME/.dotnet:$PATH" dotnet test`, reverted, reran green | PASS | Probe failed as expected: 38 passed; 1 failed; 0 skipped. Failure was expected `FinancialReport`, actual `OpinionSocial` at `tests/AmetekWatch.Tests/SearcherLogicTests.cs:116`. Re-run after revert: 39 passed; 0 failed; 0 skipped |

## Correctness checks

| Check | Result | Evidence |
| --- | --- | --- |
| Rule 1 precedence is IR/SEC domain to `FinancialReport` | ok | `Classify` normalizes domain/title at `src/AmetekWatch.Core/Search/SearchResultMapper.cs:95` and `src/AmetekWatch.Core/Search/SearchResultMapper.cs:96`, then checks `FinancialDomainSignals` before every other signal at `src/AmetekWatch.Core/Search/SearchResultMapper.cs:99` and returns `FinancialReport` at `src/AmetekWatch.Core/Search/SearchResultMapper.cs:103`. SEC and IR coverage exists at `tests/AmetekWatch.Tests/SearcherLogicTests.cs:67` and `tests/AmetekWatch.Tests/SearcherLogicTests.cs:80`. |
| Rule 2 precedence is social domain OR opinion title to `OpinionSocial` | ok | The social-domain/opinion-title check is second at `src/AmetekWatch.Core/Search/SearchResultMapper.cs:106`, evaluates `ContainsAny(domain, SocialDomainSignals) || ContainsAny(title, OpinionTitleSignals)` at `src/AmetekWatch.Core/Search/SearchResultMapper.cs:110`, and returns `OpinionSocial` at `src/AmetekWatch.Core/Search/SearchResultMapper.cs:112`. Tests cover opinion title at `tests/AmetekWatch.Tests/SearcherLogicTests.cs:107` and social domain at `tests/AmetekWatch.Tests/SearcherLogicTests.cs:120`. |
| Rule 3 is financial title from non-social/non-IR source to `FinancialReport` | ok | Financial-title matching happens only after the social/opinion branch at `src/AmetekWatch.Core/Search/SearchResultMapper.cs:115` and returns `FinancialReport` at `src/AmetekWatch.Core/Search/SearchResultMapper.cs:119`. Plain news earnings coverage is at `tests/AmetekWatch.Tests/SearcherLogicTests.cs:181`. |
| Rule 4 fallback is `Other` | ok | No recognized signal falls through to `Other` at `src/AmetekWatch.Core/Search/SearchResultMapper.cs:122` and `src/AmetekWatch.Core/Search/SearchResultMapper.cs:123`. Neutral coverage exists at `tests/AmetekWatch.Tests/SearcherLogicTests.cs:134` and `tests/AmetekWatch.Tests/SearcherLogicTests.cs:196`. |
| FLAGGED social-domain plus earnings-title case yields `OpinionSocial` and is tested | ok | The flagged edge is explicitly named at `tests/AmetekWatch.Tests/SearcherLogicTests.cs:152`, uses `linkedin.com` at `tests/AmetekWatch.Tests/SearcherLogicTests.cs:160`, and asserts `OpinionSocial` at `tests/AmetekWatch.Tests/SearcherLogicTests.cs:162`. This follows rule 2 before rule 3 at `src/AmetekWatch.Core/Search/SearchResultMapper.cs:110`. |
| Plain news article titled `AMETEK Q2 earnings` still yields `FinancialReport` | ok | The plain news test uses `news.example.com` at `tests/AmetekWatch.Tests/SearcherLogicTests.cs:190`, title `AMETEK Q2 earnings` at `tests/AmetekWatch.Tests/SearcherLogicTests.cs:187`, and asserts `FinancialReport` at `tests/AmetekWatch.Tests/SearcherLogicTests.cs:192`. |
| IR/SEC domain still yields `FinancialReport`, including opinion-title conflict | ok | The IR conflict test uses `ir.ametek.com` at `tests/AmetekWatch.Tests/SearcherLogicTests.cs:175`, an opinion/blog title at `tests/AmetekWatch.Tests/SearcherLogicTests.cs:172`, and asserts `FinancialReport` at `tests/AmetekWatch.Tests/SearcherLogicTests.cs:177`. |
| Public signature unchanged | ok | `ToFinding` remains `public static Finding ToFinding(SearchResultItem item, DateTimeOffset discoveredAt)` at `src/AmetekWatch.Core/Search/SearchResultMapper.cs:78`. |
| Explicit constant lists unchanged in shape and visible in code | ok | `FinancialDomainSignals` starts at `src/AmetekWatch.Core/Search/SearchResultMapper.cs:34`, `FinancialTitleSignals` at `src/AmetekWatch.Core/Search/SearchResultMapper.cs:43`, `OpinionTitleSignals` at `src/AmetekWatch.Core/Search/SearchResultMapper.cs:53`, and `SocialDomainSignals` at `src/AmetekWatch.Core/Search/SearchResultMapper.cs:61`. |
| Injected-`discoveredAt` purity unchanged | ok | Docs state no I/O or clock and caller-injected `discoveredAt` at `src/AmetekWatch.Core/Search/SearchResultMapper.cs:7` and `src/AmetekWatch.Core/Search/SearchResultMapper.cs:8`; `ToFinding` copies the injected value directly into `Finding.DiscoveredAt` at `src/AmetekWatch.Core/Search/SearchResultMapper.cs:90`; the field-mapping test asserts the injected value at `tests/AmetekWatch.Tests/SearcherLogicTests.cs:229`. |
| Existing 013 expectations were not weakened | ok | The diff appends the spec-020 block starting at `tests/AmetekWatch.Tests/SearcherLogicTests.cs:147`; the prior SEC, IR, earnings-title, opinion-title, social-domain, neutral, field-map, and null tests remain with their original expectations at `tests/AmetekWatch.Tests/SearcherLogicTests.cs:67`, `tests/AmetekWatch.Tests/SearcherLogicTests.cs:80`, `tests/AmetekWatch.Tests/SearcherLogicTests.cs:93`, `tests/AmetekWatch.Tests/SearcherLogicTests.cs:107`, `tests/AmetekWatch.Tests/SearcherLogicTests.cs:120`, `tests/AmetekWatch.Tests/SearcherLogicTests.cs:134`, `tests/AmetekWatch.Tests/SearcherLogicTests.cs:212`, and `tests/AmetekWatch.Tests/SearcherLogicTests.cs:233`. CC2's report also states no existing 013 expectation changed at `wiki/reports/report-spec020-CC2-searcher-heuristic-refine.md:53`. |
| Adversarial borderline: social host with financial filing title is social | ok | For an input like `SourceDomain = "x.com"` and title `AMETEK files 10-K`, rule 1 misses unless the domain has financial-domain text, rule 2 matches `x.com` via `SocialDomainSignals` at `src/AmetekWatch.Core/Search/SearchResultMapper.cs:64`, and rule 3 is not reached because rule 2 returns at `src/AmetekWatch.Core/Search/SearchResultMapper.cs:112`. |
| Adversarial borderline: missing `SourceDomain` can still classify SEC URL as financial | ok | `domain` falls back to `item.Url` when `SourceDomain` is null at `src/AmetekWatch.Core/Search/SearchResultMapper.cs:95`, so an SEC URL with an opinion title still matches `sec.gov` in `FinancialDomainSignals` at `src/AmetekWatch.Core/Search/SearchResultMapper.cs:36` before opinion-title handling. |

## HOLD blockers

None.

VERDICT: PASS
