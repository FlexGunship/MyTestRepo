# Prompt — Spec 017-CC — Dashboard reads from the SQLite store

You are **CC**. Execute Spec 017-CC (`wiki/specs/017-CC-dashboard-reads-sqlite.md`). Read it first.

## Setup
- `git fetch --prune origin`; branch from origin: `git checkout -b feature/cc-dashboard-sqlite origin/main`.
- Prefix every dotnet call: `PATH="$HOME/.dotnet:$PATH" dotnet …`. Confirm `dotnet --version`.

## Steps
1. `dotnet add src/AmetekWatch.Web reference src/AmetekWatch.Storage`.
2. In Web `Program`, read `Storage:DbPath` from config (default `ametek-watch.db`) and register
   `SqliteFindingStore(dbPath)` as `IFindingStore`, replacing the in-memory fake seeding. Confirm a
   missing/empty DB yields `[]` (schema-on-init; no crash).
3. Keep both endpoints and localhost/read-only behaviour unchanged.
4. Rewrite `tests/AmetekWatch.Web.Tests` per spec Decision 4 — drive the app against a **temp SQLite DB**
   pre-seeded via `SqliteFindingStore`, overriding `Storage:DbPath` in the `WebApplicationFactory`; assert
   seeded findings come back most-recent-first and a fresh DB returns `[]`. Hand-computed; confirm a test
   can fail then revert. Remove the obsolete in-memory-seed test.
5. Do **not** add any Anthropic/HTTP dependency, change the sweep host (015) or non-Web source, or edit the `.sln`.

## The gate
Each separately, real counts: `PATH="$HOME/.dotnet:$PATH" dotnet build -c Release` ; `… dotnet format --verify-no-changes` ; `… dotnet test`.

## Versioning ritual
Internal — no `CHANGELOG`/tag, `<Version>` stays `0.1.0`. Append a dated `CLAUDE.md` → `### Unreleased`
entry (below existing ones; don't edit them).

## Report-back format
Write `wiki/reports/report-spec017-CC-dashboard-reads-sqlite.md` per `wiki/rituals/report-format.md` (all
sections; "None." where N/A), plus a gate table (real counts, test count before/after, can-fail, clean
SHA, `dotnet --version`) and a note confirming the empty-DB `[]` case. Do **not** self-merge; push
`feature/cc-dashboard-sqlite` and end with the tip SHA + a one-line build/format/test summary. Never print
or commit secrets.
