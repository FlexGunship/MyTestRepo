# Component — <AppName>

> Template for a Project Wiki **component catalog** page (`docs/components/<app>.md`). One operator app/program
> per page. This catalog feeds the **replacement requirements doc**: user feature (#1) + intended use (#2) +
> method of implementation (#3). Keep each page focused; cross-reference the subsystem page for shared detail
> rather than repeating it.

**Header block (fill in):**
- **Subsystem:** cross-reference the owning subsystem page — in the app page (which lives in `docs/components/`)
  write the live relative link `../subsystems/<name>.md`. (Subsystem pages are on `main`.)
- **Source:** `reference/<path-to>/<dir>/` — and how it is launched/entered (cite the launcher/spawn line,
  the entry point, or its usage string).

## Feature (what it does)
One to three sentences: the user-facing capability. Cite source where the claim rests on it.

## Intended use (how/why it's used)
The operational use — the "why". **Tag provenance on every intent claim:**
- **sourced** — cite `reference/path:line` (a usage string, comment, or readme entry states it);
- **inferred** — from the name / UI / context (say so explicitly); never present inferred intent as fact;
- **needs-SME** — the "why" isn't in the source. For safety/precision-critical features, inferred is **not**
  acceptable — mark **needs-SME** for owner/operator validation.

## Implementation (how it's built)
Key files; the UI/framework structure if it's a GUI app (the generated skeleton + callbacks); the data
flow; the shared-memory regions / messages it uses (cross-ref the IPC page — `../subsystems/ipc-shared-memory.md`);
which subsystem mechanisms it relies on. Cite `reference/path:line`. Note any prebuilt/black-box dependency as
"behavior to recover" (don't invent internals).

## Requirement notes (for the replacement)
Per notable behavior, a quick call: **essential** (the *what* — a real requirement) vs **incidental** (the
*how* — a platform/era/hardware artifact, replaceable). Flag any **non-functional / safety** requirement you meet
(timing, interlocks, precision/thermal, failure modes). You needn't resolve every one — flagging seeds the
requirements synthesis.

## Revision history
| Date | Spec | Agent | Change |
|---|---|---|---|
| <YYYY-MM-DD> | <NNN-XX> | <agent> | Initial component page. |
