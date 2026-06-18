# Prompt — Spec 036-CC — Scheduled sweep host (IHostedService daemon)

You are **CC**. Execute Spec 036-CC (`wiki/specs/036-CC-scheduled-host.md`). Read it first.
**Independent of 034** — branch from `origin/main`.

## Setup
- `git fetch --prune origin`; branch from origin: `git checkout -b feature/cc-scheduled-host origin/main`.
- Prefix every dotnet call: `PATH="$HOME/.dotnet:$PATH" dotnet …`. Confirm `dotnet --version`.

## Steps
1. `dotnet add src/AmetekWatch.App package Microsoft.Extensions.Hosting`.
2. Add `SweepBackgroundService : BackgroundService` in App — `ExecuteAsync(stoppingToken)` runs
   `SweepHost.RunAsync(stoppingToken)` (the existing interval loop), honoring the token for **graceful
   shutdown**; log start/stop + each sweep. Build the `SweepHost` from the existing 028/032 pipeline+store+
   digest wiring inside the host's DI.
3. Update `Program`: `Sweep:RunOnce=true` (default) → one sweep + exit 0 (current behaviour, keep it);
   `RunOnce=false` → `Host.CreateApplicationBuilder`, register `SweepBackgroundService`, `Run()` (long-lived,
   Ctrl+C stops gracefully). Don't change `SweepHost`/`SweepRunner` seams or other projects.
4. Add a test to `tests/AmetekWatch.Tests/`: start the hosted service with **fakes** + temp DB + a **very
   short interval**, let it run **≥2 sweeps**, cancel, assert it **stops promptly** and sweeps ran (findings
   persisted). Fast + deterministic (tiny interval, bounded wait — no long real delays). Confirm the
   `RunOnce=true` path still works. Confirm a test can fail then revert.
5. Run `PATH="$HOME/.dotnet:$PATH" dotnet run --project src/AmetekWatch.App` (default RunOnce=true) — confirm one sweep + digest written.

## The gate
Each separately, real counts: `PATH="$HOME/.dotnet:$PATH" dotnet build -c Release` ; `… dotnet format --verify-no-changes` ; `… dotnet test`.

## Versioning ritual
Internal — no `CHANGELOG`/tag, `<Version>` stays `0.1.0`. Append a dated `CLAUDE.md` → `### Unreleased`
entry (below existing ones; don't edit them).

## Report-back format
Write `wiki/reports/report-spec036-CC-scheduled-host.md` per `wiki/rituals/report-format.md` (all sections;
"None." where N/A), plus the `dotnet run` confirmation and a gate table (real counts, before/after, can-fail,
clean SHA, `dotnet --version`). Do **not** self-merge; push `feature/cc-scheduled-host` and end with the tip
SHA + a one-line build/format/test summary. Never print or commit secrets.
