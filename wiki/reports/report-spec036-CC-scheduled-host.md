# Report — Spec 036-CC: Scheduled sweep host (IHostedService daemon)

**Headline outcome:** Built on branch `feature/cc-scheduled-host`, **not merged** (CX integrates; no
self-merge). `ametek-watch.exe` is now a real long-lived periodic daemon with graceful shutdown when
`Sweep:RunOnce=false`, while the default `RunOnce=true` one-shot CLI is unchanged. No version bump
(`<Version>` stays `0.1.0`, internal). Gate green on Linux .NET 8.0.422: build 0-warn, format clean,
**91/91** tests (was 89).

## 1. Branch / merge state
- Pre-merge `main` SHA (branch base): `f2ff6b866b6c223a5198875c25189aca15cbc19e` (= `origin/main`).
- Feature branch: `feature/cc-scheduled-host`; working commit: `f5cdb93aa218fc3c2362cdaf2e273adfe7515d20`
  (deliverable) + a follow-up commit adding this report. Branch deleted post-merge: **n** (CX integrates).
- Post-merge `main` SHA (pushed): **N/A — not merged** (cross-model integrator merges on PASS).
- Merge mechanic: pushed branch; **independent CX integrator** performs the gated `--no-ff` merge.
- Independent of spec 034 (branched from `origin/main`; the 034 Resilience work is not present here).

## 2. Changes
| File | Change |
| --- | --- |
| `src/AmetekWatch.App/AmetekWatch.App.csproj` | Added `Microsoft.Extensions.Hosting` **8.0.0** (pinned to 8.0.0 to match the existing 8.0.0 Configuration packages). |
| `src/AmetekWatch.App/Program.cs` | Rewritten to branch on `Sweep:RunOnce`: `true` → one-shot (unchanged behaviour); `false` → Generic-Host daemon (`Host.CreateApplicationBuilder` → register `SweepHost`+`SweepBackgroundService` → `Run()`). Uses the shared `SweepComposer`. |
| `src/AmetekWatch.App/SweepBackgroundService.cs` | **New.** `BackgroundService`; `ExecuteAsync` runs `SweepHost.RunAsync(stoppingToken)`, opens with `await Task.Yield()` (so a synchronous loop can't block host startup), logs start/stop, swallows the shutdown `OperationCanceledException`. |
| `src/AmetekWatch.App/LoggingDigestNotifier.cs` | **New.** `IDigestNotifier` decorator that logs one line per completed sweep (one digest per sweep) then delegates — satisfies "log each sweep" without touching `SweepHost`. |
| `src/AmetekWatch.App/SweepComposer.cs` | **New.** Factors the 028/032 wiring (pipeline-tier select + key-presence fallback, SQLite store, tolerant `Notify` bind, digest-sink select) into a shared `SweepComposition`/`SweepComposer.Build`. Construction only — no network, no secret. |
| `tests/AmetekWatch.Tests/SweepBackgroundServiceTests.cs` | **New.** 2 tests (daemon runs ≥2 sweeps then stops promptly on cancel; `RunOnce=true` runs exactly one sweep and returns). |
| `CLAUDE.md` | Appended a dated `### Unreleased` entry (below existing ones; none edited). |

## 3. Spec definition-of-done
- [x] `SweepBackgroundService` + `Program` host wiring — `RunOnce=true` → one-shot; `false` → hosted daemon, graceful shutdown.
- [x] `dotnet run` (default `RunOnce=true`) still runs one sweep, persists, writes digest, exit 0.
- [x] Tests: hosted service runs ≥2 sweeps on a short interval and stops on cancel; `RunOnce` path intact; can-fail confirmed.
- [x] Gate green (build/format/test, each separately, real counts). Branch pushed; tip SHA reported below.

### `dotnet run` confirmation (default `RunOnce=true`)
`PATH="$HOME/.dotnet:$PATH" dotnet run -c Release --project src/AmetekWatch.App` →
```
AMETEK Watch — sweep for "AMETEK"
Pipeline:               FAKE (deterministic; Pipeline:UseRealApi=false)
Store (SQLite):         ametek-watch.db
Digest sink:            File -> FileDigestNotifier
Persisted findings:     4
Worth-reporting digest: 3
```
Exit 0; wrote `ametek-watch-digest.md` (verified — "**3 items worth reporting.**" + 3 friendly sections)
and the SQLite DB. Both artifacts are gitignored (`git check-ignore` confirms) and were cleaned up.

### Daemon behaviour (test evidence)
- Started `SweepBackgroundService` over fakes + temp SQLite + `IntervalMinutes=0`; a counting
  `IDigestNotifier` observed **≥2 sweeps** (bounded 10s wait), then `StopAsync` returned **promptly**
  (bounded 10s) and the fakes' **4 unique** findings were persisted.
- `RunOnce=true`: `SweepHost.RunAsync` ran **exactly one** sweep and returned despite a 1440-min
  interval (no hang).

## Gate results
Run separately, prefixed `PATH="$HOME/.dotnet:$PATH"`, at working commit `f5cdb93`.
`dotnet --version` = **8.0.422**.

| Gate command | Result | Counts |
| --- | --- | --- |
| `dotnet build -c Release` | ✓ | 0 warnings, 0 errors |
| `dotnet format --verify-no-changes` | ✓ | clean (exit 0) |
| `dotnet test` | ✓ | **91 passed / 0 failed** (was **89**) |

- Test count **before**: 89 (AmetekWatch.Tests 53 + Anthropic 30 + Storage 4 + Web 2).
- Test count **after**: 91 (AmetekWatch.Tests **55** + Anthropic 30 + Storage 4 + Web 2) — **+2**.
- **Can-fail confirmed:** flipped the daemon persisted-count oracle `4 → 99` → 1 failure
  (`Expected: 99 / Actual: 4`), then reverted.
- Files changed NOT in the spec's files-to-change list: **None** beyond the `.csproj` package add and
  `CLAUDE.md`, both explicitly sanctioned by the prompt. **No `.sln` edit; no `SweepHost`/`SweepRunner`
  seam or other-project change.**

## Sources beyond the brief / surprises
- **NuGet downgrade trap.** `dotnet add package Microsoft.Extensions.Hosting` selected **10.0.9**, which
  transitively requires the Configuration packages at `>= 10.0.9` while the project pins them at `8.0.0`
  — with `TreatWarningsAsErrors=true` this is a hard **NU1605** error. Resolved by pinning Hosting to
  **8.0.0** (matches the existing 8.0.0 Configuration pins; no other package touched).
- **Generic-Host startup-blocking footgun.** `BackgroundService.StartAsync` runs `ExecuteAsync` inline
  until its first real `await`. `SweepHost.RunAsync` with `IntervalMinutes=0` over Microsoft.Data.Sqlite
  (whose async methods complete **synchronously**) never yields, so the first test hung on `StartAsync`.
  Fixed with `await Task.Yield()` at the top of `ExecuteAsync` — also the correct production guard so a
  short/zero interval can't block host startup. Did not touch `SweepHost`. (Caught by the test, fixed,
  re-verified.)
- **`dotnet test | tail` buffering.** Piping `dotnet test` through `tail` shows nothing until the run
  ends; combined with the hang above this masked the failure briefly. Switched to redirect-to-file +
  poll for the result.

## Deferred / not done
- **Windows-service / Task-Scheduler registration** and **running the exe on Windows** — out of scope
  (needs a Windows host); the daemon is the prerequisite. The single-file `win-x64` publish was not
  re-built this spec (artifact, not part of the gate).
- **Live API / live SMTP paths** — still deferred (need `ANTHROPIC_API_KEY` / SMTP creds); not exercised.
- **Distributed scheduling** — explicitly out of scope.

## Standing flags
- The capstone (028) already closed the end-to-end product loop and was flagged to the Manager as a
  candidate first user-facing milestone (0.1.0 → 1.0.0 + a `CHANGELOG`). This daemon strengthens that
  case but I made no version/`CHANGELOG` change — Manager's call.

## Roles update notice
None — no role doc edited this session.
