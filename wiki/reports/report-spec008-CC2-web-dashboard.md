# Report — Spec 008-CC2: Local web-UI dashboard

**Headline outcome:** **Not merged** (by design — CC2 does not self-merge). An ASP.NET minimal-API
dashboard `src/AmetekWatch.Web` + `tests/AmetekWatch.Web.Tests` are implemented on branch
`feature/cc2-web-dashboard`, both appended to `AmetekWatch.sln`, pushed to origin. Gate green on all
three commands (.NET 8.0.422): build 0 warnings, format clean, **10/10 tests** (7 prior + 3 new).
No version bump (`<Version>` stays `0.1.0`). In-memory store only — independent of SQLite (007). CX
integrates (cross-model) and issues VERDICT; CM lands on PASS.

## 1. Branch / merge state
- Pre-merge `main` SHA (origin/main at branch point): `9ae0a6a85aa258333b680cf237382c2d52d9be81`
- Feature branch: `feature/cc2-web-dashboard`; branch deleted post-merge: n (not merged yet)
- Post-merge `main` SHA (pushed): N/A — author does not self-merge.
- Merge mechanic: pushed branch + tip SHA; CX integrator runs the gate and lands via `--no-ff`.
- **Branch tip SHA: see the FINAL message** (reported via `git rev-parse HEAD` after all commits).

## 2. Changes
| File | Change |
|---|---|
| `src/AmetekWatch.Web/AmetekWatch.Web.csproj` | New `Microsoft.NET.Sdk.Web` (`net8.0`); shared props inherited from `Directory.Build.props`; `ProjectReference` → Core. Trimmed the template's redundant `Nullable`/`ImplicitUsings` (already in the shared props). |
| `src/AmetekWatch.Web/Program.cs` | Minimal-API host. Seeds `InMemoryFindingStore` via one fake sweep (`FakeSearcher`+`FakeTriageDecider`+`SweepRunner`); registers it as `IFindingStore`. `GET /api/findings` (JSON, most-recent `DiscoveredAt` first); `GET /` (HTML table). Binds `http://localhost:5080`. Ends with `public partial class Program {}` for test reachability. |
| `src/AmetekWatch.Web/Properties/launchSettings.json` | Template default (localhost dev profiles). Unmodified. |
| `src/AmetekWatch.Web/appsettings.json`, `appsettings.Development.json` | Template defaults (logging). Unmodified. |
| `tests/AmetekWatch.Web.Tests/AmetekWatch.Web.Tests.csproj` | New xUnit project; refs Web + Core; `Microsoft.AspNetCore.Mvc.Testing` **8.0.17** (pinned — the unversioned default resolved to net10-only 10.0.9). |
| `tests/AmetekWatch.Web.Tests/FindingsApiTests.cs` | 3 tests via `WebApplicationFactory<Program>`, hand-computed oracles (count/dedupe, ordering, worth-reporting subset). |
| `AmetekWatch.sln` | Appended `AmetekWatch.Web` (nested under `src`) and `AmetekWatch.Web.Tests` (nested under `tests`). Existing 4 entries untouched/unreordered. |
| `CLAUDE.md` | Appended a dated `### Unreleased` bullet for the dashboard (existing bullets untouched). |
| `wiki/reports/report-spec008-CC2-web-dashboard.md` | This report. |

## 3. Endpoints
- **`GET /api/findings`** → `200`, `application/json`. JSON array of all persisted `TriagedFinding`s,
  ordered most-recent `DiscoveredAt` first. Enums serialize as ints (`OpinionSocial`=0,
  `FinancialReport`=1, `Other`=2); `DateTimeOffset` as ISO-8601. Verbatim sample (whole response):

```json
[{"finding":{"url":"https://local.example.com/ametek-5k-sponsor","title":"AMETEK sponsors local charity 5K","snippet":"Community sponsorship announcement.","publishedAt":null,"category":2,"discoveredAt":"2026-06-18T11:00:00+00:00"},"verdict":{"important":false,"relevant":true,"worthReporting":false,"rationale":"Category Other is not reportable under the slice rule."}},{"finding":{"url":"https://ir.example.com/ametek-q2-earnings","title":"AMETEK reports Q2 earnings beat","snippet":"Quarterly results topped consensus estimates.","publishedAt":"2026-06-16T00:00:00+00:00","category":1,"discoveredAt":"2026-06-18T10:00:00+00:00"},"verdict":{"important":true,"relevant":true,"worthReporting":true,"rationale":"Category FinancialReport is reportable under the slice rule."}},{"finding":{"url":"https://news.example.com/ametek-analyst-note","title":"AMETEK shares climb on upbeat analyst note","snippet":"Commentary roundup on AMETEK's latest guidance.","publishedAt":"2026-06-17T00:00:00+00:00","category":0,"discoveredAt":"2026-06-18T09:00:00+00:00"},"verdict":{"important":true,"relevant":true,"worthReporting":true,"rationale":"Category OpinionSocial is reportable under the slice rule."}},{"finding":{"url":"https://sec.example.com/ametek-10q","title":"AMETEK files Form 10-Q","snippet":"Quarterly SEC filing now available.","publishedAt":"2026-06-15T00:00:00+00:00","category":1,"discoveredAt":"2026-06-18T08:00:00+00:00"},"verdict":{"important":true,"relevant":true,"worthReporting":true,"rationale":"Category FinancialReport is reportable under the slice rule."}}]
```

- **`GET /`** → `200`, `text/html`. Self-contained HTML page; `<h1>AMETEK Watch — findings (4)</h1>`
  over a table of the four findings (Category, Title, URL, Worth reporting, Rationale), same
  most-recent-first order. All dynamic text HTML-encoded.

### Hand-computed oracle (how the expectations were derived)
`FakeSearcher.Canned` = 5 findings; the `ametek-analyst-note` "(reblog)" duplicate is dropped by
`SweepRunner`'s URL-dedupe → **4 persisted**. Triage rule: `OpinionSocial` + `FinancialReport` →
worth-reporting; `Other` → not. `DiscoveredAt` anchor-hours: analyst-note +9h, q2-earnings +10h,
5k-sponsor +11h, 10q +8h → most-recent-first order **5k-sponsor, q2-earnings, analyst-note, 10q**.
Worth-reporting subset (3): analyst-note, q2-earnings, 10q. The lone non-reportable is 5k-sponsor
(`Other`). All four matched the live response exactly.

## Gate results
| Command (run separately) | Result | Counts |
|---|---|---|
| `dotnet build -c Release` | ✓ | 0 warnings, 0 errors; 4 projects + Web/Web.Tests build |
| `dotnet format --verify-no-changes` | ✓ | exit 0, no changes |
| `dotnet test` | ✓ | **10/10 passed** (AmetekWatch.Tests 7/7 + AmetekWatch.Web.Tests 3/3), 0 failed, 0 skipped |

- **Test count before:** 7 (AmetekWatch.Tests only). **After:** 10 (+3 new web tests).
- **Can-fail check:** inverted `Assert.Equal(4, findings.Count)` → `5`; ran web tests → `Failed: 1`
  (`Expected: 5, Actual: 4`); reverted → `Passed: 3`. Confirmed the test can actually fail.
- **Clean SHA (gate ran green at):** see FINAL message (`git rev-parse HEAD`). The gate was last run
  green on the committed tree.
- **`dotnet --version`:** `8.0.422`.
- **Files changed NOT in the spec's files-to-change list:** the three ASP.NET template config files
  (`launchSettings.json`, `appsettings.json`, `appsettings.Development.json`) ship with
  `dotnet new web` and were left at their defaults (all localhost, no secrets). Not separately named
  by the spec but expected scaffolding.

## Sources beyond the brief / surprises
- **`dotnet sln add` chained behind a failing command.** My initial setup chained `dotnet add package
  Microsoft.AspNetCore.Mvc.Testing` (no version) with `&& dotnet sln add <test project>`. The package
  add failed (see next bullet), so the `&&` short-circuited and the **test project was silently not
  added to the solution** — `dotnet test` then ran only the one pre-existing assembly (7 tests) and
  looked green. Caught it by diffing `dotnet sln list` against expectation; added the project
  explicitly and re-ran. Lesson reinforced: run gate/setup steps separately, and verify the test
  assembly count, not just pass/fail.
- **`Microsoft.AspNetCore.Mvc.Testing` unversioned → net10-only.** `dotnet add package` with no
  version resolved `10.0.9` (`NU1202`: supports net10.0 only). Pinned to `8.0.17` (the latest 8.x).
- **Web SDK template duplicated shared props.** `dotnet new web` emitted its own
  `Nullable`/`ImplicitUsings`; removed them so the project inherits `Directory.Build.props` like the
  other projects (incl. `TreatWarningsAsErrors`).
- **Raw-string brace escaping.** The HTML template's CSS braces broke a single-`$` interpolated raw
  string (`CS9006`); switched to `$$"""…"""` (interpolation = `{{ }}`, literal CSS braces single).

## Deferred / not done
- **SQLite store (007)** — out of scope; used `InMemoryFindingStore`. Registration swap is a later spec.
- **HTML endpoint has no automated assertion** — only `/api/findings` is asserted (per spec Decision 4).
  `GET /` was verified manually via curl (output in §3) but not covered by a test.
- **Windows runtime / browser rendering** — not exercised; dashboard tested headless on Linux via
  `WebApplicationFactory` and curl.

## Standing flags
None.

## Roles update notice
None — no role doc edited this session.
