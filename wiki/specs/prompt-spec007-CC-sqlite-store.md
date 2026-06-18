# Prompt — Spec 007-CC — SQLite IFindingStore

You are **CC**. Execute Spec 007-CC (`wiki/specs/007-CC-sqlite-store.md`). Read it first.

## Setup
- `git fetch --prune origin`. You **cannot** `git checkout main` (repo-master holds it); branch from
  origin: `git checkout -b feature/cc-sqlite-store origin/main`.
- `export PATH="$HOME/.dotnet:$PATH"` and **prefix every dotnet call** with `PATH="$HOME/.dotnet:$PATH"`
  (shell env does not persist between commands). Confirm `dotnet --version` (≥ 8).

## Steps
1. `dotnet new classlib -n AmetekWatch.Storage -o src/AmetekWatch.Storage`; add reference to
   `src/AmetekWatch.Core`; add NuGet `Microsoft.Data.Sqlite`. `dotnet sln AmetekWatch.sln add
   src/AmetekWatch.Storage/AmetekWatch.Storage.csproj`.
2. Implement `SqliteFindingStore : IFindingStore` per spec Decision 2 (schema-on-init, upsert by `Url`,
   `GetAllAsync` ordered most-recent `discovered_at` first; ISO-8601 dates; category as enum name).
3. `dotnet new xunit -n AmetekWatch.Storage.Tests -o tests/AmetekWatch.Storage.Tests`; reference Storage
   (+ Core); `dotnet sln add` it. Write the tests from spec Decision 4 using a temp-file DB; **hand-compute
   the expected values**. Confirm a test can fail (invert one assertion, run, observe, revert).
4. Do **not** touch `AmetekWatch.App` or the slice's test project.

## The gate
Run each separately, capture real counts:
`PATH="$HOME/.dotnet:$PATH" dotnet build -c Release` ; `… dotnet format --verify-no-changes` ;
`… dotnet test`.

## Versioning ritual
Internal/no user-facing release — no `CHANGELOG`, no tag, `<Version>` stays `0.1.0`. Append a dated
`CLAUDE.md` → `### Unreleased` entry recording the SQLite store (add below existing entries; don't edit them).

## Report-back format
Write `wiki/reports/report-spec007-CC-sqlite-store.md` per `wiki/rituals/report-format.md` (all
load-bearing sections; "None." where N/A), plus:
- **Gate table:** 3 commands → ✓/✗ with real counts; test count before/after; can-fail check; clean SHA;
  `dotnet --version`.
- **Files changed** incl. the `.sln` line you added.
Do **not** self-merge; push `feature/cc-sqlite-store` and end your final message with the tip SHA + a
one-line gate summary (build/format/test pass-fail). Never print or commit secrets.
