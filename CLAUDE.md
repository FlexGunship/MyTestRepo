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
- 2026-06-18 — **Spec 007-CC SQLite store landed (on branch; awaiting CX2 integration).** New class
  library `src/AmetekWatch.Storage` (`net8.0`, refs `AmetekWatch.Core`, NuGet `Microsoft.Data.Sqlite`
  10.0.9) implements `SqliteFindingStore : IFindingStore` — a durable persistence backend behind the
  existing seam. Schema-on-init (single `findings` table keyed by `url TEXT PRIMARY KEY`), `SaveAsync`
  upserts by `Url` (`INSERT … ON CONFLICT(url) DO UPDATE`), `GetAllAsync` returns all findings ordered
  by `discovered_at` **descending** (most-recently discovered first). Dates persist as ISO-8601
  round-trip text (`DateTimeOffset` "O"); `FindingCategory` persists as its enum name; null
  `PublishedAt` round-trips as SQL NULL. New xUnit project `tests/AmetekWatch.Storage.Tests` (4 tests,
  temp-file DBs, hand-computed oracles: full round-trip, upsert-replaces-latest-wins, descending
  ordering, nullable-`PublishedAt`) — can-fail confirmed and reverted. Both projects appended to
  `AmetekWatch.sln`. `AmetekWatch.App` and the slice test project untouched (runtime SQLite selection
  is a later wiring spec). Gate green on Linux .NET 8.0.422: `dotnet build -c Release` (0 warn),
  `dotnet format --verify-no-changes`, `dotnet test` (11/11 — 7 slice + 4 storage). `<Version>` stays
  `0.1.0` (internal, no user-facing release). Auth still deferred.
- 2026-06-18 — **Spec 008-CC2 local web-UI dashboard built (on branch; CX integrates).** New
  `src/AmetekWatch.Web` (`Microsoft.NET.Sdk.Web`, `net8.0`, references `AmetekWatch.Core`): an ASP.NET
  minimal-API dashboard. At startup it seeds an `InMemoryFindingStore` by running one fake sweep through
  Core (`FakeSearcher` + `FakeTriageDecider` + `SweepRunner`) and registers the populated store as
  `IFindingStore` (DI) — independent of the SQLite work (007); a later spec swaps the registration.
  Endpoints: `GET /api/findings` (JSON, most-recent `DiscoveredAt` first) and `GET /` (a minimal
  server-rendered HTML table: category, title, url, worth-reporting, rationale). Read-only, binds
  `localhost` only, no auth. New xUnit project `tests/AmetekWatch.Web.Tests` drives the real app via
  `WebApplicationFactory<Program>` (`Microsoft.AspNetCore.Mvc.Testing` 8.0.17; `Program` made
  `public partial`): 3 tests with hand-computed oracles (4 persisted after URL-dedupe, most-recent-first
  order, the 3 worth-reporting findings). Both new projects appended to `AmetekWatch.sln`. Gate green on
  .NET 8.0.422: `dotnet build -c Release` (0 warn), `dotnet format --verify-no-changes`, `dotnet test`
  (10/10 = 7 prior + 3 new). `AmetekWatch.App` and the slice test project untouched; no SQLite.
- 2026-06-18 — **Spec 011-CC triage prompt & rubric builder landed (on branch; awaiting integration).**
  New `src/AmetekWatch.Core/Triage/` with two pure-C# types (no I/O, no new NuGet, no new project):
  `TriageRubric` (a `const string SystemPrompt` — general AMETEK/AME awareness with SPECIAL WEIGHT on
  personal/social opinion pieces and reputable-institution financial reports; defines the three
  verdict dimensions Important/Relevant/WorthReporting and asks for a structured verdict = 3 booleans
  + short rationale) and `TriagePromptBuilder` (`BuildSystemPrompt()` → the rubric;
  `BuildUserContent(Finding)` → a deterministic, labelled user message rendering Category/Title/Url/
  Snippet/PublishedAt, null-safe via a `(unknown)` sentinel for absent `PublishedAt`, `O`/invariant
  date formatting, null-finding throws `ArgumentNullException`). 7 new tests appended to the existing
  `AmetekWatch.Tests` project (hand-computed oracles: weighting + 3 dimensions present, every field
  labelled, null-`PublishedAt` → `(unknown)`, determinism, null guard) — can-fail confirmed and
  reverted. `ITriageDecider`/`FakeTriageDecider`, `AmetekWatch.App`, the SQLite/Web projects, the
  `.sln`, and the test `.csproj` are all untouched. Gate green on Linux .NET 8.0.422:
  `dotnet build -c Release` (0 warn), `dotnet format --verify-no-changes`, `dotnet test` (18/18 — 7
  slice + 7 triage + 4 storage). `<Version>` stays `0.1.0` (internal). Auth still deferred.
- 2026-06-18 — **Spec 013-CC searcher query & result-mapping logic landed (on branch; awaiting integration).**
  New `src/AmetekWatch.Core/Search/` with three pure-C# types (no I/O, no clock, no new NuGet/project):
  `SearchResultItem` (a record for one raw search hit — `Url`/`Title`/`Snippet`/`DateTimeOffset?
  PublishedAt`/`string? SourceDomain`), `SearchQueryBuilder` (`BuildQueries(SweepQuery)` → an ordered,
  ordinally de-duplicated list of query strings: a general subject query plus one per focus area —
  opinion/social sentiment and reputable financial reports/earnings; trims the subject; deterministic),
  and `SearchResultMapper` (`ToFinding(SearchResultItem, DateTimeOffset discoveredAt)` → `Finding` with a
  documented category heuristic driven by explicit commented constant lists: `FinancialReport` for
  SEC/EDGAR or investor-relations domains or titles naming earnings/10-Q/10-K/annual report;
  `OpinionSocial` for op-ed/opinion/blog titles or known social domains; else `Other`. `discoveredAt` is
  injected — no `DateTimeOffset.Now`; null item throws `ArgumentNullException`). 11 new tests in
  `tests/AmetekWatch.Tests/SearcherLogicTests.cs` appended to the existing project (hand-computed oracles:
  queries cover subject + both focus areas in fixed order, de-duplicated/deterministic, subject trimmed;
  ToFinding classifies SEC/IR/earnings → FinancialReport, opinion/social → OpinionSocial, neutral → Other,
  maps all fields, uses the injected `discoveredAt`, null guard) — can-fail confirmed and reverted.
  `ISearcher`/`FakeSearcher`, `AmetekWatch.App`, the Storage/Web projects, the `.sln`, and the test
  `.csproj` are all untouched. Gate green on Linux .NET 8.0.422: `dotnet build -c Release` (0 warn),
  `dotnet format --verify-no-changes`, `dotnet test` (25/25 — 18 Core [7 slice + 11 searcher] + 4 storage
  + 3 web). `<Version>` stays `0.1.0` (internal). Auth still deferred.
- 2026-06-18 — **Spec 015-CC2 App config-driven sweep host + SQLite persistence built (on branch;
  CX integrates).** `AmetekWatch.App` is now a runnable, config-driven sweep host that does real
  end-to-end work (still with the **fake** searcher/decider — real Anthropic tiers remain the final
  deferred spec). App now references `AmetekWatch.Storage` and adds
  `Microsoft.Extensions.Configuration[.Json/.Binder]` 8.0.0. New `src/AmetekWatch.App/appsettings.json`
  (`Sweep: {Subject:"AMETEK", IntervalMinutes:1440, RunOnce:true}`, `Storage:{DbPath:"ametek-watch.db"}`,
  copied to output) binds to a new `SweepOptions` record. New `SweepHost` (ctor: `ISearcher`,
  `ITriageDecider`, `IFindingStore`, `SweepOptions`): `RunOnceAsync(ct)` drives one `SweepRunner` sweep,
  persists every triaged survivor, returns the worth-reporting digest; `RunAsync(ct)` loops run-once →
  `Task.Delay(IntervalMinutes)` until cancelled when `RunOnce==false` (cancellation-friendly; no hidden
  clock — discovery timestamps come from the searcher tier). `Program` binds config → `SweepOptions`,
  constructs `SqliteFindingStore(DbPath)` + the fakes, runs `RunOnceAsync`, prints the digest, exits 0
  (default `RunOnce:true` so the CLI terminates). New `tests/AmetekWatch.Tests/SweepHostTests.cs` (4
  tests, temp-file SQLite DB + fakes, hand-computed oracles: 4 persisted after URL-dedupe, 3
  worth-reporting, digest order url-b/url-a/url-d, Other persisted-but-not-digested, durable round-trip
  over a re-opened store) — can-fail confirmed and reverted. To compile that test the existing
  `AmetekWatch.Tests.csproj` gained a single `ProjectReference` to `AmetekWatch.App` (Storage + Core flow
  transitively) — necessary because `SweepHost` lives in App and the project previously referenced only
  Core; **no new test project and no `.sln` edit** (the spec's "no .csproj edit" wording could not be
  met literally — a SweepHost test cannot reference App without it). `.gitignore` now excludes the local
  `ametek-watch.db` runtime store. No Anthropic/HTTP deps; Web/Triage/Search source and the `.sln`
  untouched. `dotnet run --project src/AmetekWatch.App`: persisted 4, digest 3, exit 0; `appsettings.json`
  resolved from the output dir and a SQLite DB file written. Gate green on Linux .NET 8.0.422:
  `dotnet build -c Release` (0 warn), `dotnet format --verify-no-changes`, `dotnet test` (36/36 — was
  32; AmetekWatch.Tests 25→29 + 4 storage + 3 web). `<Version>` stays `0.1.0` (internal). Auth still
  deferred.
- 2026-06-18 — **Spec 017-CC dashboard reads the shared SQLite store built (on branch; CX2 integrates).**
  The web dashboard now serves findings from the **same durable SQLite store** the App sweep host (015)
  persists to, closing the offline v1 loop (sweep writes → dashboard shows). `AmetekWatch.Web` now
  references `AmetekWatch.Storage`; `Program` reads `Storage:DbPath` from config (default
  `ametek-watch.db`, matching App's `appsettings.json`) and registers `SqliteFindingStore(dbPath)` as
  the `IFindingStore` — **replacing** the in-memory fake-sweep seeding (the `InMemoryFindingStore` +
  `SweepRunner`/`FakeSearcher`/`FakeTriageDecider` startup seed is gone, and the top-level program is no
  longer async). `SqliteFindingStore`'s schema-on-init means a missing/empty DB yields `[]` rather than
  crashing. Both endpoints (`GET /api/findings` JSON most-recent-`DiscoveredAt`-first; `GET /` HTML table)
  and localhost/read-only behaviour are unchanged. `tests/AmetekWatch.Web.Tests` rewritten to drive the
  real app via `WebApplicationFactory<Program>` against a **temp SQLite DB**: the factory overrides
  `Storage:DbPath` through `IHost.ConfigureHostConfiguration` (early enough that `Program` reads it at
  build time — app-configuration sources added later are too late in the minimal-hosting model), and the
  tests pre-seed the DB via `SqliteFindingStore`. 2 tests with hand-computed oracles: three findings
  seeded out-of-order (+9h/+11h/+10h) come back most-recent-first (+11h,+10h,+9h) with fields round-tripped,
  and a fresh schema-only DB returns `[]` (the empty-DB/no-crash case). The obsolete in-memory-seed tests
  (3 of them) are removed; can-fail confirmed (broke the ordering oracle → 1 fail) and reverted. The test
  `.csproj` gained an explicit `ProjectReference` to `AmetekWatch.Storage` (used directly to seed). No
  Anthropic/HTTP deps; the sweep host (015), non-Web source, and the `.sln` are untouched. Gate green on
  Linux .NET 8.0.422: `dotnet build -c Release` (0 warn), `dotnet format --verify-no-changes`,
  `dotnet test` (35/35 — was 36; AmetekWatch.Web.Tests 3→2 + 29 Core + 4 storage). `<Version>` stays
  `0.1.0` (internal). Auth still deferred.
- 2026-06-18 — **Spec 020-CC2 searcher category-heuristic refinement landed (on branch; CX2 integrates).**
  Refined `SearchResultMapper.Classify` (013) so a **known social/opinion source domain wins over a
  financial-title signal** — addressing CX's 014 edge note: a social post whose title carries a financial
  word (e.g. a LinkedIn post titled "AMETEK earnings reaction") is opinion/social commentary *about*
  earnings, not an institutional filing. New precedence (first match wins, documented in comments): (1)
  institutional/regulator/IR source **domain** (SEC/EDGAR, `ir.`/`investor.`) → `FinancialReport`
  (authoritative regardless of title); (2) known social/blogging **domain** OR opinion/op-ed/blog **title**
  → `OpinionSocial`; (3) financial-report **title** (earnings/10-Q/10-K/annual report) from a non-social,
  non-IR source → `FinancialReport`; (4) else `Other`. Only the `Classify` ordering + XML-doc/comment
  wording changed: the public signature, the explicit commented constant lists (contents unchanged), and
  the injected-`discoveredAt` purity are all untouched. `SearchQueryBuilder`/`SearchResultItem`, non-Search
  source, the Anthropic work (019), and the `.sln` were not touched. 4 tests appended to
  `tests/AmetekWatch.Tests/SearcherLogicTests.cs` (hand-computed: flagged social-domain+earnings-title →
  `OpinionSocial`; IR-domain+opinion-title → `FinancialReport`; plain-news "AMETEK Q2 earnings" →
  `FinancialReport`; neutral → `Other`) — can-fail confirmed (flipped the flagged oracle → 1 fail) and
  reverted. **No existing 013 test expectation changed** under the new precedence. Gate green on Linux
  .NET 8.0.422: `dotnet build -c Release` (0 warn), `dotnet format --verify-no-changes`, `dotnet test`
  (39/39 — was 35; AmetekWatch.Tests 29→33 + 4 storage + 2 web). `<Version>` stays `0.1.0` (internal).
  Auth still deferred.
- 2026-06-18 — **Spec 019-CC Anthropic triage decider adapter (Opus 4.8) built (on branch; CX integrates).**
  The real `ITriageDecider` now exists — Opus 4.8 judging a `Finding` against the 011 rubric with
  structured output — built and **fully unit-tested offline**, deferring only the live network call to
  when an `ANTHROPIC_API_KEY` is present. New class library `src/AmetekWatch.Anthropic` (`net8.0`, refs
  `AmetekWatch.Core`, NuGet **`Anthropic` 12.29.1** — the official .NET SDK, resolved current). It splits
  pure logic from the one untestable line: an `IMessagesClient` seam (`Task<string>
  CreateMessageTextAsync(MessageCreateParams, ct)` → first text block) with a real
  `AnthropicMessagesClient` wrapping the SDK's `AnthropicClient` (reads `ANTHROPIC_API_KEY` from env; ~5
  lines — the **only** code not unit-tested); a pure `TriageRequestFactory` (`Build(Finding)` →
  `MessageCreateParams` with `Model.ClaudeOpus4_8`, the rubric as a cache-controlled `List<TextBlockParam>`
  system block via `CacheControlEphemeral` (prompt-caches the rubric prefix), the rendered finding as the
  user message, `OutputConfig.Format = JsonOutputFormat` whose JSON schema pins
  `{important,relevant,worthReporting:boolean, rationale:string}` all required / `additionalProperties:false`,
  `MaxTokens 1024`); a pure `TriageVerdictParser` (`Parse(string)` → `TriageVerdict`, throws
  `FormatException` on malformed/missing/mistyped JSON); and `AnthropicTriageDecider : ITriageDecider`
  (ctor `IMessagesClient` + factory + parser; `JudgeAsync` = build → call → parse). New xUnit project
  `tests/AmetekWatch.Anthropic.Tests` with a `FakeMessagesClient` (canned JSON, records request): 13 tests,
  hand-computed oracles — factory pins the model id, the system block carries the rubric text AND cache
  control, the schema exposes the four required fields, user content == `BuildUserContent`, null guard;
  parser maps known JSON and throws on garbage/missing/wrong-type/null; decider with the fake yields the
  expected verdict, sends an Opus-4.8 request, null guard. **No live API call, no hardcoded key** — the real
  `AnthropicMessagesClient` compiles but is not exercised. Both projects added to `AmetekWatch.sln`; no other
  project's source or the sweep host touched. Can-fail confirmed (flipped a decider verdict oracle → 1 fail)
  and reverted. Gate green on Linux .NET 8.0.422: `dotnet build -c Release` (0 warn), `dotnet format
  --verify-no-changes`, `dotnet test` (48/48 — was 35; +13 Anthropic). `<Version>` stays `0.1.0` (internal).
- 2026-06-18 — **Spec 025-CC2 digest notifier seam + file sink built (on branch; CX2 integrates).** New
  folder `src/AmetekWatch.Core/Notify/` adds the pluggable digest-delivery seam the charter wanted (file
  sink now, email a later drop-in) — pure C#, no new NuGet/project: `IDigestNotifier`
  (`Task NotifyAsync(IReadOnlyList<TriagedFinding> digest, CancellationToken ct)`), `FileDigestNotifier`,
  and `NullDigestNotifier` (no-op default when no sink is configured). `FileDigestNotifier` (ctor:
  output path, subject, `Func<DateTimeOffset>` timestamp provider) writes a **friendly Markdown digest** —
  a heading naming the subject + run time, the worth-reporting count, then one section per finding with
  its kind (friendly label: "Opinion / Social" / "Financial Report" / "Other"), title, link, and the
  decider's rationale ("Why it matters"). **Friendly names only** — no internal type/property/enum
  identifiers leak into the file; empty digest renders a clean "Nothing to report this run." form; the
  file is overwritten each run. The run timestamp is **injected** (provider; converted to UTC) — **no
  `DateTimeOffset.Now`** — so the output is deterministic. New
  `tests/AmetekWatch.Tests/DigestNotifierTests.cs` appended to the existing `AmetekWatch.Tests` project
  (no `.csproj`/`.sln` edit): 4 tests with hand-computed full-string oracles over a temp file — seeded
  two-item digest (exact Markdown, +02:00 stamp normalises to 14:30 UTC, asserts no internal names leak),
  empty "nothing to report" case, overwrite-replaces-prior-run, and `NullDigestNotifier` writes nothing.
  Can-fail confirmed (flipped the worth-reporting count oracle → 1 fail) and reverted. **No App/DI/SweepHost
  wiring, no email/SMTP, no Anthropic projects touched, no `.sln` edit** (all deferred to a later wiring
  spec). Gate green on Linux .NET 8.0.422: `dotnet build -c Release` (0 warn), `dotnet format
  --verify-no-changes`, `dotnet test` (56/56 — was 52; AmetekWatch.Tests 33→37 + 4 storage + 2 web + 13
  Anthropic). `<Version>` stays `0.1.0` (internal). Auth still deferred.
