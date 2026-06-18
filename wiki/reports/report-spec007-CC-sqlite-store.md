# Report — Spec 007-CC: SQLite IFindingStore

**Headline outcome:** Durable SQLite persistence implemented behind the existing `IFindingStore`
seam. **Not merged** (CC does not self-merge; awaiting CX2 cross-model integration). No version bump
(`<Version>` stays `0.1.0`, internal). Branch `feature/cc-sqlite-store` pushed; gate green on Linux
.NET 8.0.422 — build (0 warn) / format / test (11/11).

## 1. Branch / merge state
- Pre-merge `main` SHA: `654efc4c4d3f763a8ca75db6ce10b42e381390e4` (origin/main at branch time).
- Feature branch: `feature/cc-sqlite-store` (branched from `origin/main`); working commit: see tip SHA
  in the final message. Branch deleted post-merge: **n** (integrator's call).
- Post-merge `main` SHA (pushed): **N/A** — not merged. CX2 integrates (author ≠ integrator); CM lands
  on PASS.
- Merge mechanic: pushed branch, integrator merges (`--no-ff`). No self-merge.

## 2. Changes
| File | Change |
| --- | --- |
| `src/AmetekWatch.Storage/AmetekWatch.Storage.csproj` | New `net8.0` class library; refs `AmetekWatch.Core`; NuGet `Microsoft.Data.Sqlite` 10.0.9. |
| `src/AmetekWatch.Storage/SqliteFindingStore.cs` | `SqliteFindingStore : IFindingStore` — schema-on-init, upsert by `Url`, `GetAllAsync` ordered `discovered_at` DESC; ISO-8601 dates, category as enum name. |
| `tests/AmetekWatch.Storage.Tests/AmetekWatch.Storage.Tests.csproj` | New xUnit project; refs Storage + Core. |
| `tests/AmetekWatch.Storage.Tests/SqliteFindingStoreTests.cs` | 4 tests over temp-file DBs with hand-computed oracles. |
| `AmetekWatch.sln` | Appended the two new project entries (`AmetekWatch.Storage`, `AmetekWatch.Storage.Tests`) under the existing `src`/`tests` solution folders. Existing entries untouched/unreordered. |
| `CLAUDE.md` | Appended a dated `### Unreleased` entry recording the SQLite store (below existing entries; none edited). |

## 3. Implementation notes (spec Decision 2 conformance)
- **Schema-on-init:** constructor calls `EnsureSchema()` → `CREATE TABLE IF NOT EXISTS findings`.
  Single table, `url TEXT PRIMARY KEY`, columns: `title`, `snippet`, `published_at` (nullable),
  `category`, `discovered_at`, `important`, `relevant`, `worth_reporting`, `rationale`.
- **Upsert by `Url`:** `SaveAsync` issues `INSERT … ON CONFLICT(url) DO UPDATE SET …` — same URL
  collapses to one row, latest save wins (verified by test).
- **Ordering:** `GetAllAsync` runs `ORDER BY discovered_at DESC` → most-recently discovered first.
  ISO-8601 round-trip text sorts lexicographically in agreement with chronological order for a fixed
  offset; tests use UTC (`+00:00`) discovered_at values so the ordering oracle is unambiguous.
- **Date encoding:** `DateTimeOffset.ToString("O")` on write; `DateTimeOffset.Parse(…, RoundtripKind)`
  on read. Null `PublishedAt` → `DBNull.Value` → read back as `null` (verified).
- **Category:** stored as `FindingCategory.ToString()` (enum name), parsed with `Enum.Parse`.

## Gate results
| Command | Result | Counts |
| --- | --- | --- |
| `dotnet build -c Release` | ✓ | 0 warnings, 0 errors (5 projects) |
| `dotnet format --verify-no-changes` | ✓ | exit 0, no changes |
| `dotnet test` | ✓ | Failed 0, Passed 11, Skipped 0 |

- Test count **before**: 7 (slice suite only). **After**: 11 (7 slice + 4 new storage). The 4 new are
  all in `AmetekWatch.Storage.Tests`; the slice suite is unchanged at 7.
- **Can-fail check:** inverted the ordering assertion in
  `GetAll_OrdersByDiscoveredAtDescending` (expected `…/oldest` at index 0) → test **failed** as
  expected (`Expected "…/oldest", Actual "…/newest"`); reverted; suite green again (4/4 storage).
- Gate ran clean at the pushed tip SHA (reported in the final message).
- `dotnet --version`: **8.0.422**.
- Files changed NOT in the spec's files-to-change list: `CLAUDE.md` (mandated by the prompt's
  versioning ritual) and this report. No product/source files outside the new Storage project + its
  tests + the `.sln` entry were touched.

## Sources beyond the brief / surprises
- **`Microsoft.Data.Sqlite` resolved to 10.0.9** (the latest), not an 8.0.x. It declares net8.0
  compatibility, restored cleanly, and builds with `TreatWarningsAsErrors=true` at 0 warnings; all
  tests pass on the bundled SQLite native library on Linux. Flagging the major-version jump for the
  Manager in case a pin to an 8.0.x line is preferred for the eventual Windows publish.
- Connection pooling: `SqliteFindingStore` opens/closes a connection per call. Tests call
  `SqliteConnection.ClearAllPools()` in `Dispose` before deleting the temp DB file so no pooled handle
  blocks cleanup.

## Deferred / not done
- **Windows runtime verification** of the SQLite native interop in a single-file `win-x64` publish —
  not exercised here (built/tested on Linux only), consistent with the slice's deferral. Named
  explicitly: the bundled `e_sqlite3` native asset under single-file publish on Windows is unverified.
- **Runtime SQLite selection in `AmetekWatch.App`** — out of scope by Decision 3 (later wiring spec).

## Standing flags
- None new. Anthropic API auth remains deferred project-wide (pre-existing, untouched here).

## Roles update notice
- None. No role doc edited this session.
