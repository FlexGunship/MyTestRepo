# Report — Spec 015-CC2: App config-driven sweep host with SQLite persistence

**Headline outcome:** Built on branch `feature/cc2-app-sweep-host`; **NOT merged** (CX integrates,
cross-model; no self-merge). No version bump — `<Version>` stays `0.1.0` (internal). Gate green on
Linux .NET 8.0.422: `build` (0 warn) / `format` / `test` (36/36). **One judgment call flagged:** the
existing `AmetekWatch.Tests.csproj` gained a single `ProjectReference` to `AmetekWatch.App` — the
spec's "no .csproj edit" wording cannot be met literally because `SweepHost` lives in App and the test
project previously referenced only Core (details in §"Sources beyond the brief / surprises").

## 1. Branch / merge state
- Pre-merge `main` SHA (base = `origin/main`): `3d0caa9d2a9cb8196a283ab5cad629dbb3732f2b`
- Feature branch: `feature/cc2-app-sweep-host`
- Working commit(s):
  - `bbfa99be2f1345e64877ce6eb07f5382f609b887` — implementation + CLAUDE.md + .gitignore
  - report commit — this file (tip; see push line at the bottom)
- Branch deleted post-merge: n/a (not merged)
- Merge mechanic: **pushed branch; integrator (CX) merges.** No self-merge.

## 2. Changes
| File | Change |
| --- | --- |
| `src/AmetekWatch.App/AmetekWatch.App.csproj` | Add ProjectReference to `AmetekWatch.Storage`; add `Microsoft.Extensions.Configuration[.Json/.Binder]` 8.0.0; copy `appsettings.json` to output. |
| `src/AmetekWatch.App/appsettings.json` | **New.** `Sweep:{Subject:"AMETEK",IntervalMinutes:1440,RunOnce:true}`, `Storage:{DbPath:"ametek-watch.db"}`. |
| `src/AmetekWatch.App/SweepOptions.cs` | **New.** `record` (Subject/IntervalMinutes/RunOnce) bound from the `Sweep` section; shipped-config defaults. |
| `src/AmetekWatch.App/SweepHost.cs` | **New.** `SweepHost(ISearcher, ITriageDecider, IFindingStore, SweepOptions)`; `RunOnceAsync` (one `SweepRunner` sweep → persist → digest) + cancellation-friendly `RunAsync` loop. |
| `src/AmetekWatch.App/Program.cs` | Rewritten: bind config → `SweepOptions`, construct `SqliteFindingStore(DbPath)` + fakes, `RunOnceAsync`, print digest, `return 0`. |
| `tests/AmetekWatch.Tests/AmetekWatch.Tests.csproj` | Add a single ProjectReference to `AmetekWatch.App` (Storage + Core transitive). No new project, no `.sln` edit. |
| `tests/AmetekWatch.Tests/SweepHostTests.cs` | **New.** 4 tests over a temp-file SQLite DB + fakes; hand-computed oracles. |
| `.gitignore` | Exclude the local `ametek-watch.db` runtime store (+ `*.db-shm`/`*.db-wal`). |
| `CLAUDE.md` | Appended a dated `### Unreleased` entry (below existing ones; none edited). |

## 3. `dotnet run` digest stdout
`PATH="$HOME/.dotnet:$PATH" dotnet run --project src/AmetekWatch.App` (exit 0):

```
AMETEK Watch — sweep for "AMETEK"
Store (SQLite):         ametek-watch.db
Persisted findings:     4
Worth-reporting digest: 3

[1] FinancialReport — AMETEK reports Q2 earnings beat
    url:       https://ir.example.com/ametek-q2-earnings
    rationale: Category FinancialReport is reportable under the slice rule.
[2] OpinionSocial — AMETEK shares climb on upbeat analyst note
    url:       https://news.example.com/ametek-analyst-note
    rationale: Category OpinionSocial is reportable under the slice rule.
[3] FinancialReport — AMETEK files Form 10-Q
    url:       https://sec.example.com/ametek-10q
    rationale: Category FinancialReport is reportable under the slice rule.
```

Hand-computed oracle (from `FakeSearcher.Canned`, 5 entries in discovery order): url-a OpinionSocial
09:00 → kept/worth; url-b FinancialReport 10:00 → kept/worth; url-c Other 11:00 → kept/**not** worth;
url-a 12:00 → **dropped** (duplicate URL, first wins); url-d FinancialReport 08:00 → kept/worth.
⇒ **4 persisted**, **3 worth-reporting**, digest order by `DiscoveredAt` desc = **url-b, url-a, url-d**.
Matches stdout exactly.

## 4. Runtime confirmations
- **`appsettings.json` found at runtime:** copied to `src/AmetekWatch.App/bin/Release/net8.0/appsettings.json`
  (147 bytes) and loaded via `SetBasePath(AppContext.BaseDirectory).AddJsonFile(... optional:false)`. Run
  succeeded (a missing/unfound file would throw, since `optional:false`).
- **SQLite DB file written:** `ametek-watch.db` (12,288 bytes) written at the process working directory
  after the run; deleted post-verification and now git-ignored so it is never committed.
- **Durability cross-check (test #4):** a second `SqliteFindingStore` re-opened over the same temp file
  reads back all 4 unique findings.

## Gate results
All commands run **separately**, each prefixed `PATH="$HOME/.dotnet:$PATH"`. `dotnet --version` =
**8.0.422**. Clean SHA: `bbfa99be2f1345e64877ce6eb07f5382f609b887`.

| Gate command | Result | Notes |
| --- | --- | --- |
| `dotnet build -c Release` | ✓ | 0 Warning(s), 0 Error(s) |
| `dotnet format --verify-no-changes` | ✓ | exit 0, no diffs |
| `dotnet test` | ✓ | **36/36 passed**, 0 failed, 0 skipped |

Test count **before**: 32 (AmetekWatch.Tests 25 + Storage.Tests 4 + Web.Tests 3).
Test count **after**: 36 (AmetekWatch.Tests **29** + Storage.Tests 4 + Web.Tests 3) — **+4** new
`SweepHostTests`.

**Can-fail confirmed:** temporarily asserted `persisted.Count == 99` in
`RunOnceAsync_PersistsFourUnique_ReturnsThreeWorthReporting` → `Assert.Equal() Failure: Expected 99,
Actual 4` → reverted to `== 4` (suite green again).

**Files changed NOT in the spec's files-to-change list:** `.gitignore` (added the `ametek-watch.db`
ignore so the runtime store is never committed — see surprises).

## Sources beyond the brief / surprises
- **The "no `.csproj` edit" constraint on the test project is not literally satisfiable, and I made a
  call.** Spec Decision 5 / the prompt say add `SweepHostTests.cs` to the existing `AmetekWatch.Tests`
  "(no `.csproj`/`.sln` edit)". But `SweepHost` (and `SweepOptions`) live in `AmetekWatch.App` per
  Decision 3, and `AmetekWatch.Tests` previously referenced **only** `AmetekWatch.Core` — so a test that
  constructs `SweepHost` cannot compile without a reference to App. I added the **single minimal**
  `ProjectReference` to `AmetekWatch.App` (which transitively brings Storage + Core, satisfying the
  spec's "temp-file SQLite" requirement too). I did **not** create a new test project and did **not**
  touch the `.sln`. I read the parenthetical's intent — paired with `.sln` and contrasted with spec 008
  which added a whole new project + appended to the `.sln` — as "reuse the existing test project, don't
  grow the project graph / solution," which this honors. **Flagging for CM/CX**: if a literal no-csproj
  rule is required, the alternative would be relocating `SweepHost` out of App (contradicts Decision 3)
  or a new test project (contradicts "existing project / no .sln edit"). I judged the one-line reference
  the least-surprising path; it is commented inline in the csproj.
- **`.gitignore` addition (out of the named file list).** `dotnet run` writes `ametek-watch.db` at the
  cwd (repo root); it showed up untracked and was **not** previously ignored. Added an ignore rule so a
  runtime store can never be accidentally committed. Minimal, defensive, reversible.
- **Package versions:** pinned the three `Microsoft.Extensions.Configuration*` packages at `8.0.0` to
  match the `net8.0` target (Storage already uses `Microsoft.Data.Sqlite` 10.0.9, referenced
  transitively by the test).

## Deferred / not done
- **Real Anthropic-backed `ISearcher`/`ITriageDecider`** — out of scope; still the deterministic fakes
  (the final deferred spec). No Anthropic/HTTP dependency added.
- **`RunAsync` long-running loop** — implemented and cancellation-friendly, but only `RunOnceAsync` is
  exercised by `Program`/tests (the loop's `Task.Delay(IntervalMinutes)` path is not unit-tested to
  avoid baking wall-clock waits into the suite; `RunOnce:true` is the shipped default).
- **Windows runtime execution** — built/run on Linux .NET 8.0.422 only; no `win-x64` publish run this
  spec (Windows verification remains deferred project-wide).
- **Web dashboard reading the SQLite DB** — explicitly out of scope (later spec).

## Standing flags
None. (No pre-existing out-of-scope blockers encountered; Web/Triage/Search source and the `.sln` were
left untouched as required.)

## Roles update notice
None — no role doc edited this session.
