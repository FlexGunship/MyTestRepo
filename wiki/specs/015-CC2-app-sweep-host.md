# Spec 015-CC2 — App: config-driven sweep host with SQLite persistence

## Status
- Doc type: implementation (wire the runtime — still offline/fakes; real API deferred)
- Executes: **CC2**; pushes `feature/cc2-app-sweep-host`; **CX** integrates (cross-model); CM lands on PASS. No self-merge.
- Number 015 verified free (highest spec file = 014; 015 = 014 + 1).
- Paired prompt: prompt-spec015-CC2-app-sweep-host.md
- Final on-disk: `src/AmetekWatch.App/` (Program + `SweepHost` + appsettings) + a new test file in
  `tests/AmetekWatch.Tests/`. App now references `AmetekWatch.Storage`.

## Background
The pieces exist behind seams (domain, `SweepRunner`, fakes, SQLite store, triage-prompt + searcher
logic). This wires `AmetekWatch.App` into a **runnable, config-driven sweep host** that persists to
SQLite — the first time the exe does real work end-to-end (still with the **fake** searcher/decider;
the real Anthropic-backed `ISearcher`/`ITriageDecider` remain the final deferred spec).

## Decisions made
1. **App references `AmetekWatch.Storage`** (add a project reference — touches `AmetekWatch.App.csproj`,
   not the `.sln`). Add `Microsoft.Extensions.Configuration` + `…Configuration.Json` + `…Configuration.Binder`.
2. **`appsettings.json`** in App: `Sweep: { Subject: "AMETEK", IntervalMinutes: 1440, RunOnce: true }`,
   `Storage: { DbPath: "ametek-watch.db" }`. Bound to a `SweepOptions` record.
3. **`SweepHost`** (a testable class in App): ctor takes `ISearcher`, `ITriageDecider`, `IFindingStore`,
   `SweepOptions`. `RunOnceAsync(CancellationToken)` runs one `SweepRunner` sweep for the subject,
   persisting to the injected store, and returns the digest. `RunAsync(CancellationToken)` loops:
   run-once, then if `RunOnce == false` `await Task.Delay(IntervalMinutes)` and repeat until cancelled
   (keep the loop trivial and cancellation-friendly; **no `DateTimeOffset.Now` baked into logic that a
   test needs to pin** — inject a clock or pass `discoveredAt` through where the slice already does).
4. **`Program`** builds config → `SweepOptions`; constructs `SqliteFindingStore(DbPath)` + `FakeSearcher`
   + `FakeTriageDecider`; runs `SweepHost.RunOnceAsync` (default `RunOnce: true`) and prints the digest;
   exit 0. (Keep `RunOnce` the default so the CLI terminates deterministically.)
5. **Tests** — add `tests/AmetekWatch.Tests/SweepHostTests.cs` to the existing project (no `.csproj`/`.sln`
   edit). Use a **temp-file SQLite DB** + the fakes; assert `RunOnceAsync` persists the expected findings
   to the store and returns the worth-reporting digest (hand-computed from the fakes). Confirm a test can
   fail then revert.

## Out of scope
- The real Anthropic `ISearcher`/`ITriageDecider` (final spec). Email delivery. Changing the Web dashboard
  to read the SQLite DB (later). Windows-service/Task-Scheduler packaging.

## Definition of done
- [ ] App references Storage; `appsettings.json` + `SweepOptions` + `SweepHost` + updated `Program`.
- [ ] `tests/AmetekWatch.Tests/SweepHostTests.cs` (temp DB + fakes; can-fail confirmed).
- [ ] `dotnet run --project src/AmetekWatch.App` runs one sweep, persists to SQLite, prints the digest, exit 0.
- [ ] Gate green (build/format/test, each separately, real counts). Branch pushed; tip SHA reported.

## Deliverable / report-back
See the prompt file.
