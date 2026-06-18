PASS: dashboard reads findings from the shared SQLite store with isolated temp-DB Web tests.

## Branch state

- Integration branch: feature/cx2-integrate-017
- Clean reviewed branch SHA before this review file: bc54bd4bc2dea59eecc9fe66ac1109066e918a27
- Reviewed origin/feature/cc-dashboard-sqlite SHA: bc54bd4bc2dea59eecc9fe66ac1109066e918a27
- Merge base with origin/main: 7d8f5f902bf1eb2bf0fd8e3605a1ba60d95f5405
- dotnet --version: 8.0.422

## Gates

| Gate | Command | Result | Counts |
| --- | --- | --- | --- |
| Build | `PATH="$HOME/.dotnet:$PATH" dotnet build -c Release` | PASS | 7 projects built; 0 warnings; 0 errors |
| Format | `PATH="$HOME/.dotnet:$PATH" dotnet format --verify-no-changes` | PASS | 0 files changed |
| Test | `PATH="$HOME/.dotnet:$PATH" dotnet test` | PASS | 35 passed; 0 failed; 0 skipped |
| Can-fail probe | Temporarily changed `Assert.Equal(3, findings.Count)` to `Assert.Equal(4, findings.Count)`, ran `PATH="$HOME/.dotnet:$PATH" dotnet test`, reverted, reran green | PASS | Probe failed as expected: 34 passed; 1 failed; 0 skipped. Failure was `Expected: 4`, `Actual: 3` at `tests/AmetekWatch.Web.Tests/FindingsApiTests.cs:108`. Re-run after revert: 35 passed; 0 failed; 0 skipped |

## Correctness checks

| Check | Result | Evidence |
| --- | --- | --- |
| Web reads `Storage:DbPath` with default `ametek-watch.db` | ok | `src/AmetekWatch.Web/Program.cs:22` reads `builder.Configuration["Storage:DbPath"] ?? "ametek-watch.db"`. |
| Web serves findings from `SqliteFindingStore` | ok | `src/AmetekWatch.Web/Program.cs:23` registers `new SqliteFindingStore(dbPath)` as the singleton `IFindingStore`. The Web project references Storage at `src/AmetekWatch.Web/AmetekWatch.Web.csproj:9-12`. |
| In-memory fake startup seeding is gone | ok | Current `src/AmetekWatch.Web/Program.cs:20-23` only reads config and registers SQLite. The removed main-branch fake sweep was `InMemoryFindingStore` plus `SweepRunner` at old `Program.cs:17-23`; no current Web reference to `FakeSearcher`, `FakeTriageDecider`, `SweepRunner`, or `InMemoryFindingStore` remains. |
| Fresh or empty DB returns `[]` with no crash | ok | `SqliteFindingStore` creates schema in the constructor at `src/AmetekWatch.Storage/SqliteFindingStore.cs:27-35` and `:38-60`; `GetAllAsync` returns an empty list when no rows read at `:125-153`. Web test `GetFindings_FreshEmptyDb_ReturnsEmptyArray` creates a unique temp DB at `tests/AmetekWatch.Web.Tests/FindingsApiTests.cs:136`, schema-inits only at `:139-140`, calls `/api/findings` at `:142-143`, and asserts empty at `:145`. |
| `GET /api/findings` returns seeded findings most-recent-first | ok | Endpoint maps `/api/findings` at `src/AmetekWatch.Web/Program.cs:27-35` and orders descending by `Finding.DiscoveredAt` at `:31-33`. The test seeds +9h, +11h, +10h out of insert order at `tests/AmetekWatch.Web.Tests/FindingsApiTests.cs:100-103`, then expects c, b, a at `:110-119`. |
| Endpoints, localhost binding, and read-only behavior unchanged | ok | Binding remains loopback-only at `src/AmetekWatch.Web/Program.cs:17-18`; endpoints remain `GET /api/findings` at `:27-35` and `GET /` at `:37-45`; no write endpoints are added. |
| Web tests override `Storage:DbPath` to a temp DB | ok | `TempDbFactory` uses host configuration before `Program` reads config at `tests/AmetekWatch.Web.Tests/FindingsApiTests.cs:47-56`; the override value is `Storage:DbPath = _dbPath` at `:51-54`. `NewTempDbPath` uses a GUID under temp at `:60-62`, avoiding stale shared files. |
| Tests seed via `SqliteFindingStore` with hand-computed expectations | ok | Seeded test constructs `SqliteFindingStore(dbPath)` at `tests/AmetekWatch.Web.Tests/FindingsApiTests.cs:100`; expectations are fixed from `Anchor` at `:32-33`, `MakeFinding` at `:64-78`, expected URL order at `:110-119`, and first-row field checks at `:121-125`. |
| No Anthropic or HTTP dependency in Web implementation/tests | ok | Web project has only Core and Storage project references at `src/AmetekWatch.Web/AmetekWatch.Web.csproj:9-12`. Web test packages are local test-host/xUnit packages plus project references at `tests/AmetekWatch.Web.Tests/AmetekWatch.Web.Tests.csproj:12-28`; no Anthropic package or outbound HTTP client dependency is introduced. The only HTTP usage is in-process `WebApplicationFactory` client at `tests/AmetekWatch.Web.Tests/FindingsApiTests.cs:81-85`. |
| Sweep host (015) and non-Web source untouched by this spec branch | ok | `git diff --name-status origin/main...HEAD` lists only `CLAUDE.md`, `src/AmetekWatch.Web/AmetekWatch.Web.csproj`, `src/AmetekWatch.Web/Program.cs`, `tests/AmetekWatch.Web.Tests/AmetekWatch.Web.Tests.csproj`, `tests/AmetekWatch.Web.Tests/FindingsApiTests.cs`, and `wiki/reports/report-spec017-CC-dashboard-reads-sqlite.md`; no `src/AmetekWatch.App/SweepHost.cs`, app host code, Core source, or Storage source changes are in this branch. |

## HOLD blockers

None.

VERDICT: PASS
