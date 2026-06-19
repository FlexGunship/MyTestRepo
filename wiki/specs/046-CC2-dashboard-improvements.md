# Spec 046-CC2 — Dashboard improvements (richer findings view)

## Status
- Doc type: implementation (UX — make the dashboard genuinely useful)
- Executes: **CC2**; pushes `feature/cc2-dashboard-improvements`; **CX2** integrates (cross-model); CM lands on PASS. No self-merge.
- Number 046 verified free (search `wiki/specs/`; this is highest + 1). **Web-only — independent of 045.**
- Paired prompt: prompt-spec046-CC2-dashboard-improvements.md
- Final on-disk: `src/AmetekWatch.Web/Program.cs` + `tests/AmetekWatch.Web.Tests/`.

## Background
The dashboard (008/017) renders a bare HTML table from the SQLite store. This makes it actually useful for
scanning AMETEK findings: a summary header, richer columns, friendly category labels, a worth-reporting
filter, and minimal styling — all read-only and local.

## Decisions made
1. **`GET /` (HTML)** — keep server-rendered + self-contained (no external assets; all dynamic text HTML-escaped):
   - **Header:** project title, total findings, **worth-reporting count**, and a "generated `<UTC time>`" line.
   - **Table columns:** friendly **Category** label ("Opinion / Social" / "Financial Report" / "Other" — **no
     internal enum names**), **Title** (a link to the finding `Url`), **Worth reporting** (a clear ✓ / — mark),
     **Published** and **Discovered** dates (friendly; "—" when null), and the decider's **rationale**.
   - **Worth-reporting filter:** `GET /?worthOnly=true` shows only worth-reporting findings (default shows all);
     a small link/toggle between the two views.
   - **Minimal inline CSS** for readability (a clean table; highlight worth-reporting rows). Keep it lightweight.
2. **`GET /api/findings`** — keep returning all `TriagedFinding`s most-recent first; add optional `?worthOnly=true`
   to filter (default unchanged). JSON shape unchanged otherwise.
3. Read-only, binds localhost, no auth. Reads from the configured `IFindingStore` (the SQLite store, per 017) —
   don't change the store wiring. Web-only; don't touch other projects or the `.sln`.
4. **Tests** (`tests/AmetekWatch.Web.Tests/`, `WebApplicationFactory`): assert the HTML contains the summary
   counts and the new columns/labels (and that **no internal enum/type names** leak into the rendered HTML);
   `GET /api/findings?worthOnly=true` returns only worth-reporting findings while the default returns all;
   ordering preserved. Hand-computed; confirm a test can fail then revert. Update the existing 008/017 tests if
   the markup they assert on changed (and note which).

## Out of scope
- Auth, writes from the dashboard, pagination, charts/trends, external CSS/JS frameworks. The API/SMTP.

## Definition of done
- [ ] `GET /` richer view (header counts, friendly columns, worth-only filter, light CSS, no internal names);
      `GET /api/findings?worthOnly=true` filter; defaults unchanged.
- [ ] Web tests cover the new content + filter; existing Web tests pass (updated if markup changed). Can-fail confirmed.
- [ ] Gate green (build/format/test, each separately, real counts). Branch pushed; tip SHA reported.

## Deliverable / report-back
See the prompt file.
