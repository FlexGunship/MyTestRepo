# Spec 017-CC — Dashboard reads from the SQLite store

## Status
- Doc type: implementation (unify dashboard + sweeper on the shared SQLite store; still offline)
- Executes: **CC**; pushes `feature/cc-dashboard-sqlite`; **CX2** integrates (cross-model); CM lands on PASS. No self-merge.
- Number 017 verified free (highest spec file = 016; 017 = 016 + 1).
- Paired prompt: prompt-spec017-CC-dashboard-reads-sqlite.md
- Final on-disk: `src/AmetekWatch.Web/` (Program + appsettings + `.csproj` ref) + updated `tests/AmetekWatch.Web.Tests/`.

## Background
Spec 008 seeded the dashboard's store **in-memory** (placeholder). Spec 015 made the sweep host
**persist to SQLite** (`ametek-watch.db`). This connects them: the dashboard now reads the **same SQLite
store**, so it displays what the sweeper actually persisted — the coherent v1 loop (sweep writes →
dashboard shows). Still offline (no Anthropic).

## Decisions made
1. **`AmetekWatch.Web` references `AmetekWatch.Storage`**; add `Microsoft.Extensions.Configuration.Binder`
   if needed (Web already has configuration via the host).
2. **Read `Storage:DbPath`** from config (default `ametek-watch.db`, matching App's `appsettings.json`).
   Register `SqliteFindingStore(dbPath)` as the `IFindingStore` (replace the in-memory fake seeding).
   `SqliteFindingStore` creates the schema on init, so a missing/empty DB yields an empty dashboard
   (no crash) — verify `GET /api/findings` returns `[]` against a fresh DB.
3. **Keep the endpoints** (`GET /api/findings` JSON most-recent-first; `GET /` HTML table) and
   localhost/read-only behaviour unchanged.
4. **Tests** — rewrite `tests/AmetekWatch.Web.Tests` to drive the app against a **temp SQLite DB**:
   pre-seed the DB via `SqliteFindingStore` with known `TriagedFinding`s, point the app's config at that
   DB (e.g. `WebApplicationFactory` `ConfigureAppConfiguration`/`UseSetting` to override `Storage:DbPath`),
   and assert `GET /api/findings` returns them most-recent-first; add a test that a fresh/empty DB returns
   `[]`. Hand-computed; confirm a test can fail then revert. (Remove the now-obsolete in-memory-seed test.)

## Out of scope
- The real Anthropic pipeline (final spec). Email. Writing from the dashboard (read-only). Changing the
  sweep host or any non-Web project source.

## Definition of done
- [ ] Web reads `Storage:DbPath` and serves findings from `SqliteFindingStore`; empty DB → `[]` (no crash).
- [ ] `tests/AmetekWatch.Web.Tests` updated (temp DB, seeded + empty cases; can-fail confirmed).
- [ ] Gate green (build/format/test, each separately, real counts). Branch pushed; tip SHA reported.

## Deliverable / report-back
See the prompt file.
