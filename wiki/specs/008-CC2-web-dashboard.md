# Spec 008-CC2 — Local web-UI dashboard

## Status
- Doc type: implementation (the local web-UI dashboard — owner chose a web UI)
- Executes: **CC2**; merge: integrator-merged — CC2 pushes `feature/cc2-web-dashboard`; **CX** integrates
  (cross-model) and issues VERDICT; CM lands on PASS. CC2 does not self-merge.
- Number 008 verified free (highest spec file = 007; 008 = 007 + 1).
- Paired prompt: prompt-spec008-CC2-web-dashboard.md
- Final on-disk: `src/AmetekWatch.Web/` + `tests/AmetekWatch.Web.Tests/` + `.sln` updated.

## Background
The charter's deliverable surface is a **local web UI** (owner decision) that browses triaged findings.
This builds the first version: an ASP.NET minimal-API app that reads findings from an `IFindingStore`
and renders them. It depends only on `AmetekWatch.Core` (the `IFindingStore` seam + the fakes), so it is
**independent of the SQLite work (007)** — use an in-memory store seeded by one fake sweep for now.

## Decisions made
1. **New web project `src/AmetekWatch.Web`** (`Microsoft.NET.Sdk.Web`, `net8.0`), references
   `AmetekWatch.Core`. Add it to `AmetekWatch.sln` (append; do not reorder existing entries).
2. **Seed data at startup** by running one fake sweep through Core: `FakeSearcher` + `FakeTriageDecider`
   + `SweepRunner` + `InMemoryFindingStore` (register the populated `IFindingStore` in DI). *(When 007's
   `SqliteFindingStore` lands, a later spec swaps the registration — do not depend on 007 here.)*
3. **Endpoints:**
   - `GET /api/findings` → JSON array of all `TriagedFinding`s from the store (most-recent first).
   - `GET /` → a minimal self-contained HTML page (a table of findings: category, title, url, worth-
     reporting flag, rationale). Server-rendered or a static page that fetches `/api/findings` — your call.
4. **Tests** — new xUnit project `tests/AmetekWatch.Web.Tests` using
   `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory<Program>`); make `Program` partial/public
   as needed. Assert `GET /api/findings` returns 200 and the expected seeded findings (count + that the
   worth-reporting ones are present), with **hand-computed** expectations from the fakes. Confirm a test
   can fail then revert.
5. Keep the dashboard **read-only** and local (no auth, binds localhost). Do not modify `AmetekWatch.App`
   or the slice's test project.

## Out of scope
- SQLite (007) — use the in-memory store. Real Anthropic data. Scheduling. Auth. Email.

## Definition of done
- [ ] `src/AmetekWatch.Web` with the two endpoints; added to `.sln`.
- [ ] `tests/AmetekWatch.Web.Tests` (WebApplicationFactory) asserting `/api/findings`; can-fail confirmed.
- [ ] Gate green: `dotnet build -c Release`, `dotnet format --verify-no-changes`, `dotnet test` (each
      separately; real counts). Branch pushed; tip SHA reported.

## Deliverable / report-back
See the prompt file.
