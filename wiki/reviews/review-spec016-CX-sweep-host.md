PASS - CC2 spec-015 app sweep host is acceptable for integration review.

## Branch State

- Review branch: `feature/cx-integrate-015`
- Review branch SHA before review artifact: `912e2ae77369f228500c19fe9de5166a996d480a`
- Reviewed source branch: `origin/feature/cc2-app-sweep-host`
- Reviewed source SHA: `912e2ae77369f228500c19fe9de5166a996d480a`
- Clean source SHA after can-fail revert and green re-run: `912e2ae77369f228500c19fe9de5166a996d480a`
- `dotnet --version`: `8.0.422`

## Gate Table

| Gate | Command | Result | Real counts |
| --- | --- | --- | --- |
| Build | `PATH="$HOME/.dotnet:$PATH" dotnet build -c Release` | PASS | 7 project outputs built/listed; 2 projects restored, 5 up-to-date for restore; 0 warnings, 0 errors |
| Format | `PATH="$HOME/.dotnet:$PATH" dotnet format --verify-no-changes` | PASS | 0 changed files reported; command exited 0 with no output |
| Test | `PATH="$HOME/.dotnet:$PATH" dotnet test` | PASS | 36 total: 36 passed, 0 failed, 0 skipped (`AmetekWatch.Storage.Tests` 4, `AmetekWatch.Web.Tests` 3, `AmetekWatch.Tests` 29) |
| Can-fail | Temporarily inverted `Assert.Equal(3, digest.Count)` to `Assert.Equal(4, digest.Count)`, ran `PATH="$HOME/.dotnet:$PATH" dotnet test`, reverted, re-ran green | PASS | Failure observed: 1 failed, 35 passed, 0 skipped; `Expected: 4`, `Actual: 3` at `tests/AmetekWatch.Tests/SweepHostTests.cs:49`; post-revert re-run: 36 passed, 0 failed, 0 skipped |

## dotnet run

Command:

```bash
rm -f ametek-watch.db && PATH="$HOME/.dotnet:$PATH" dotnet run --project src/AmetekWatch.App
```

Digest stdout:

```text
AMETEK Watch — sweep for "AMETEK"
Store (SQLite):         ametek-watch.db
Persisted findings:     4
Worth-reporting digest: 3

[1] FinancialReport — AMETEK reports Q2 earnings beat
    url:       https://ir.example.com/ametek-q2-earnings
    rationale: Category FinancialReport is reportable under the slice rule.
[2] OpinionSocial — AMETEK shares climb on upbeat analyst note
    url:       https://news.example.com/ametek-analyst-note
    rationale: Category OpinionSocial is reportable under the slice rule.
[3] FinancialReport — AMETEK files Form 10-Q
    url:       https://sec.example.com/ametek-10q
    rationale: Category FinancialReport is reportable under the slice rule.
```

Runtime confirmations:

- SQLite DB file created: `ametek-watch.db`, size `12288` bytes after the run.
- `appsettings.json` is found at runtime: `Program.cs` sets the config base to `AppContext.BaseDirectory` and loads `appsettings.json` with `optional: false` at `src/AmetekWatch.App/Program.cs:12`; the project copies `appsettings.json` to output at `src/AmetekWatch.App/AmetekWatch.App.csproj:21`, and the file was present under both Debug and Release output directories after build/run.

## Correctness Checks

| Check | Result | Evidence |
| --- | --- | --- |
| `SweepHost.RunOnceAsync` runs one `SweepRunner` sweep | ok | `RunOnceAsync` constructs `new SweepRunner(_searcher, _triage, _store)` at `src/AmetekWatch.App/SweepHost.cs:42` and calls `runner.RunAsync(new SweepQuery(_options.Subject), ct)` at `src/AmetekWatch.App/SweepHost.cs:43`. |
| `RunOnceAsync` persists to injected `IFindingStore` | ok | The injected store is captured at `src/AmetekWatch.App/SweepHost.cs:20` and assigned from constructor input at `src/AmetekWatch.App/SweepHost.cs:31`; the runner persists every triaged survivor through `_store.SaveAsync(tf, ct)` at `src/AmetekWatch.Core/Pipeline/SweepRunner.cs:53`. |
| `RunOnceAsync` returns the worth-reporting digest | ok | `SweepRunner.RunAsync` returns `triaged.Where(t => t.Verdict.WorthReporting).OrderByDescending(...)` at `src/AmetekWatch.Core/Pipeline/SweepRunner.cs:58`; `RunOnceAsync` returns that task result directly at `src/AmetekWatch.App/SweepHost.cs:43`. |
| `RunAsync` loops and is cancellation-friendly | ok | `RunAsync` loops with `while (true)` at `src/AmetekWatch.App/SweepHost.cs:54`, checks `ct.ThrowIfCancellationRequested()` before each sweep at `src/AmetekWatch.App/SweepHost.cs:56`, passes the token into `RunOnceAsync` at `src/AmetekWatch.App/SweepHost.cs:57`, exits for `RunOnce` at `src/AmetekWatch.App/SweepHost.cs:59`, and otherwise waits with token-aware `Task.Delay(..., ct)` at `src/AmetekWatch.App/SweepHost.cs:64`. This is not a busy-spin because every non-RunOnce iteration awaits the interval delay. |
| Config binds `SweepOptions.Subject` | ok | `Program.cs` binds `config.GetSection("Sweep").Get<SweepOptions>()` at `src/AmetekWatch.App/Program.cs:17`; the config supplies `"Subject": "AMETEK"` at `src/AmetekWatch.App/appsettings.json:3`; the option defaults to `"AMETEK"` at `src/AmetekWatch.App/SweepOptions.cs:16`. |
| Config binds `SweepOptions.IntervalMinutes` | ok | The same bind occurs at `src/AmetekWatch.App/Program.cs:17`; config supplies `"IntervalMinutes": 1440` at `src/AmetekWatch.App/appsettings.json:4`; the option default is `1440` at `src/AmetekWatch.App/SweepOptions.cs:19`. |
| Config binds `SweepOptions.RunOnce` and defaults to terminating CLI mode | ok | Config supplies `"RunOnce": true` at `src/AmetekWatch.App/appsettings.json:5`; the option default is `true` at `src/AmetekWatch.App/SweepOptions.cs:26`; the loop returns when true at `src/AmetekWatch.App/SweepHost.cs:59`. The CLI calls `RunOnceAsync` directly at `src/AmetekWatch.App/Program.cs:23`, so it terminates deterministically regardless. |
| Config binds storage `DbPath` | ok | `Program.cs` reads `config["Storage:DbPath"] ?? "ametek-watch.db"` at `src/AmetekWatch.App/Program.cs:18`; config supplies `"DbPath": "ametek-watch.db"` at `src/AmetekWatch.App/appsettings.json:8`. |
| `Program` constructs `SqliteFindingStore` plus fakes | ok | `Program.cs` constructs `new SqliteFindingStore(dbPath)` at `src/AmetekWatch.App/Program.cs:20` and `new SweepHost(new FakeSearcher(), new FakeTriageDecider(), store, options)` at `src/AmetekWatch.App/Program.cs:21`. |
| No real Anthropic or HTTP dependency anywhere in app composition | ok | App project references only Core and Storage at `src/AmetekWatch.App/AmetekWatch.App.csproj:10`, and only configuration packages at `src/AmetekWatch.App/AmetekWatch.App.csproj:15`; app composition uses only `FakeSearcher` and `FakeTriageDecider` at `src/AmetekWatch.App/Program.cs:21`. Repository search found no Anthropic or HTTP dependency in `src/AmetekWatch.App`. |
| Test uses temp-file SQLite DB | ok | `SweepHostTests` creates a unique path under `Path.GetTempPath()` at `tests/AmetekWatch.Tests/SweepHostTests.cs:29`, constructs `new SqliteFindingStore(_dbPath)` at `tests/AmetekWatch.Tests/SweepHostTests.cs:35`, verifies file creation at `tests/AmetekWatch.Tests/SweepHostTests.cs:72`, and reopens a fresh store over the same file at `tests/AmetekWatch.Tests/SweepHostTests.cs:85`. |
| Test has hand-computed persistence and digest assertions | ok | The hand-computed oracle is documented at `tests/AmetekWatch.Tests/SweepHostTests.cs:13`; assertions check 4 persisted and 3 digest entries at `tests/AmetekWatch.Tests/SweepHostTests.cs:48`, digest URL order at `tests/AmetekWatch.Tests/SweepHostTests.cs:61`, non-worth persisted but excluded from digest at `tests/AmetekWatch.Tests/SweepHostTests.cs:72`, and persistence after reopening the same DB file at `tests/AmetekWatch.Tests/SweepHostTests.cs:88`. |
| Persistence truly hits SQLite, not in-memory | ok | `SqliteFindingStore` builds a `SqliteConnectionStringBuilder` with `DataSource = dbPath` at `src/AmetekWatch.Storage/SqliteFindingStore.cs:30`, creates the schema over a real connection at `src/AmetekWatch.Storage/SqliteFindingStore.cs:40`, and `SaveAsync` opens SQLite and executes an `INSERT ... ON CONFLICT` command at `src/AmetekWatch.Storage/SqliteFindingStore.cs:70`. The host test reopens the same file through a new store and reads back 4 rows at `tests/AmetekWatch.Tests/SweepHostTests.cs:85`. |

## HOLD Blockers

None.

VERDICT: PASS
