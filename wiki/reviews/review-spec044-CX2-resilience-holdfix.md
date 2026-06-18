# PASS - CX2 integration review for CC spec-043 resilience HOLD-fix

Branch state:
- Integration branch: `feature/cx2-integrate-043`
- Clean reviewed SHA before review commit: `9e7992a77057cd2cbeaff1225d17020d45965c94`
- Reviewed source branch: `origin/feature/cc-resilience-holdfix`
- Reviewed source SHA: `9e7992a77057cd2cbeaff1225d17020d45965c94`
- .NET SDK: `8.0.422`

Gate table:

| Gate | Command | Result |
| --- | --- | --- |
| Build | `PATH="$HOME/.dotnet:$PATH" dotnet build -c Release` | PASS: 0 warnings, 0 errors |
| Format | `PATH="$HOME/.dotnet:$PATH" dotnet format --verify-no-changes` | PASS: no changes required |
| Tests | `PATH="$HOME/.dotnet:$PATH" dotnet test` | PASS: 118 total, 0 failed, 0 skipped (`Storage` 4 + `AmetekWatch.Tests` 66 + `Anthropic` 46 + `Web` 2) |
| Can-fail | Inverted one transient-classification assertion, ran `dotnet test`, then reverted and reran | PASS: observed nonzero test run with `AnthropicTransientTests.BadRequest_400_IsNotTransient` failing at line 76; after revert, `dotnet test` passed all 118 |

Run note:
- `PATH="$HOME/.dotnet:$PATH" dotnet run --project src/AmetekWatch.App` exited 0 on default fakes.
- Output confirmed one sweep for `AMETEK`, `Pipeline: FAKE`, SQLite store `ametek-watch.db`, file digest sink, 4 persisted findings, and 3 worth-reporting digest items.
- The run wrote ignored runtime outputs `ametek-watch.db` and `ametek-watch-digest.md`; the digest file contained 3 reportable items.

042 blockers resolved:
- 401 no-retry test exists: `tests/AmetekWatch.Anthropic.Tests/AnthropicTransientTests.cs:80` constructs `AnthropicUnauthorizedException`; `tests/AmetekWatch.Anthropic.Tests/AnthropicTransientTests.cs:89` asserts `AnthropicTransient.IsTransient(ex) == false`.
- 403 no-retry test exists: `tests/AmetekWatch.Anthropic.Tests/AnthropicTransientTests.cs:93` constructs `AnthropicForbiddenException`; `tests/AmetekWatch.Anthropic.Tests/AnthropicTransientTests.cs:102` asserts `AnthropicTransient.IsTransient(ex) == false`.
- `SweepHost` runner seam is backward-compatible and blessed by 043: `src/AmetekWatch.App/SweepHost.cs:38`-`44` keeps existing constructor parameters and adds optional `SweepRunner? runner = null`; `src/AmetekWatch.App/SweepHost.cs:51` maps null to `new SweepRunner(_searcher, _triage, _store)`. Existing construction still compiles and is exercised without passing a runner in `tests/AmetekWatch.Tests/SweepHostTests.cs:33`-`37`; the full prior suite passed.

Correctness re-verification:
- Anthropic transient classifier is conservative: `src/AmetekWatch.Anthropic/AnthropicTransient.cs:56`-`65` returns true only for 429, 529/5xx, and API status >=500; `src/AmetekWatch.Anthropic/AnthropicTransient.cs:68`-`78` returns true for SDK IO, raw network, and timeout failures; `src/AmetekWatch.Anthropic/AnthropicTransient.cs:80`-`83` returns false for all other argument/parse/non-API failures.
- Classifier tests cover transient 429/5xx/529/network/timeout cases at `tests/AmetekWatch.Anthropic.Tests/AnthropicTransientTests.cs:18`-`64` and `tests/AmetekWatch.Anthropic.Tests/AnthropicTransientTests.cs:117`-`141`; non-transient 4xx, argument, parse, invalid data, and null cases at `tests/AmetekWatch.Anthropic.Tests/AnthropicTransientTests.cs:67`-`114` and `tests/AmetekWatch.Anthropic.Tests/AnthropicTransientTests.cs:144`-`166`.
- `SweepComposer` selects `RetryPolicy` for real tier and `NoRetryPolicy` for fake tier at `src/AmetekWatch.App/SweepComposer.cs:76`-`81`; fake-tier test asserts `NoRetryPolicy` at `tests/AmetekWatch.Tests/SweepComposerResilienceTests.cs:40`-`53`.
- `SweepComposer` passes `digestOnlyNew: options.OnlyReportNew` and logs triage errors through `onTriageError` at `src/AmetekWatch.App/SweepComposer.cs:83`-`90`. Only-new behavior is proven by `tests/AmetekWatch.Tests/SweepComposerResilienceTests.cs:61`-`99`.
- Config binds `Pipeline:Retry` via `src/AmetekWatch.App/PipelineOptions.cs:23`-`27` and retry defaults at `src/AmetekWatch.App/PipelineOptions.cs:34`-`40`; shipped config sets `Pipeline:Retry` at `src/AmetekWatch.App/appsettings.json:11`-`16`.
- Config binds `Sweep:OnlyReportNew` with default false at `src/AmetekWatch.App/SweepOptions.cs:28`-`34`; shipped config sets `"OnlyReportNew": false` at `src/AmetekWatch.App/appsettings.json:2`-`7`.
- No live call in default fakes: shipped config has `Pipeline:UseRealApi=false` at `src/AmetekWatch.App/appsettings.json:11`-`12`; `SweepComposer` returns fakes immediately when false at `src/AmetekWatch.App/SweepComposer.cs:111`-`115`. The real client factory is only reached when `UseRealApi` is true and `ANTHROPIC_API_KEY` is present at `src/AmetekWatch.App/SweepComposer.cs:117`-`123`.
- `.sln` untouched: `git diff --name-only origin/main...HEAD | rg '\.sln$'` returned no results.

No-secret confirmation:
- Reviewed the branch diff for key/secret/token patterns. The diff references only the environment variable name `ANTHROPIC_API_KEY` and configuration booleans; no secret value, token, or hardcoded API key is present.
- The app path reads only key presence and does not print secrets: `src/AmetekWatch.App/Program.cs:15`-`16`; `src/AmetekWatch.App/SweepComposer.cs:35`-`37`; `src/AmetekWatch.App/SweepComposer.cs:117`-`127`.

HOLD blockers:
- None.

VERDICT: PASS
