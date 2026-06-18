# Report — Spec 017-CC: Dashboard reads from the SQLite store

**Headline outcome:** Built on branch `feature/cc-dashboard-sqlite` — **not merged** (no self-merge;
CX2 integrates cross-model). The web dashboard now serves findings from the shared durable SQLite
store (`Storage:DbPath`, default `ametek-watch.db`) the App sweep host persists to, replacing the
in-memory fake-sweep seeding. Empty/missing DB yields `[]` (no crash) — verified. No version bump
(`<Version>` stays `0.1.0`, internal). Gate green (build 0 warn / format clean / test 35/35).

## 1. Branch / merge state
- Pre-merge `main` SHA: `f85a3fd08c9d01ee5cdd0740b5319bde01d673d3`
- Feature branch: `feature/cc-dashboard-sqlite` (branched from `origin/main`); working commit: see tip
  SHA at the end of this report; branch deleted post-merge: n (not merged)
- Post-merge `main` SHA (pushed): N/A — not merged (CX2 integrates)
- Merge mechanic: pushed branch, integrator (CX2) merges cross-model. No self-merge.

## 2. Changes
| File | Change |
| --- | --- |
| `src/AmetekWatch.Web/AmetekWatch.Web.csproj` | Added `ProjectReference` to `AmetekWatch.Storage`. |
| `src/AmetekWatch.Web/Program.cs` | Replaced the in-memory fake-sweep seeding with reading `Storage:DbPath` from config (default `ametek-watch.db`) and registering `SqliteFindingStore(dbPath)` as `IFindingStore`. Top-level program no longer async (the only `await` was the removed seed). Endpoints and localhost/read-only behaviour unchanged. |
| `tests/AmetekWatch.Web.Tests/AmetekWatch.Web.Tests.csproj` | Added explicit `ProjectReference` to `AmetekWatch.Storage` (used directly to seed the temp DB). |
| `tests/AmetekWatch.Web.Tests/FindingsApiTests.cs` | Rewritten to drive the real app against a temp SQLite DB seeded via `SqliteFindingStore`, overriding `Storage:DbPath` via the factory. 2 tests (seeded most-recent-first; empty DB → `[]`). Removed the 3 obsolete in-memory-seed tests. |
| `CLAUDE.md` | Appended a dated `### Unreleased` entry (below existing ones; none edited). |
| `wiki/reports/report-spec017-CC-dashboard-reads-sqlite.md` | This report. |

## 3. Spec-specific detail

### Program wiring (`src/AmetekWatch.Web/Program.cs`)
- `var dbPath = builder.Configuration["Storage:DbPath"] ?? "ametek-watch.db";`
- `builder.Services.AddSingleton<IFindingStore>(new SqliteFindingStore(dbPath));`
- The in-memory `InMemoryFindingStore` + `SweepRunner`/`FakeSearcher`/`FakeTriageDecider` startup seed
  is removed entirely. No Anthropic/HTTP dependency added; the sweep host (015), non-Web source, and the
  `.sln` are untouched.

### Empty-DB `[]` case (confirmed)
`SqliteFindingStore`'s constructor runs `EnsureSchema()` (`CREATE TABLE IF NOT EXISTS findings …`), so a
missing or empty DB file is schema-initialised on first connection and `GetAllAsync` returns an empty
list rather than crashing. Test `GetFindings_FreshEmptyDb_ReturnsEmptyArray` points the app at a
schema-only temp DB (never written) and asserts `GET /api/findings` returns `[]` (`Assert.Empty`). Green.

### Tests (`tests/AmetekWatch.Web.Tests/FindingsApiTests.cs`)
- `TempDbFactory : WebApplicationFactory<Program>` overrides `Storage:DbPath` via
  `IHostBuilder.ConfigureHostConfiguration` (see Surprises — `ConfigureAppConfiguration` is too late).
- `GetFindings_ReturnsSeededFindings_MostRecentDiscoveredFirst`: three findings seeded **out of**
  DiscoveredAt order (a=+9h, c=+11h, b=+10h relative to anchor `2026-06-18T00:00:00Z`); oracle asserts
  the endpoint returns them most-recent-first — c (+11h), b (+10h), a (+9h) — plus field round-trip
  (DiscoveredAt, Category, WorthReporting) on the first row.
- `GetFindings_FreshEmptyDb_ReturnsEmptyArray`: empty/no-crash case above.
- Can-fail: temporarily reordered the expected URL array (a/b/c instead of c/b/a) → 1 failed, 1 passed;
  reverted to the correct oracle → 2/2 pass.

## Gate results
All commands run separately, prefixed `PATH="$HOME/.dotnet:$PATH"`. `dotnet --version` = **8.0.422**.
Clean SHA: gate ran clean at the branch tip (committed working tree).

| Gate command | Result |
| --- | --- |
| `dotnet build -c Release` | ✓ Build succeeded, **0 Warning(s), 0 Error(s)** |
| `dotnet format --verify-no-changes` | ✓ exit 0 (clean) |
| `dotnet test` | ✓ **35/35 passed**, 0 failed, 0 skipped (AmetekWatch.Tests 29 + AmetekWatch.Storage.Tests 4 + AmetekWatch.Web.Tests 2) |

- Test count **before**: 36 (AmetekWatch.Tests 29 + Storage 4 + **Web 3**).
- Test count **after**: 35 (AmetekWatch.Tests 29 + Storage 4 + **Web 2**) — net −1 because the 3 obsolete
  in-memory-seed Web tests were replaced by 2 SQLite-backed tests.
- Files changed NOT in the spec's named files-to-change list: `tests/AmetekWatch.Web.Tests/AmetekWatch.Web.Tests.csproj`
  (explicit `AmetekWatch.Storage` project reference — the test seeds via `SqliteFindingStore` directly).
  The spec named "updated `tests/AmetekWatch.Web.Tests/`"; the `.csproj` is within that project. No `.sln`
  edit, no non-Web source edit.

## Sources beyond the brief / surprises
- **Config override timing (minimal hosting).** The spec suggested
  `ConfigureAppConfiguration`/`UseSetting` to override `Storage:DbPath`. `ConfigureAppConfiguration` (via
  `IWebHostBuilder`) is applied **too late** — `Program` reads `builder.Configuration["Storage:DbPath"]`
  at build time, before the factory's app-configuration sources are merged, so the override silently
  missed and the seeded test returned 0 instead of 3. Fixed by overriding `IHostBuilder.ConfigureHostConfiguration`
  (host configuration is the base for `builder.Configuration` and is in place early). This is the only
  judgment deviation from the spec's literal suggestion; behaviour matches the spec's intent.

## Deferred / not done
- Cross-model integration and the merge to `main` — owned by CX2 (no self-merge).
- The real Anthropic pipeline, email, and dashboard-write paths remain out of scope (final/deferred specs).

## Standing flags
- None new. The offline-v1 loop (sweep persists → dashboard reads the same SQLite DB) is now wired;
  Anthropic auth remains globally deferred as charter notes.

## Roles update notice
None — no role doc edited this session.
