# Ritual: Project Wiki page revision history

Every page in the **Project Wiki** ([`docs/`](../../docs/README.md)) carries its own **revision history**
footer, so anyone can see *how and when a page changed, and by which agent* without digging through git.

> **Git is the authoritative record.** Author identity (`CM`/`CC`/`CX`/`GB`), timestamp, and the exact
> diff live in the commit log and cannot drift. The footer is the **friendly in-wiki view** of that
> record — it must agree with git, not replace it. If the two ever disagree, git wins and the footer is
> corrected.

This is the analysis-deliverable counterpart to the `## Changelog` sections on role docs and to the
`## Status & Roadmap` in [`CLAUDE.md`](../../CLAUDE.md).

## The footer

The **last section of every `docs/**/*.md` page** is a `## Revision history` table:

```markdown
## Revision history

| Date | Spec | Agent | Change |
|---|---|---|---|
| 2026-06-05 | 003-CC | CC | Initial repository map (first-pass breadth). |
| 2026-06-07 | 012-CX | CX | Added the comms/IO family; corrected 3 citations. |
```

- **Date** — `YYYY-MM-DD`, the ship date.
- **Spec** — the `NNN-XX` that authorized the change (the routing spec). Use `—` only for un-specced
  index maintenance (e.g. CM seeding the wiki home).
- **Agent** — the call-sign that made the change (`CM` / `CC` / `CX` / `GB`).
- **Change** — one line: what changed, not how. Newest row at the **bottom** (append-only).

## Who writes a row

- The **page author** adds the first row when the page is created.
- **Anyone who edits a page appends a row** — including an integrator who tweaks a page during a merge,
  and the integrator who flips a coverage row in the wiki home. One row per ship that touched the page.
- Rows are **append-only**: never rewrite or delete a prior row. A mistaken row is corrected by a new row.

## Enforcement

The docs gate (`python tools/doc_check.py`, or `python3`) **requires** a `## Revision history` section in
every `docs/**/*.md` file. A page without one is a gate failure — the same standing as a broken link or an
un-cited claim. There is no "I'll add it later."

## Changelog
- 2026-06-05 — Initial. Per-page revision-history footer + docs-gate enforcement; git is authoritative.
