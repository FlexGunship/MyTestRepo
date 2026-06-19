# Prompt — Spec 046-CC2 — Dashboard improvements

You are **CC2**. Execute Spec 046-CC2 (`wiki/specs/046-CC2-dashboard-improvements.md`). Read it first.
**Web-only — independent of 045.**

## Setup
- `git fetch --prune origin`; branch from origin: `git checkout -b feature/cc2-dashboard-improvements origin/main`.
- Prefix every dotnet call: `PATH="$HOME/.dotnet:$PATH" dotnet …`. Confirm `dotnet --version`.

## Steps
1. In `src/AmetekWatch.Web/Program.cs`, enrich `GET /` (server-rendered, self-contained, **all dynamic text
   HTML-escaped**): a header (title, total + worth-reporting counts, "generated `<UTC time>`"); table columns
   = friendly **Category** label (no internal enum names), **Title** linked to `Url`, **Worth reporting** (✓/—),
   **Published** + **Discovered** (friendly, "—" if null), **rationale**; a `?worthOnly=true` view (default all)
   with a toggle link; minimal inline CSS (highlight worth-reporting rows).
2. `GET /api/findings` — keep all-most-recent-first; add optional `?worthOnly=true` filter (default unchanged).
3. Read-only, localhost, no auth; read from the configured `IFindingStore` (don't change store wiring); Web-only,
   don't touch other projects or the `.sln`.
4. Tests (`tests/AmetekWatch.Web.Tests/`, `WebApplicationFactory`): assert the HTML has the summary counts +
   new columns/labels (and **no internal enum/type names** leak); `?worthOnly=true` filters both endpoints;
   ordering preserved; existing 008/017 tests pass (update if markup changed, note which). Confirm a test can fail then revert.

## The gate
Each separately, real counts: `PATH="$HOME/.dotnet:$PATH" dotnet build -c Release` ; `… dotnet format --verify-no-changes` ; `… dotnet test`.

## Versioning ritual
Internal — no `CHANGELOG`/tag, `<Version>` stays `0.1.0`. Append a dated `CLAUDE.md` → `### Unreleased`
entry (below existing ones; don't edit them).

## Report-back format
Write `wiki/reports/report-spec046-CC2-dashboard-improvements.md` per `wiki/rituals/report-format.md` (all
sections; "None." where N/A), plus a gate table (real counts, before/after, can-fail, clean SHA,
`dotnet --version`) and a note of any existing Web tests updated. Do **not** self-merge; push
`feature/cc2-dashboard-improvements` and end with the tip SHA + a one-line build/format/test summary. Never print or commit secrets.
