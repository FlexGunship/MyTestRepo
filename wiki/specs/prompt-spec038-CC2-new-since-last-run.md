# Prompt — Spec 038-CC2 — "New since last run" digest

You are **CC2**. Execute Spec 038-CC2 (`wiki/specs/038-CC2-new-since-last-run.md`). Read it first.
**Independent of 036** — branch from `origin/main` (which has 034's `SweepRunner`).

## Setup
- `git fetch --prune origin`; branch from origin: `git checkout -b feature/cc2-new-since-last-run origin/main`.
- Prefix every dotnet call: `PATH="$HOME/.dotnet:$PATH" dotnet …`. Confirm `dotnet --version`.

## Steps
1. `SweepRunner` (backward-compatible): add **one optional** ctor param `bool digestOnlyNew = false` (keep the
   034 params; existing construction must still compile). At the **start** of `RunAsync` (before persisting),
   snapshot the `Url`s already in the store via `IFindingStore.GetAllAsync()` (**no interface change**). A
   triaged finding is **new** iff its `Url` is not in that snapshot. **Persist all** (upsert) as today. When
   `digestOnlyNew==true`, the digest = worth-reporting **AND new**; when `false`, unchanged. Ordering unchanged.
2. Add a test to `tests/AmetekWatch.Tests/`: pre-seed the store with one worth-reporting `Url`; sweep with
   `digestOnlyNew=true` → that `Url` excluded from the digest but still persisted, new worth-reporting findings
   included; `digestOnlyNew=false` → all worth-reporting in digest. Confirm existing tests still pass (default
   false). Hand-computed; confirm a test can fail then revert.
3. Do **not** change the App/Anthropic projects, the `IFindingStore` interface, or the `.sln`.

## The gate
Each separately, real counts: `PATH="$HOME/.dotnet:$PATH" dotnet build -c Release` ; `… dotnet format --verify-no-changes` ; `… dotnet test`.

## Versioning ritual
Internal — no `CHANGELOG`/tag, `<Version>` stays `0.1.0`. Append a dated `CLAUDE.md` → `### Unreleased`
entry (below existing ones; don't edit them).

## Report-back format
Write `wiki/reports/report-spec038-CC2-new-since-last-run.md` per `wiki/rituals/report-format.md` (all sections;
"None." where N/A), plus a gate table (real counts, before/after, can-fail, clean SHA, `dotnet --version`) and
confirmation that the existing `SweepRunner`/end-to-end tests still pass with the default. Do **not** self-merge;
push `feature/cc2-new-since-last-run` and end with the tip SHA + a one-line build/format/test summary. Never print or commit secrets.
