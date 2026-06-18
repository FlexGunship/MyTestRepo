# Spec 007-CC — SQLite IFindingStore

## Status
- Doc type: implementation (persistence layer behind the existing seam)
- Executes: **CC**; merge: integrator-merged — CC pushes `feature/cc-sqlite-store`; **CX2** integrates
  (cross-model) and issues VERDICT; CM lands on PASS. CC does not self-merge.
- Number 007 verified free (highest spec file = 006; 007 = 006 + 1).
- Paired prompt: prompt-spec007-CC-sqlite-store.md
- Final on-disk: `src/AmetekWatch.Storage/` + `tests/AmetekWatch.Storage.Tests/` + `.sln` updated.

## Background
The 001 slice shipped `IFindingStore` with only an in-memory implementation. This adds a durable
**SQLite** implementation behind that same interface, so sweeps persist across runs. Offline/no-auth —
pure local persistence; the Anthropic pipeline is untouched.

## Decisions made
1. **New class library `src/AmetekWatch.Storage`** (`net8.0`), references `AmetekWatch.Core`, NuGet
   `Microsoft.Data.Sqlite`. Add it to `AmetekWatch.sln` (append, do not reorder existing entries).
2. **`SqliteFindingStore : IFindingStore`** — constructor takes a DB file path (or connection string).
   On first use it creates the schema if absent (a single `findings` table keyed by `Url` TEXT PRIMARY
   KEY, columns for title, snippet, published_at (nullable), category, discovered_at, important,
   relevant, worth_reporting, rationale). `SaveAsync` **upserts by `Url`** (`INSERT … ON CONFLICT(url)
   DO UPDATE`). `GetAllAsync` returns all `TriagedFinding`s ordered by `discovered_at` **descending**.
   Store dates as ISO-8601 text (round-trip `DateTimeOffset`), category as its enum name.
3. **Do not modify `AmetekWatch.App`** in this spec (selecting SQLite at runtime is a later wiring
   spec). Keep this change confined to the new Storage project + its tests + the `.sln` entry.
4. **Tests** — new xUnit project `tests/AmetekWatch.Storage.Tests` (own project, not the slice's test
   project, to avoid cross-unit conflicts). Use a **temp file** DB (not `:memory:` shared-cache pitfalls
   unless you handle them). Assert, with hand-computed expectations: round-trip of a saved
   `TriagedFinding`; upsert replaces (same `Url` saved twice → one row, latest wins); `GetAllAsync`
   ordering is most-recent `discovered_at` first; nullable `PublishedAt` round-trips as null. Confirm a
   test can fail then revert.

## Out of scope
- Wiring SQLite into `AmetekWatch.App` / runtime selection (later spec). The web dashboard (008). The
  real Anthropic pipeline.

## Definition of done
- [ ] `src/AmetekWatch.Storage` + `SqliteFindingStore` implemented; added to `.sln`.
- [ ] `tests/AmetekWatch.Storage.Tests` with the assertions above (can-fail confirmed).
- [ ] Gate green: `dotnet build -c Release`, `dotnet format --verify-no-changes`, `dotnet test` (each
      separately; report real counts). Branch pushed; tip SHA reported.

## Deliverable / report-back
See the prompt file.
