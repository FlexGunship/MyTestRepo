# Spec 001-CC — Vertical slice: solution scaffold, pipeline seams, green gate

## Status
- Doc type: implementation (thin vertical slice / toolchain bring-up)
- Executes: **CC**; merge: **integrator-merged (onboarding)** — CC pushes the branch + tip SHA; an
  independent **CX** integrator runs the gate and lands it via a separate forthcoming `002-CX-integrate`
  spec. CC does **not** self-merge.
- Number 001 verified free (wiki/specs/ was empty; highest = none; 001 = first).
- Paired prompt: prompt-spec001-CC-vertical-slice.md
- Final on-disk locations after merge: `AmetekWatch.sln`, `src/AmetekWatch.Core/`,
  `src/AmetekWatch.App/`, `tests/AmetekWatch.Tests/`, `.gitignore` (extended), `Directory.Build.props`.

## Background
AMETEK Watch is a build-mode project (see [`../product-charter.md`](../product-charter.md)): a Windows
C#/.NET exe that sweeps the web for AMETEK news via a two-tier Claude pipeline (Sonnet 4.6 searcher →
Opus 4.8 triage decider). Before any feature work, the standard build-mode opening is a **thin vertical
slice** that proves the toolchain and the gate end-to-end and establishes the core architectural seams.
This is that slice.

Two owner decisions shape it: **(a)** the Anthropic API is **not** wired yet — the pipeline lives behind
interfaces with deterministic fakes so everything builds/runs/tests **offline with no key**; **(b)** the
eventual dashboard is a local web UI (not in this slice). The dev surfaces (CC, CX) are on Linux, so the
slice must build and test **cross-platform** with the .NET SDK; the Windows `.exe` is produced as a
cross-compiled artifact (built, not executed — Windows runtime verification is explicitly deferred).

## Decisions made
1. **Solution layout** — one solution, three projects:
   - `src/AmetekWatch.Core` — class library (`net8.0`): domain types + pipeline interfaces + the
     orchestrator + the fakes/in-memory store. No I/O, no Anthropic SDK.
   - `src/AmetekWatch.App` — console host (`net8.0`, `OutputType=Exe`, `AssemblyName=ametek-watch`):
     wires one sweep and prints the digest to stdout. This is what later becomes the Windows service/UI host.
   - `tests/AmetekWatch.Tests` — **xUnit** test project referencing Core.
2. **Target framework `net8.0`** across all projects. Establish a `Directory.Build.props` at the repo
   root that sets `<LangVersion>latest</LangVersion>`, `<Nullable>enable</Nullable>`,
   `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, and the shared `<Version>0.1.0</Version>` —
   this `<Version>` is the **single live version source** future ships bump from (never hardcode a
   version elsewhere).
3. **Domain types** (records in `AmetekWatch.Core.Model`):
   - `enum FindingCategory { OpinionSocial, FinancialReport, Other }`
   - `Finding` — `string Url`, `string Title`, `string Snippet`, `DateTimeOffset? PublishedAt`,
     `FindingCategory Category`, `DateTimeOffset DiscoveredAt`. URL is the dedupe identity.
   - `TriageVerdict` — `bool Important`, `bool Relevant`, `bool WorthReporting`, `string Rationale`.
   - `TriagedFinding` — pairs a `Finding` with its `TriageVerdict`.
   - `SweepQuery` — at minimum `string Subject` (e.g. `"AMETEK"`) and an optional `int MaxResults`.
4. **Pipeline seams** (interfaces in `AmetekWatch.Core.Pipeline`):
   - `ISearcher` — `Task<IReadOnlyList<Finding>> SweepAsync(SweepQuery query, CancellationToken ct)`
     (the Sonnet 4.6 tier).
   - `ITriageDecider` — `Task<TriageVerdict> JudgeAsync(Finding finding, CancellationToken ct)`
     (the Opus 4.8 tier).
   - `IFindingStore` — `Task SaveAsync(TriagedFinding tf, CancellationToken ct)` and
     `Task<IReadOnlyList<TriagedFinding>> GetAllAsync(CancellationToken ct)`.
5. **Orchestrator** — `SweepRunner` (depends on the three interfaces): `RunAsync(SweepQuery)` →
   search → **dedupe by `Url`** (first occurrence wins) → triage each surviving finding → persist
   **every** triaged finding to the store → return a **digest** = the triaged findings where
   `WorthReporting == true`, ordered most-recent-`DiscoveredAt` first. Persistence keeps everything;
   the digest is the worth-reporting subset.
6. **Fakes** (in Core, for the slice and for tests): `FakeSearcher` returns a fixed, deterministic list
   (include at least one duplicate URL, one `OpinionSocial`, one `FinancialReport`, one `Other`);
   `FakeTriageDecider` returns deterministic verdicts by a simple stated rule (e.g. `OpinionSocial` and
   `FinancialReport` → `WorthReporting = true`; `Other` → `false`); `InMemoryFindingStore`.
7. **App behavior** — `Program` constructs the runner from the fakes, runs one sweep for
   `Subject = "AMETEK"`, and prints a readable digest (count persisted, count worth reporting, then each
   worth-reporting finding's category/title/url/rationale). Exit code 0 on success.
8. **No git tag / no CHANGELOG for this slice** — it is internal bring-up, not a user-visible release
   (CHANGELOG.md is created at the first user-facing ship). Update `CLAUDE.md` Status & Roadmap as the
   ship step; leave `<Version>` at `0.1.0`.

The developer implements as stated unless they find a strong reason to deviate — in which case they
flag it in the report rather than silently choosing otherwise.

## Scope — what to build
- The solution + three projects + `Directory.Build.props` + `.gitignore` extension (add `bin/`, `obj/`,
  `dist/`, `*.user`).
- The domain types, interfaces, `SweepRunner`, fakes, and in-memory store (all in Core).
- The console `Program` that runs one fake sweep and prints the digest.
- **Tests (xUnit), real assertions that can fail** — at minimum:
  - dedupe: a searcher returning a duplicate URL yields one persisted finding for that URL;
  - digest filter: only `WorthReporting == true` findings appear in the digest, and all findings
    (including non-worth-reporting) are persisted;
  - ordering: digest is sorted most-recent-`DiscoveredAt` first;
  - a runner happy-path over `FakeSearcher` asserting **exact** expected counts/values computed by hand
    (independent oracle — not "whatever the code returns").
- Confirm the Windows artifact builds: `dotnet publish -c Release -r win-x64 --self-contained
  -p:PublishSingleFile=true -o dist` produces `dist/ametek-watch.exe`. **Built, not executed.**

## Out of scope
- The real Anthropic SDK, any network call, any API key. (Seams + fakes only.)
- SQLite persistence — `IFindingStore` exists; only the in-memory impl ships here. (Later spec.)
- The web-UI dashboard, scheduling/cadence, email hooks, the triage rubric content. (Later specs.)
- Executing the Windows `.exe` (no Windows runtime on the dev host) and any CI config.
- Real web_search wiring, prompt-cache plumbing — deferred to the SDK-wiring spec.

## Working model
(Detail in the prompt file — prompt-spec001-CC-vertical-slice.md.)

## Definition of done
- [ ] Solution + three projects + `Directory.Build.props` + `.gitignore` extension exist as specified.
- [ ] Domain types, interfaces, `SweepRunner`, fakes, in-memory store implemented in Core.
- [ ] `dotnet run --project src/AmetekWatch.App` runs one fake sweep and prints a digest; exit 0.
- [ ] `dotnet publish … -r win-x64 …` produces `dist/ametek-watch.exe` (artifact built, not committed).
- [ ] Tests present with assertions that can fail, covering dedupe / digest-filter / ordering / counts.
- [ ] Gate green: `dotnet build -c Release`, `dotnet format --verify-no-changes`, `dotnet test` — each
      run separately, real counts recorded.
- [ ] `CLAUDE.md` Status & Roadmap updated with the slice's landing.
- [ ] Branch pushed + tip SHA reported (CC does not self-merge). Gate green. (Merged by CX via 002.)

## Deliverable / report-back
See the prompt file for the report-back format.
