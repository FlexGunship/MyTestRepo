# Spec 036-CC — Scheduled sweep host (IHostedService daemon)

## Status
- Doc type: implementation (make the exe a proper long-lived periodic daemon)
- Executes: **CC**; pushes `feature/cc-scheduled-host`; **CX** integrates (cross-model); CM lands on PASS. No self-merge.
- Number 036 verified free (search `wiki/specs/`; this is highest + 1). **Independent of 034** (App-only).
- Paired prompt: prompt-spec036-CC-scheduled-host.md
- Final on-disk: `src/AmetekWatch.App/` (Program + a hosted service) + `AmetekWatch.App.csproj` (Hosting pkgs) + a test in `tests/AmetekWatch.Tests/`.

## Background
`SweepHost.RunAsync` already loops on the interval when `RunOnce=false`, but `Program` just calls it inline.
The charter wants **periodic sweeps** as a real daemon. This wraps the sweep loop in a .NET Generic Host
`IHostedService` with proper lifecycle and **graceful shutdown** (Ctrl+C / SIGTERM), so `ametek-watch.exe`
can run as a long-lived scheduler (and later as a Windows service / Task Scheduler job).

## Decisions made
1. **Add `Microsoft.Extensions.Hosting`** to `AmetekWatch.App` (touches `AmetekWatch.App.csproj`, not `.sln`).
2. **`SweepBackgroundService : BackgroundService`** (or `IHostedService`) in App — `ExecuteAsync(stoppingToken)`
   runs `SweepHost.RunAsync(stoppingToken)` (the existing interval loop), honoring the token for **graceful
   shutdown**; logs start/stop and each sweep via the host logger. Construct the `SweepHost` (with the chosen
   pipeline + store + digest notifier from the existing 028/032 wiring) inside the host's DI.
3. **`Program`** behaviour by config (preserve `RunOnce`):
   - `Sweep:RunOnce=true` (default) → run **one** sweep and exit 0 (current behaviour — keep it; the CLI/tests
     stay deterministic).
   - `Sweep:RunOnce=false` → build a Generic Host (`Host.CreateApplicationBuilder`), register
     `SweepBackgroundService`, and `Run()` it (long-lived; Ctrl+C stops gracefully).
   Do not change `SweepHost`/`SweepRunner` seams or other projects.
4. **Tests** (`tests/AmetekWatch.Tests/`): start the `SweepBackgroundService` (or host) with the **fakes**, a
   temp DB, and a **very short interval**, let it run **≥2 sweeps**, then signal the cancellation token and
   assert it **stops promptly** and that sweeps ran (findings persisted ≥ once). Keep it fast + deterministic
   (no real long delays — use a tiny interval and bounded wait). Confirm the `RunOnce=true` path still works.
   Confirm a test can fail then revert.

## Out of scope
- Windows service registration / Task Scheduler XML / running the exe on Windows (needs a Windows host). The
  live API. Distributed scheduling.

## Definition of done
- [ ] `SweepBackgroundService` + `Program` host wiring (RunOnce=true → one-shot; false → hosted daemon, graceful shutdown).
- [ ] `dotnet run` (default RunOnce=true) still runs one sweep, persists, writes digest, exit 0.
- [ ] Tests (hosted service runs ≥2 sweeps on a short interval + stops on cancel); RunOnce path intact; can-fail confirmed.
- [ ] Gate green (build/format/test, each separately, real counts). Branch pushed; tip SHA reported.

## Deliverable / report-back
See the prompt file.
