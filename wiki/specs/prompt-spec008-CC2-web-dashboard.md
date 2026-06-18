# Prompt — Spec 008-CC2 — Local web-UI dashboard

You are **CC2**. Execute Spec 008-CC2 (`wiki/specs/008-CC2-web-dashboard.md`). Read it first.

## Setup
- `git fetch --prune origin`. You **cannot** `git checkout main` (repo-master holds it); branch from
  origin: `git checkout -b feature/cc2-web-dashboard origin/main`.
- `export PATH="$HOME/.dotnet:$PATH"` and **prefix every dotnet call** with `PATH="$HOME/.dotnet:$PATH"`
  (shell env does not persist). Confirm `dotnet --version` (≥ 8).

## Steps
1. `dotnet new web -n AmetekWatch.Web -o src/AmetekWatch.Web`; reference `src/AmetekWatch.Core`;
   `dotnet sln AmetekWatch.sln add src/AmetekWatch.Web/AmetekWatch.Web.csproj`.
2. In `Program`, seed an `InMemoryFindingStore` by running one fake sweep through Core
   (`FakeSearcher`+`FakeTriageDecider`+`SweepRunner`), register it as the `IFindingStore`, and map the two
   endpoints from spec Decision 3 (`GET /api/findings` JSON; `GET /` HTML table). Bind localhost.
3. `dotnet new xunit -n AmetekWatch.Web.Tests -o tests/AmetekWatch.Web.Tests`; add `Microsoft.AspNetCore.Mvc.Testing`;
   reference Core; `dotnet sln add` it. Make `Program` reachable (`public partial class Program {}`). Write
   a `WebApplicationFactory<Program>` test asserting `GET /api/findings` → 200 + expected seeded findings
   (hand-computed). Confirm a test can fail (invert one assertion, run, observe, revert).
4. Do **not** touch `AmetekWatch.App`, the slice's test project, or anything SQLite-related (that's 007).

## The gate
Run each separately, capture real counts:
`PATH="$HOME/.dotnet:$PATH" dotnet build -c Release` ; `… dotnet format --verify-no-changes` ;
`… dotnet test`.

## Versioning ritual
Internal — no `CHANGELOG`, no tag, `<Version>` stays `0.1.0`. Append a dated `CLAUDE.md` → `### Unreleased`
entry for the dashboard (add below existing entries; don't edit them).

## Report-back format
Write `wiki/reports/report-spec008-CC2-web-dashboard.md` per `wiki/rituals/report-format.md` (all
load-bearing sections; "None." where N/A), plus:
- **Gate table:** 3 commands → ✓/✗ real counts; test count before/after; can-fail check; clean SHA;
  `dotnet --version`.
- **Endpoints:** confirm the routes and what `GET /api/findings` returns (paste the JSON or a sample).
- **Files changed** incl. the `.sln` line you added.
Do **not** self-merge; push `feature/cc2-web-dashboard` and end your final message with the tip SHA + a
one-line gate summary. Never print or commit secrets.
