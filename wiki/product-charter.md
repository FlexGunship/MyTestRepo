# AMETEK Watch — Product Charter

> The authoritative **What / Who / Why / North star / Principles / Scope / Success** for this project.
> CM wrote this at first boot from the owner interview (see [`bootstrap/first-boot.md`](bootstrap/first-boot.md)).
> Keep it current as scope sharpens. **Name is a working title** — see *Open*.

## What
A Windows desktop application (`ametek-watch.exe`) that periodically sweeps the public web for fresh,
relevant material about **AMETEK, Inc.** (NYSE: AME), aggregates the findings, and surfaces the ones
worth a human's attention. Two areas get special weight: **personal/social opinion pieces** about the
company, and **financial reports** published by reputable institutions. The program runs a two-tier
LLM pipeline — a **searcher/aggregator** (Claude Sonnet 4.6) gathers and de-duplicates candidates;
a **triage decider** (Claude Opus 4.8) judges each candidate for importance, relevance, and whether
it's worth reporting.

## Who
- **Audience / users:** the owner — general AMETEK awareness monitoring (no single optimization lens;
  surface anything fresh and notable, with the two special-focus areas weighted up). A single-operator
  desktop tool, not a multi-tenant service.
- **Producers:** the team (CM + CC + CX) — see [`roles/surfaces.md`](roles/surfaces.md).

## Why
Keeping a finger on the pulse of what's being said and published about AMETEK — sentiment, opinion,
and financial reporting — is otherwise a manual, easily-missed chore. This automates the sweep and,
crucially, the **judgment**: the expensive part isn't fetching links, it's deciding which of dozens of
hits actually matter. "Done" for v1: the operator runs the app on a cadence and gets a trustworthy,
de-duplicated, triaged digest of what's new about AMETEK — with the noise already filtered out by the
decider tier, and a local record they can browse over time.

## North star
A dependable, low-maintenance personal monitoring agent for AMETEK that the owner trusts enough to
*not* check the news manually — with a persisted history that supports looking back at how coverage
and sentiment evolved. Architected so the same pipeline could later watch additional entities or push
to additional sinks (email, etc.) without a rewrite.

## Mode & gate
- **Mode:** **build** (writing software). `reference/` is not used; source lives in `src/`.
- **Gate (to be pinned in [`/CLAUDE.md`](../CLAUDE.md) — see *Open*):** a build-and-test gate, run as
  separate steps, never chained:
  ```
  dotnet build -c Release                 # compiles cleanly
  dotnet format --verify-no-changes       # style/format clean (stands in for lint)
  dotnet test                             # full suite green; assertions that can actually fail
  ```
  Single-file publish (`dotnet publish -c Release -r win-x64 --self-contained`) produces
  `dist/ametek-watch.exe`. C# is statically typed, so the typecheck step is folded into `build`.

## Technical shape (decision anchors)
- **Toolchain:** C# / .NET, native Windows executable. Official Anthropic **.NET SDK** (`dotnet add
  package Anthropic`; `AnthropicClient`, API key from the `ANTHROPIC_API_KEY` environment variable —
  never hardcoded or committed).
- **Auth deferred (owner decision).** Wiring the real Anthropic SDK + API key is **the last step**.
  Until then the pipeline lives behind interfaces (`ISearcher`, `ITriageDecider`) with deterministic
  **fake** implementations, so the entire app builds, runs, and tests **offline** with no key. The
  real SDK-backed implementations drop into those seams in a late spec without disturbing the rest.
- **Two-tier model pipeline** (exact IDs, grounded against the current API reference):
  - **Searcher / aggregator — Claude Sonnet 4.6 (`claude-sonnet-4-6`).** 1M context, 64K max output,
    $3 / $15 per 1M input/output tokens. Runs the routine sweeps.
  - **Triage decider — Claude Opus 4.8 (`claude-opus-4-8`).** 1M context, 128K max output,
    $5 / $25 per 1M input/output tokens. Judges each elevated candidate.
- **Web search:** the searcher uses Claude's **server-side `web_search` tool**
  (`WebSearchTool20260209`) — Claude issues queries, Anthropic executes them and returns results with
  citations; `MaxUses`, `AllowedDomains`/`BlockedDomains`, and `UserLocation` are available, and
  dynamic result-filtering is built into this tool version. This is preferred over scraping search
  engines directly (which violates their ToS) or wiring an external search API. *(An external search
  API behind a custom tool — Brave/Bing/SerpAPI — stays the documented fallback if specific-engine
  control is ever required; pinned at spec time.)*
- **Storage & output:** a **local database (SQLite)** of findings + verdicts, with a small local
  **web-UI dashboard** (served by the exe over localhost) for browsing and trend-tracking over time.
  Built with **delivery hooks left open for adding email** later (the digest is a content artifact the
  decider produces; the sink is pluggable).
- **Scheduling:** periodic sweeps on a configurable cadence (mechanism — built-in timer vs. Windows
  Task Scheduler — pinned at spec time). Default cadence TBD with owner (likely daily).
- **Cost lever:** the stable triage instruction (rubric + AMETEK context) is a **prompt-cache** prefix
  on the Opus calls; per-candidate content goes after the cache breakpoint. Opus 4.8 needs a ≥4096-token
  cached prefix to cache; within a single sweep the cache stays warm across back-to-back triage calls
  (cache reads ≈0.1× input price), so a batch of triage calls largely pays the cached portion once.
  Web-search server-tool usage is billed separately from token costs.

## Principles
1. **Judgment is the product.** The decider tier exists to cut noise; a short, correct digest beats a
   long unfiltered one. Relevance/importance criteria are explicit and live in the triage prompt.
2. **Public sources only; respect platform ToS.** Use sanctioned search (Claude's web-search tool or a
   licensed search API), honor `robots.txt`/ToS, and never harass or target individuals — opinion
   pieces are aggregated as public sentiment, not as dossiers on people.
3. **Verify against reality, not the previous implementation.** Tests use real, representative inputs
   and assert against independent expected values; "matches the old run" is not an oracle.
4. **No secrets in the repo.** API keys via environment only; never printed, never committed.
5. **Accuracy over volume; `main` always green; author ≠ integrator.**

## Scope (v1 — refine as you learn)
- **In:** the periodic sweep; Sonnet 4.6 search + aggregation + de-duplication; Opus 4.8 triage with an
  explicit relevance/importance rubric weighted toward opinion/social pieces and reputable financial
  reports; persistence to a local SQLite store; a basic local dashboard to browse triaged findings;
  configurable cadence; a single Windows executable; the build-and-test gate.
- **Out (for now):** email/push delivery (only the *hooks* are in v1); monitoring entities other than
  AMETEK; multi-user/hosted deployment; auto-trading or any action on findings; sentiment scoring
  models beyond the decider's qualitative judgment.

## Success
- A fresh run produces a de-duplicated, triaged digest of genuinely new AMETEK material, with
  opinion/social pieces and reputable financial reports correctly elevated and obvious noise dropped.
- Findings persist to the local store and are browsable in the dashboard across runs.
- The gate is green and `ametek-watch.exe` builds and runs a full sweep end-to-end on Windows.
- Triage decisions are inspectable (each finding shows the decider's importance/relevance verdict).

## Resolved
- **Dashboard form** → **local web UI** served by the exe (owner, 2026-06-18). *(WPF/WinForms not used.)*
- **Anthropic auth** → **deferred to the last step** (owner, 2026-06-18); build behind fakes until then.
- **Gate pinning** → done — the build-and-test gate is pinned in [`/CLAUDE.md`](../CLAUDE.md) and the
  Status & Roadmap is started.

## Open (resolve early)
- **Name** — "AMETEK Watch" / `ametek-watch.exe` is a working title; confirm or replace.
- **API budget** *(deferred with auth, but informs sizing)* — rough acceptable cost per sweep, to size
  the searcher's search-tool `MaxUses` and the triage batch size when the real SDK is wired.
- **Cadence & scheduling mechanism** — default sweep interval; built-in timer vs. Task Scheduler.
- **Reputable-institution list** — which financial-report sources count as "reputable" (seed list for
  the triage rubric).

## Changelog
- 2026-06-18 — Resolved dashboard form (local web UI) and deferred Anthropic auth to the final step
  (build behind fakes until then); pinned the gate + started Status & Roadmap in `/CLAUDE.md`.
- 2026-06-18 — Charter written at first boot from the owner interview (build mode; C#/.NET Windows exe;
  Sonnet 4.6 searcher → Opus 4.8 decider; web-search server tool; SQLite + dashboard with email hooks).
