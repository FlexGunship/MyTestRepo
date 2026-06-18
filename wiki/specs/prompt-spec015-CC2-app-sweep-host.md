# Prompt — Spec 015-CC2 — App: config-driven sweep host with SQLite persistence

You are **CC2**. Execute Spec 015-CC2 (`wiki/specs/015-CC2-app-sweep-host.md`). Read it first.

## Setup
- `git fetch --prune origin`; branch from origin: `git checkout -b feature/cc2-app-sweep-host origin/main`.
- Prefix every dotnet call: `PATH="$HOME/.dotnet:$PATH" dotnet …`. Confirm `dotnet --version`.

## Steps
1. Add a project reference from `AmetekWatch.App` to `AmetekWatch.Storage`
   (`dotnet add src/AmetekWatch.App reference src/AmetekWatch.Storage`); add the
   `Microsoft.Extensions.Configuration[.Json/.Binder]` packages to App.
2. Add `src/AmetekWatch.App/appsettings.json` and a `SweepOptions` record per spec Decisions 2.
   Set `appsettings.json` to copy to output (`<CopyToOutputDirectory>` or content include) so it's found
   at runtime.
3. Implement `SweepHost` (App) per Decision 3 — `RunOnceAsync` and a cancellation-friendly `RunAsync`
   loop; keep it pure of hidden clocks where a test must pin behavior.
4. Update `Program` per Decision 4 — bind config, construct `SqliteFindingStore` + the fakes, run one
   sweep (`RunOnce: true` default), print the digest, exit 0. Capture the stdout for the report.
5. Add `tests/AmetekWatch.Tests/SweepHostTests.cs` to the **existing** `AmetekWatch.Tests` project (no
   `.csproj`/`.sln` edit): temp-file SQLite DB + fakes; assert persistence + digest (hand-computed).
   Confirm a test can fail then revert.
6. Do **not** add any Anthropic/HTTP dependency, touch the Web/Triage/Search source, or edit the `.sln`.

## The gate
Each separately, real counts: `PATH="$HOME/.dotnet:$PATH" dotnet build -c Release` ; `… dotnet format --verify-no-changes` ; `… dotnet test`.
Also run `PATH="$HOME/.dotnet:$PATH" dotnet run --project src/AmetekWatch.App` and capture the digest stdout.

## Versioning ritual
Internal — no `CHANGELOG`/tag, `<Version>` stays `0.1.0`. Append a dated `CLAUDE.md` → `### Unreleased`
entry (below existing ones; don't edit them).

## Report-back format
Write `wiki/reports/report-spec015-CC2-app-sweep-host.md` per `wiki/rituals/report-format.md` (all
sections; "None." where N/A), plus: the `dotnet run` digest stdout; a gate table (real counts, test
count before/after, can-fail, clean SHA, `dotnet --version`); confirm a SQLite DB file is written and that
`appsettings.json` is found at runtime. Do **not** self-merge; push `feature/cc2-app-sweep-host` and end
with the tip SHA + a one-line build/format/test summary. Never print or commit secrets.
