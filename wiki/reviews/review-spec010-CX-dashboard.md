PASS - spec-008 local web-UI dashboard is integration-ready.

## Branch state
- Review branch: `feature/cx-integrate-008`
- Clean reviewed SHA before review commit: `cf8add427a25386d9de6ce2bce91a6e7a728c701`
- Reviewed source SHA `origin/feature/cc2-web-dashboard`: `cf8add427a25386d9de6ce2bce91a6e7a728c701`
- dotnet: `8.0.422`

## Gate table
| Check | Command | Result | Real counts |
| --- | --- | --- | --- |
| Build | `PATH="$HOME/.dotnet:$PATH" dotnet build -c Release` | PASS | 5 projects built; 0 warnings; 0 errors |
| Format | `PATH="$HOME/.dotnet:$PATH" dotnet format --verify-no-changes` | PASS | 0 files changed; command produced no diagnostics |
| Tests | `PATH="$HOME/.dotnet:$PATH" dotnet test` | PASS | 10 total passed: 7 `AmetekWatch.Tests`, 3 `AmetekWatch.Web.Tests`; 0 failed; 0 skipped |
| Can-fail proof | Temporarily inverted `tests/AmetekWatch.Web.Tests/FindingsApiTests.cs:52` from expected 4 to expected 5, then ran `PATH="$HOME/.dotnet:$PATH" dotnet test` | PASS | Suite failed as expected: 1 failed, 9 passed; failure was `Expected: 5 Actual: 4` in `GetFindings_ReturnsTheFourPersistedFindings_DuplicateDropped`; reverted assertion and re-ran green: 10 passed, 0 failed |

## Correctness checks
- OK: `GET /api/findings` returns store `TriagedFinding` values most-recent first. The endpoint reads `IFindingStore.GetAllAsync`, orders by `Finding.DiscoveredAt` descending, and serializes the ordered list (`src/AmetekWatch.Web/Program.cs:27`, `src/AmetekWatch.Web/Program.cs:30`, `src/AmetekWatch.Web/Program.cs:32`, `src/AmetekWatch.Web/Program.cs:34`). The test asserts the hand-computed URL order (`tests/AmetekWatch.Web.Tests/FindingsApiTests.cs:60`, `tests/AmetekWatch.Web.Tests/FindingsApiTests.cs:65`).
- OK: `GET /` serves an HTML table. The root endpoint returns `Results.Content(RenderHtml(ordered), "text/html")` (`src/AmetekWatch.Web/Program.cs:37`, `src/AmetekWatch.Web/Program.cs:44`), and `RenderHtml` emits a `<table>` with category/title/URL/worth/rationale headers (`src/AmetekWatch.Web/Program.cs:84`, `src/AmetekWatch.Web/Program.cs:86`).
- OK: The store is seeded via one fake Core sweep. Startup constructs `InMemoryFindingStore`, `SweepRunner(new FakeSearcher(), new FakeTriageDecider(), store)`, runs one `SweepQuery(Subject: "AMETEK")`, then registers that populated store as `IFindingStore` (`src/AmetekWatch.Web/Program.cs:19`, `src/AmetekWatch.Web/Program.cs:20`, `src/AmetekWatch.Web/Program.cs:21`, `src/AmetekWatch.Web/Program.cs:23`). Core confirms `SweepRunner` persists every deduped survivor (`src/AmetekWatch.Core/Pipeline/SweepRunner.cs:47`, `src/AmetekWatch.Core/Pipeline/SweepRunner.cs:53`) into the in-memory store (`src/AmetekWatch.Core/Pipeline/InMemoryFindingStore.cs:9`, `src/AmetekWatch.Core/Pipeline/InMemoryFindingStore.cs:16`).
- OK: No dependency on SQLite/spec-007 durable storage in the web slice. The web project references only `AmetekWatch.Core` (`src/AmetekWatch.Web/AmetekWatch.Web.csproj:9`, `src/AmetekWatch.Web/AmetekWatch.Web.csproj:10`), and the test project references only MVC testing/xUnit packages plus Web/Core projects (`tests/AmetekWatch.Web.Tests/AmetekWatch.Web.Tests.csproj:12`, `tests/AmetekWatch.Web.Tests/AmetekWatch.Web.Tests.csproj:14`, `tests/AmetekWatch.Web.Tests/AmetekWatch.Web.Tests.csproj:25`, `tests/AmetekWatch.Web.Tests/AmetekWatch.Web.Tests.csproj:26`). `rg` found only comments mentioning SQLite in the web slice, not package or runtime dependencies.
- OK: The dashboard binds localhost. `UseUrls("http://localhost:5080")` is set on the web host (`src/AmetekWatch.Web/Program.cs:14`, `src/AmetekWatch.Web/Program.cs:15`); launch settings also use localhost URLs (`src/AmetekWatch.Web/Properties/launchSettings.json:7`, `src/AmetekWatch.Web/Properties/launchSettings.json:16`, `src/AmetekWatch.Web/Properties/launchSettings.json:25`).
- OK: `WebApplicationFactory<Program>` test hits the real endpoint and asserts hand-computed values, not a stub. The test fixture is `IClassFixture<WebApplicationFactory<Program>>` (`tests/AmetekWatch.Web.Tests/FindingsApiTests.cs:27`), stores the injected factory (`tests/AmetekWatch.Web.Tests/FindingsApiTests.cs:31`, `tests/AmetekWatch.Web.Tests/FindingsApiTests.cs:33`), creates a client from it and calls `GetAsync("/api/findings")` (`tests/AmetekWatch.Web.Tests/FindingsApiTests.cs:37`, `tests/AmetekWatch.Web.Tests/FindingsApiTests.cs:38`), then deserializes `TriagedFinding` JSON (`tests/AmetekWatch.Web.Tests/FindingsApiTests.cs:41`). The expected count/order/worth-reporting values are stated as hand-computed from fakes (`tests/AmetekWatch.Web.Tests/FindingsApiTests.cs:14`, `tests/AmetekWatch.Web.Tests/FindingsApiTests.cs:17`, `tests/AmetekWatch.Web.Tests/FindingsApiTests.cs:19`, `tests/AmetekWatch.Web.Tests/FindingsApiTests.cs:22`) and asserted directly (`tests/AmetekWatch.Web.Tests/FindingsApiTests.cs:52`, `tests/AmetekWatch.Web.Tests/FindingsApiTests.cs:65`, `tests/AmetekWatch.Web.Tests/FindingsApiTests.cs:87`, `tests/AmetekWatch.Web.Tests/FindingsApiTests.cs:97`). There is no `ConfigureTestServices` override or fake response handler in the test file.
- OK: `AmetekWatch.App` and the existing slice/storage tests are untouched by this feature. `git diff --name-status origin/main...HEAD -- src/AmetekWatch.App tests/AmetekWatch.Tests src/AmetekWatch.Core` returned no changed files, while the feature diff adds only the web project/tests plus solution/docs metadata.

## HOLD blockers
None.

VERDICT: PASS
