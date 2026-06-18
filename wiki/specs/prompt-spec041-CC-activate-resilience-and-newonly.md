# Prompt — Spec 041-CC — Activate retry + new-only in the live pipeline

You are **CC**. Execute Spec 041-CC (`wiki/specs/041-CC-activate-resilience-and-newonly.md`). Read it first.
**Invoke the `/claude-api` skill** for the SDK's C# exception types when writing `AnthropicTransient`.

## Setup
- `git fetch --prune origin`; branch from origin: `git checkout -b feature/cc-activate-resilience origin/main`.
- Prefix every dotnet call: `PATH="$HOME/.dotnet:$PATH" dotnet …`. Confirm `dotnet --version`.

## Steps
1. Add `src/AmetekWatch.Anthropic/AnthropicTransient.cs` — `static bool IsTransient(Exception)`: transient =
   rate-limit (429) / overloaded (529) / 5xx / network/timeout (`HttpRequestException`,
   `TaskCanceledException`/`TimeoutException`); non-API/argument/parse → not transient. Consult `/claude-api`
   for the SDK exception types; conservative + documented.
2. Add config `Pipeline:Retry:{MaxAttempts:3, BaseDelayMs:500}` + `Sweep:OnlyReportNew:false` to
   `appsettings.json` + options records.
3. In `SweepComposer` (036): `UseRealApi==true` → `new RetryPolicy(MaxAttempts, BaseDelay,
   AnthropicTransient.IsTransient)`; else `NoRetryPolicy`. Build `SweepRunner` with that retry policy,
   `digestOnlyNew: Sweep.OnlyReportNew`, and an `onTriageError` that logs the skipped finding. Don't change
   the `SweepRunner`/`SweepHost` seams or notifier impls.
4. Tests: `AnthropicTransient` cases you can construct (`HttpRequestException`→true; `ArgumentException`/
   `FormatException`→false); a `SweepComposer` test (`UseRealApi=false` → `NoRetryPolicy`, honors
   `OnlyReportNew`) — offline, no network/key. Existing `SweepComposer`/daemon/end-to-end tests still pass.
   Confirm a test can fail then revert.
5. Run `PATH="$HOME/.dotnet:$PATH" dotnet run --project src/AmetekWatch.App` (default fakes) — confirm one sweep + digest written.

## The gate
Each separately, real counts: `PATH="$HOME/.dotnet:$PATH" dotnet build -c Release` ; `… dotnet format --verify-no-changes` ; `… dotnet test`.

## Versioning ritual
Internal — no `CHANGELOG`/tag, `<Version>` stays `0.1.0`. Append a dated `CLAUDE.md` → `### Unreleased`
entry (below existing ones; don't edit them).

## Report-back format
Write `wiki/reports/report-spec041-CC-activate-resilience-and-newonly.md` per `wiki/rituals/report-format.md`
(all sections; "None." where N/A), plus the `dotnet run` confirmation, the resolved transient-exception types
used, and a gate table (real counts, before/after, can-fail, clean SHA, `dotnet --version`). Do **not**
self-merge; push `feature/cc-activate-resilience` and end with the tip SHA + a one-line build/format/test
summary. Never print or commit secrets.
