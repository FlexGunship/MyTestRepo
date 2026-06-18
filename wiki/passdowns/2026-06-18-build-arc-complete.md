# Passdown — 2026-06-18 — AMETEK Watch build arc (offline v1 complete)

**Headline:** In one autonomous CM-driven session, AMETEK Watch went from a bare charter to a **complete,
runnable, offline v1** on `main` — every spine component built by the dev roster and **cross-model
integrated** (author ≠ integrator), `main` green throughout (**36 tests**). The exe runs a real
config-driven sweep that persists to SQLite. The **only** deliberately-deferred work is wiring the real
Anthropic SDK (owner's decision: auth last).

## What's on `main` (all landed, gate green)
- **Bootstrap:** charter (`wiki/product-charter.md`), pinned C#/.NET gate in `/CLAUDE.md`, full surface
  register. Roster onboarded: **CC, CC2, CX, CX2, GB** (specs 003/005/006 + the original CC/CX dispatch).
- **001 — vertical slice:** `AmetekWatch` solution; `Core` domain records + seams (`ISearcher`,
  `ITriageDecider`, `IFindingStore`) + `SweepRunner` (search→dedupe-by-`Url`→triage→persist→worth-reporting
  digest) + deterministic fakes + in-memory store; `App` console host; `Directory.Build.props` (net8.0,
  Nullable, TreatWarningsAsErrors, single `Version=0.1.0`). 7 tests.
- **007 — SQLite store:** `AmetekWatch.Storage` / `SqliteFindingStore : IFindingStore` (schema-on-init,
  upsert by `Url`, ordered reads, faithful `DateTimeOffset` round-trip). 4 tests.
- **008 — web dashboard:** `AmetekWatch.Web` ASP.NET minimal API (`GET /api/findings`, `GET /`). 3 tests.
- **011 — triage prompt builder:** `Core/Triage/` `TriageRubric` + `TriagePromptBuilder` (pure; the system
  prompt encodes the charter weighting — special weight on opinion/social + reputable financial reports —
  and the 3 verdict dimensions). 7 tests.
- **013 — searcher logic:** `Core/Search/` `SearchQueryBuilder` + `SearchResultMapper` + `SearchResultItem`
  (pure; query construction for the two focus areas; documented constant-list category heuristic; injected
  clock). 11 tests.
- **015 — app sweep host:** `App` `SweepOptions` + `SweepHost` (`RunOnceAsync`/cancellation-friendly
  `RunAsync` loop) + `appsettings.json`; `Program` runs a config-driven sweep persisting to **SQLite**.
  Verified by running the exe: 4 persisted to `ametek-watch.db`, 3 worth-reporting. 4 tests.
- **017 — dashboard reads SQLite:** *(IN FLIGHT at write time — CC building `feature/cc-dashboard-sqlite`,
  CX2 to integrate via 018.)* Points the dashboard at the same `Storage:DbPath` so it shows what the
  sweeper persisted. If not yet landed, it's the first thing to finish next session.

## Remaining backlog (prioritized; next session)
1. **Finish 017** (dashboard ↔ shared SQLite) if still in flight.
2. **The real Anthropic pipeline — the final deferred step (needs an API key).** Wire SDK-backed
   `ISearcher` (Sonnet 4.6 + server-side `web_search` tool) and `ITriageDecider` (Opus 4.8) implementations
   behind the existing seams, consuming the 011 triage prompts + 013 search logic. Prompt-cache the stable
   triage system prompt (≥4096-tok prefix on Opus). Model IDs/SDK/pricing in [[charter-status]] and the
   charter. **Precondition: `ANTHROPIC_API_KEY` + a cost budget** (still an open charter item).
3. **Searcher heuristic refinement** — CX's 014 review noted a documented edge: a social post whose title
   contains an earnings signal classifies as `FinancialReport` (financial checks precede social). Refine
   ordering/scoring if desired.
4. **Email delivery** — the charter left hooks open; add an email sink for the digest.
5. **Scheduling / Windows packaging** — Task Scheduler or a service wrapper; verify the `win-x64`
   single-file `dist/ametek-watch.exe` actually runs on Windows (so far built, not executed).
6. **Open charter items:** confirm the project name, default sweep cadence, and the reputable-source seed
   list for the triage rubric.

## Process notes / gotchas (carried from [[agent-dispatch-toolchain]] and the prior passdown)
- **Worktree rule:** branch new work from `origin/main` (never `git checkout main` — repo-master holds it);
  integrators branch from `origin/<author-branch>` (can't check out a peer's branch).
- **Shared `~/.dotnet`** (8.0.422): prefix every dotnet call `PATH="$HOME/.dotnet:$PATH" dotnet …` (env
  doesn't persist between a surface's commands).
- **`.sln`/`CLAUDE.md` merge conflicts** are routine when two branches add projects or changelog entries in
  parallel — CM resolves at land time (union the project blocks / keep all changelog bullets), then re-runs
  the gate before pushing. Keeping per-unit tests in the *existing* test project (no new `.sln` entry)
  avoids `.sln` churn for pure-logic units.
- **Codex can transiently stall** at startup (a hung API call, state `Sl`, zero progress) — kill the
  process tree and re-dispatch; the retry has cleared it every time.
- **Numbering:** spec files on disk run 001/003/004/005/006/…/017 (002 is a gap from early onboarding-report
  naming). Highest-file + 1; never reuse.
- **Integration ceremony that worked well:** a tight self-contained codex prompt that says *act immediately*,
  re-runs the gate with a real can-fail check, runs the app where relevant, and reviews with `file:line`
  citations ending `VERDICT: PASS`/`HOLD`. Every integration this session passed with substantive reviews.
