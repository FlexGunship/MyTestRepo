# Ritual: Passdown

A **passdown** is an end-of-arc continuity brief, written by the Manager into
`wiki/passdowns/`. It captures session-scoped context so the next session (which may be a
fresh, compacted instance) can pick up without re-deriving everything.

A passdown is **subordinate to the durable docs**. If a passdown and a role doc or `CLAUDE.md`
disagree, **the durable docs win** — the passdown is a snapshot, the contracts are the law.

Copy-paste starting point: [`templates/passdown.md`](../templates/passdown.md).

---

## When

At the end of a work arc, or on the owner's request. Not every session needs one — write a
passdown when there's enough accumulated context that losing it would cost the next session
real time.

## Naming

```
project-passdown-<version-or-date>.md
e.g.  project-passdown-2026-05-28.md   (pre-release: date-stamped)
      project-passdown-v0_3_0.md       (post-release: semver-stamped)
```

Newest wins at session start.

---

## Content shape

```markdown
# the project — Passdown <stamp>

## Why this document exists
One line. Read the role docs and CLAUDE.md first; this is context, not law.

## Project at a glance
A small table: current version, what's shipping, the active arc.

## Where authoritative information lives
Pointers to the docs that win over this passdown.

## What shipped since the last passdown
Specs merged, with their numbers and one-line outcomes.

## Patterns observed worth carrying forward
What's working; non-obvious lessons from this arc.

## Pending / forward queue
What's queued in backlog/next and backlog/later; what's mid-flight.

## Documentation staleness inventory
Docs you know are drifting and haven't been fixed yet.

## Recent Q&A worth carrying forward
The Q&A files whose answers future sessions will want.

## What to avoid (recurring failure shapes)
A numbered list of the traps this team keeps almost falling into.

## Communicating with the owner
Any in-flight relay context.

## Closing
One or two lines.
```

---

## Discovery at session start

The Manager reads the **most recent passdown** (newest stamp) as step 2 of its session-start
checklist, after its role doc. Developers read it too, before executing.

---

## Changelog
- 2026-05-28 — Initial. The wiki is the single home for passdowns (no dual storage).
