PASS - CC spec-028 capstone integration review: pipeline toggle and digest wiring are accepted.

## Branch State

- Review branch: `feature/cx-integrate-028`
- Review branch SHA before this review commit: `1e1732f3d31db17ae7cc66d34d1aeb7d1af7aa61`
- Reviewed source branch: `origin/feature/cc-pipeline-toggle`
- Reviewed source SHA: `1e1732f3d31db17ae7cc66d34d1aeb7d1af7aa61`
- Clean SHA after gates/can-fail restore: `1e1732f3d31db17ae7cc66d34d1aeb7d1af7aa61`
- .NET SDK: `8.0.422`

## Gate Table

| Gate | Command | Result | Real counts |
| --- | --- | --- | --- |
| Build | `PATH="$HOME/.dotnet:$PATH" dotnet build -c Release` | PASS | 9 projects built/restored; 0 warnings, 0 errors |
| Format | `PATH="$HOME/.dotnet:$PATH" dotnet format --verify-no-changes` | PASS | no output; no formatting changes |
| Test | `PATH="$HOME/.dotnet:$PATH" dotnet test` | PASS | 76 passed, 0 failed, 0 skipped |
| Can-fail | Temporarily inverted `Create_UseRealApiFalse_ResolvesFakes` assertion, ran `dotnet test`, reverted, reran | PASS | inverted run: 75 passed, 1 failed, 0 skipped; restored run: 76 passed, 0 failed, 0 skipped |

Can-fail observed failure:

```text
Failed AmetekWatch.Tests.PipelineToggleAndDigestTests.Create_UseRealApiFalse_ResolvesFakes
Assert.IsType() Failure: Value is not the exact type
Expected: typeof(AmetekWatch.Anthropic.AnthropicSearcher)
Actual:   typeof(AmetekWatch.Core.Pipeline.FakeSearcher)
```

## Executable Run

Command:

```text
rm -f ametek-watch.db ametek-watch-digest.md
env -u ANTHROPIC_API_KEY PATH="$HOME/.dotnet:$PATH" dotnet run --project src/AmetekWatch.App
```

Stdout:

```text
AMETEK Watch - sweep for "AMETEK"
Pipeline:               FAKE (deterministic; Pipeline:UseRealApi=false)
Store (SQLite):         ametek-watch.db
Digest sink:            ametek-watch-digest.md
Persisted findings:     4
Worth-reporting digest: 3

[1] FinancialReport - AMETEK reports Q2 earnings beat
    url:       https://ir.example.com/ametek-q2-earnings
    rationale: Category FinancialReport is reportable under the slice rule.
[2] OpinionSocial - AMETEK shares climb on upbeat analyst note
    url:       https://news.example.com/ametek-analyst-note
    rationale: Category OpinionSocial is reportable under the slice rule.
[3] FinancialReport - AMETEK files Form 10-Q
    url:       https://sec.example.com/ametek-10q
    rationale: Category FinancialReport is reportable under the slice rule.
```

Digest-file confirmation:

- `ametek-watch.db` existed after the run.
- `ametek-watch-digest.md` existed after the run, size 647 bytes.
- Digest content started with `# AMETEK Watch digest` and included `**3 items worth reporting.**`.

No-key real-toggle runtime check was exercised without editing source by copying the built app to `/tmp`, changing only the copied runtime `appsettings.json` to `"UseRealApi": true`, and running `env -u ANTHROPIC_API_KEY dotnet ametek-watch.dll`. It printed:

```text
WARNING: Pipeline:UseRealApi is true but ANTHROPIC_API_KEY is not set - falling back to the deterministic fakes.
Pipeline:               FAKE (fell back: ANTHROPIC_API_KEY not set)
Persisted findings:     4
Worth-reporting digest: 3
```

## Correctness Checks

- OK - `PipelineFactory` returns real `AnthropicSearcher` and `AnthropicTriageDecider` when `useRealApi=true`: `src/AmetekWatch.App/PipelineFactory.cs:35`, `src/AmetekWatch.App/PipelineFactory.cs:39`, `src/AmetekWatch.App/PipelineFactory.cs:44`, `src/AmetekWatch.App/PipelineFactory.cs:49`.
- OK - `PipelineFactory` returns `FakeSearcher` and `FakeTriageDecider` when `useRealApi=false`: `src/AmetekWatch.App/PipelineFactory.cs:30`, `src/AmetekWatch.App/PipelineFactory.cs:32`.
- OK - `PipelineFactory` invokes no search/triage work; it only constructs and returns objects. The real client factory is called to construct adapters, but no `SweepAsync` or `JudgeAsync` call appears in the factory: `src/AmetekWatch.App/PipelineFactory.cs:25`, `src/AmetekWatch.App/PipelineFactory.cs:36`, `src/AmetekWatch.App/PipelineFactory.cs:39`, `src/AmetekWatch.App/PipelineFactory.cs:44`, `src/AmetekWatch.App/PipelineFactory.cs:49`.
- OK - `Program` binds `Sweep`, `Pipeline`, `Notify`, and `Storage:DbPath` from config: `src/AmetekWatch.App/Program.cs:18`, `src/AmetekWatch.App/Program.cs:23`, `src/AmetekWatch.App/Program.cs:24`, `src/AmetekWatch.App/Program.cs:25`, `src/AmetekWatch.App/Program.cs:26`.
- OK - Default config selects fakes and a digest path: `src/AmetekWatch.App/appsettings.json:10`, `src/AmetekWatch.App/appsettings.json:11`, `src/AmetekWatch.App/appsettings.json:13`, `src/AmetekWatch.App/appsettings.json:14`.
- OK - When `UseRealApi=true`, `Program` checks only whether `ANTHROPIC_API_KEY` is present, prints a clear warning if absent, falls back to fakes, and logs the active fake fallback pipeline: `src/AmetekWatch.App/Program.cs:34`, `src/AmetekWatch.App/Program.cs:36`, `src/AmetekWatch.App/Program.cs:47`, `src/AmetekWatch.App/Program.cs:49`, `src/AmetekWatch.App/Program.cs:50`, `src/AmetekWatch.App/Program.cs:70`, `src/AmetekWatch.App/Program.cs:71`.
- OK - Digest delivery is through `IDigestNotifier`: interface at `src/AmetekWatch.Core/Notify/IDigestNotifier.cs:10`, `SweepHost` receives it at `src/AmetekWatch.App/SweepHost.cs:24`, defaults to `NullDigestNotifier` at `src/AmetekWatch.App/SweepHost.cs:41`, and calls it after persistence via `SweepRunner.RunAsync` at `src/AmetekWatch.App/SweepHost.cs:52`, `src/AmetekWatch.App/SweepHost.cs:53`, `src/AmetekWatch.App/SweepHost.cs:54`.
- OK - `Program` chooses `FileDigestNotifier` when `Notify:DigestPath` is set and `NullDigestNotifier` otherwise: `src/AmetekWatch.App/Program.cs:60`, `src/AmetekWatch.App/Program.cs:61`, `src/AmetekWatch.App/Program.cs:62`.
- OK - `FileDigestNotifier` writes the digest file content: `src/AmetekWatch.Core/Notify/FileDigestNotifier.cs:38`, `src/AmetekWatch.Core/Notify/FileDigestNotifier.cs:60`, `src/AmetekWatch.Core/Notify/FileDigestNotifier.cs:63`, `src/AmetekWatch.Core/Notify/FileDigestNotifier.cs:64`.
- OK - `RunOnce` remains default true in both options and shipped config: `src/AmetekWatch.App/SweepOptions.cs:26`, `src/AmetekWatch.App/appsettings.json:5`.
- OK - Selection test asserts resolved types without invoking them; the fake messages client throws if invoked, and tests assert only runtime types: `tests/AmetekWatch.Tests/PipelineToggleAndDigestTests.cs:35`, `tests/AmetekWatch.Tests/PipelineToggleAndDigestTests.cs:38`, `tests/AmetekWatch.Tests/PipelineToggleAndDigestTests.cs:43`, `tests/AmetekWatch.Tests/PipelineToggleAndDigestTests.cs:45`, `tests/AmetekWatch.Tests/PipelineToggleAndDigestTests.cs:51`, `tests/AmetekWatch.Tests/PipelineToggleAndDigestTests.cs:52`, `tests/AmetekWatch.Tests/PipelineToggleAndDigestTests.cs:56`, `tests/AmetekWatch.Tests/PipelineToggleAndDigestTests.cs:60`, `tests/AmetekWatch.Tests/PipelineToggleAndDigestTests.cs:61`.
- OK - End-to-end test uses fakes plus temp DB plus temp digest path and asserts SQLite persistence plus digest file content: `tests/AmetekWatch.Tests/PipelineToggleAndDigestTests.cs:27`, `tests/AmetekWatch.Tests/PipelineToggleAndDigestTests.cs:31`, `tests/AmetekWatch.Tests/PipelineToggleAndDigestTests.cs:65`, `tests/AmetekWatch.Tests/PipelineToggleAndDigestTests.cs:67`, `tests/AmetekWatch.Tests/PipelineToggleAndDigestTests.cs:68`, `tests/AmetekWatch.Tests/PipelineToggleAndDigestTests.cs:70`, `tests/AmetekWatch.Tests/PipelineToggleAndDigestTests.cs:73`, `tests/AmetekWatch.Tests/PipelineToggleAndDigestTests.cs:74`, `tests/AmetekWatch.Tests/PipelineToggleAndDigestTests.cs:77`, `tests/AmetekWatch.Tests/PipelineToggleAndDigestTests.cs:81`, `tests/AmetekWatch.Tests/PipelineToggleAndDigestTests.cs:83`, `tests/AmetekWatch.Tests/PipelineToggleAndDigestTests.cs:84`.
- OK - No live API call in tests: selection uses `UnusedMessagesClient` and does not invoke it; digest test uses `PipelineFactory.Create(useRealApi: false)`: `tests/AmetekWatch.Tests/PipelineToggleAndDigestTests.cs:36`, `tests/AmetekWatch.Tests/PipelineToggleAndDigestTests.cs:38`, `tests/AmetekWatch.Tests/PipelineToggleAndDigestTests.cs:45`, `tests/AmetekWatch.Tests/PipelineToggleAndDigestTests.cs:67`.
- OK - `.sln` untouched: `git diff --name-only origin/main...HEAD | rg '\.sln$'` returned no matches.

## No-Secret Confirmation

Grep of the CC diff for key/secret/token/password/bearer patterns found only documentation/code references to the environment variable name `ANTHROPIC_API_KEY` and no hardcoded key or token value. No secret/token was printed or committed.

## HOLD Blockers

None.

VERDICT: PASS
