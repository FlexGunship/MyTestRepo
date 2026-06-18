# CLAUDE.md — AMETEK Watch

> **AMETEK Watch** — a Windows C#/.NET app that periodically sweeps the web for fresh, relevant
> material about AMETEK, Inc. (NYSE: AME), weighted toward personal/social opinion pieces and
> reputable financial reports. A two-tier Claude pipeline does the work: **Sonnet 4.6** searches and
> aggregates (via the server-side `web_search` tool); **Opus 4.8** triages each finding for
> importance, relevance, and whether it's worth reporting. The authoritative What/Who/Why/Scope is the
> [product charter](wiki/product-charter.md).

This file is the **dev-facing onboarding pointer** and the canonical **Status & Roadmap**. Every agent
reads it at the start of every working session, after its role doc.

## Read this first, every session
1. Your role doc in [`wiki/roles/`](wiki/roles/) (CM / CC / CX).
2. The other agents' role docs in the same folder (knowing your counterparts keeps the
   plans-vs-executes separation clean).
3. The most recent passdown in [`wiki/passdowns/`](wiki/passdowns/).
4. [`wiki/best-practices.md`](wiki/best-practices.md), the [git & gates contract](wiki/contracts/git-and-gates.md),
   and the relevant [rituals](wiki/rituals/).
5. Your own learnings file in [`wiki/surfaces/`](wiki/surfaces/README.md), and append to it at run end.
6. Open questions in [`wiki/qanda/`](wiki/qanda/).
7. This file's **Status & Roadmap** below; and the operating manual [`wiki/README.md`](wiki/README.md).

## The gate
`main` is the single integration trunk and is **always green**. The only path to `main` for a
deliverable is a **gated `--no-ff` merge from a feature branch**, performed by an **independent
cross-model integrator** (author ≠ integrator). The gate is **flexible — it is whatever proves this
project correct**. **Mode: build** (C#/.NET). Pinned gate — run each command **separately, never
chained** (a chain hides which step failed):

```bash
# Build-and-test gate for AMETEK Watch (C#/.NET). Run each SEPARATELY.
dotnet build -c Release              # compiles cleanly; the compiler is the typecheck
dotnet format --verify-no-changes    # style/format clean (stands in for lint)
dotnet test                          # full suite green; assertions that can actually fail
```

The Windows deliverable is produced by a single-file publish (artifact, not part of the gate):

```bash
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o dist
# -> dist/ametek-watch.exe
```

Doc-only ships (specs, prompts, reports, Q&A, wiki edits) go straight to `main` (no review gate);
deliverables go branch → gate → **cross-model** integrate (author ≠ integrator). There is no docs/
`doc_check.py` gate on product code — `doc_check.py` only guards the wiki.

## Status & Roadmap
> Dev-facing changelog. ~5–15 lines per entry. Updated as a step in every ship. (User-facing notes,
> if the project releases a product, go in `CHANGELOG.md`.)

### Unreleased
- 2026-06-18 — **Bring-up.** Project chartered as *AMETEK Watch* (see
  [`wiki/product-charter.md`](wiki/product-charter.md)): a Windows C#/.NET exe that periodically sweeps
  the web for AMETEK news (opinion/social pieces + reputable financial reports), two-tier Claude
  pipeline — Sonnet 4.6 (`claude-sonnet-4-6`) searches/aggregates via the server-side `web_search`
  tool, Opus 4.8 (`claude-opus-4-8`) triages. Storage: local SQLite + a local **web-UI** dashboard;
  email delivery hooks left open (out of v1). CC + CX surfaces onboarded. **Gate pinned**
  (dotnet build / format / test). **Anthropic API auth is deferred** — the pipeline is built behind
  interfaces with deterministic fakes until a late wiring spec, so the whole system is buildable and
  testable offline now. Spec **001-CC** (vertical slice: solution scaffold + pipeline seams + green
  gate) authored and ready to dispatch.
- 2026-06-18 — **Spec 001-CC vertical slice landed (on branch; integrator-merged via 002-CX).** The
  `AmetekWatch` solution now exists: `AmetekWatch.Core` (domain records + `ISearcher`/`ITriageDecider`/
  `IFindingStore` seams + `SweepRunner` with dedupe-by-`Url` and a worth-reporting digest + deterministic
  fakes/in-memory store), `AmetekWatch.App` console host (`AssemblyName=ametek-watch`) that runs one fake
  AMETEK sweep and prints the digest, and an xUnit suite (7 tests, hand-computed oracles for dedupe /
  digest-filter / ordering / counts). Root `Directory.Build.props` pins `net8.0`, `Nullable=enable`,
  `TreatWarningsAsErrors=true`, and the single live `Version=0.1.0`. Gate green on Linux .NET 8.0.422:
  `dotnet build -c Release` (0 warn), `dotnet format --verify-no-changes`, `dotnet test` (7/7). Windows
  `dist/ametek-watch.exe` cross-compiled (`win-x64`, self-contained, single-file) — built, not executed
  (Windows runtime verification deferred). No Anthropic SDK / network dependency yet (auth still deferred).
