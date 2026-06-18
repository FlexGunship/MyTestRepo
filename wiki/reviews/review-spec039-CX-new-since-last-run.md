# PASS - spec 038 new-since-last-run digest review

## Branch state

| Item | Value |
| --- | --- |
| Review branch | `feature/cx-integrate-038` |
| Review branch SHA before review commit | `186b716719eca402c98ff9464e4784330986dc00` |
| Reviewed source branch | `origin/feature/cc2-new-since-last-run` |
| Reviewed source branch SHA | `186b716719eca402c98ff9464e4784330986dc00` |
| Scope check | App, Anthropic, and `.sln` files untouched; changes are Core runner, Core tests, and docs (`CLAUDE.md`, wiki report). |

## Gate table

`PATH="$HOME/.dotnet:$PATH"` used for all dotnet commands. `dotnet --version`: `8.0.422`.

| Gate | Result | Real counts |
| --- | --- | --- |
| `dotnet build -c Release` | PASS | 0 warnings, 0 errors |
| `dotnet format --verify-no-changes` | PASS | clean, no output |
| `dotnet test` | PASS | 97 passed, 0 failed, 0 skipped, 97 total (`AmetekWatch.Tests` 61, Storage 4, Anthropic 30, Web 2) |
| Can-fail confirmation | PASS | Temporarily inverted `SweepRunnerOnlyNewTests.cs:54` to expect known `UrlB` in the only-new digest; `dotnet test` failed with 1 failed test at that line, showing actual digest excluded `UrlB`; reverted and re-ran green: 97 passed, 0 failed. |
| Clean SHA after revert / before review doc | PASS | `186b716719eca402c98ff9464e4784330986dc00` |

## Correctness checks

| Check | Status | Evidence |
| --- | --- | --- |
| `digestOnlyNew` is optional and defaults false. | ok | Constructor adds `bool digestOnlyNew = false` after the existing optional 034 params at `src/AmetekWatch.Core/Pipeline/SweepRunner.cs:37`-`43`, stores it at `src/AmetekWatch.Core/Pipeline/SweepRunner.cs:50`. |
| Backward-compatible 3-arg construction still compiles. | ok | Existing helper still calls `new SweepRunner(searcher, new FakeTriageDecider(), store)` at `tests/AmetekWatch.Tests/SweepRunnerTests.cs:25`-`29`; App host also still calls the 3-arg constructor at `src/AmetekWatch.App/SweepHost.cs:50`-`53`. Build and tests pass unchanged. |
| Spec 034 construction still compiles. | ok | Resilience test still uses named `onTriageError` without passing `digestOnlyNew` at `tests/AmetekWatch.Tests/SweepRunnerResilienceTests.cs:38`-`42`; suite passes. |
| No `IFindingStore` interface change. | ok | Interface remains only `SaveAsync` and `GetAllAsync` at `src/AmetekWatch.Core/Pipeline/IFindingStore.cs:9`-`13`. |
| Newness uses a pre-sweep snapshot of `GetAllAsync()` URLs. | ok | Snapshot happens immediately after query null-check and before search/triage/save at `src/AmetekWatch.Core/Pipeline/SweepRunner.cs:61`-`74`; search starts later at `src/AmetekWatch.Core/Pipeline/SweepRunner.cs:78`-`80`, and saves occur later at `src/AmetekWatch.Core/Pipeline/SweepRunner.cs:111`-`113`. This is adversarially correct: the snapshot cannot include this sweep's upserts. |
| A finding is new iff its `Url` was not already present. | ok | Existing findings populate `knownUrls` from `tf.Finding.Url` at `src/AmetekWatch.Core/Pipeline/SweepRunner.cs:69`-`73`; digest excludes only when `knownUrls.Contains(t.Finding.Url)` at `src/AmetekWatch.Core/Pipeline/SweepRunner.cs:118`-`120`. |
| All triaged findings are still persisted regardless of digest filtering. | ok | Each successfully triaged `tf` is saved before digest filtering at `src/AmetekWatch.Core/Pipeline/SweepRunner.cs:111`-`113`; only the returned digest query is filtered at `src/AmetekWatch.Core/Pipeline/SweepRunner.cs:118`-`122`. Test confirms known `UrlB` and non-worth `UrlC` remain persisted at `tests/AmetekWatch.Tests/SweepRunnerOnlyNewTests.cs:57`-`60`. |
| Ordering unchanged. | ok | Digest still applies `OrderByDescending(t => t.Finding.DiscoveredAt)` after worth/new filtering at `src/AmetekWatch.Core/Pipeline/SweepRunner.cs:118`-`122`; existing ordering test remains unchanged at `tests/AmetekWatch.Tests/SweepRunnerTests.cs:92`-`106`. |
| With `digestOnlyNew=true`, already-known worth-reporting URL is excluded from digest but persisted. | ok | Seeded known `UrlB` at `tests/AmetekWatch.Tests/SweepRunnerOnlyNewTests.cs:31`-`40`; `digestOnlyNew: true` at `tests/AmetekWatch.Tests/SweepRunnerOnlyNewTests.cs:46`-`50`; digest expected `[UrlA, UrlD]` and excludes `UrlB` at `tests/AmetekWatch.Tests/SweepRunnerOnlyNewTests.cs:53`-`55`; persistence of `UrlB` asserted at `tests/AmetekWatch.Tests/SweepRunnerOnlyNewTests.cs:57`-`58`. |
| With `digestOnlyNew=true`, genuinely new worth-reporting findings are included. | ok | Same test expects new worth-reporting `UrlA` and `UrlD` in digest at `tests/AmetekWatch.Tests/SweepRunnerOnlyNewTests.cs:53`-`54`. Empty-store case confirms all worth-reporting findings are new at `tests/AmetekWatch.Tests/SweepRunnerOnlyNewTests.cs:76`-`87`. |
| With `digestOnlyNew=false`, digest is unchanged. | ok | Seeded false case constructs `digestOnlyNew: false` at `tests/AmetekWatch.Tests/SweepRunnerOnlyNewTests.cs:63`-`70` and expects unchanged worth-reporting digest `[UrlB, UrlA, UrlD]` at `tests/AmetekWatch.Tests/SweepRunnerOnlyNewTests.cs:72`-`73`. |

## HOLD blockers

None.

VERDICT: PASS
