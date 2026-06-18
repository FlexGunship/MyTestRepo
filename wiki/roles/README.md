# Roles

These are the working contracts for every member of the team. They define what
each role does, what it does not do, and the disciplines it is held to.

| File | Role | Tag | Surface |
|---|---|---|---|
| [`manager.md`](manager.md) | Claude Manager | `CM` | Claude on the planning host |
| [`claude-dev.md`](claude-dev.md) | Claude Code | `CC` | Claude — own git worktree |
| [`claude-dev.md`](claude-dev.md) | Claude Code (2nd) | `CC2` | Claude — own worktree; follows `claude-dev.md` |
| [`codex-dev.md`](codex-dev.md) | Codex | `CX` | GPT-5.5 Codex — own worktree |
| [`codex-dev.md`](codex-dev.md) | Codex (2nd) | `CX2` | GPT-5.5 Codex — own worktree; follows `codex-dev.md` |
| [`grok-build.md`](grok-build.md) | Grok Build | `GB` | Grok / xAI — own worktree (optional 5th surface; tie-breaker / `GB`-tagged dev / image-gen) |

> Core roster is **CM + CC + CX**; a balanced team runs 2 Claude (`CC1`/`CC2`) + 2 Codex (`CX1`/`CX2`)
> so cross-model integration always has an independent reviewer. Extra or alternative surfaces can be
> added — **[`grok-build.md`](grok-build.md) is the worked example**: a third model that adopts the Claude
> Dev contract and serves as a cross-model tie-breaker, an extra executor, or for image/diagram work. To
> add any surface, give it its own role doc, tag, and branch namespace, and list it in
> [`surfaces.md`](surfaces.md), the live source of truth.

## Surface Register

- Active surfaces, branch namespaces, and merge authority are tracked in [`surfaces.md`](surfaces.md) — that file is the live source of truth; this table is the on-ramp summary.

## Reading order at session start

Read **your** role doc, then skim the **others**. Understanding your counterparts'
responsibilities is what keeps the produces/executes separation clean — a developer who knows
what the Manager owns won't silently make planning decisions, and the Manager who knows what a
developer owns won't over-specify execution mechanics.

## Editing role docs

These are **living documents**, edited in place. Git history is the version trail — there are
no `_vN.md` suffixes.

- Either side may edit either role doc when a pattern warrants it.
- Every doc carries a `## Changelog` section at the bottom. Add a dated one-line entry for any
  material change.
- **Roles update notice (sync protocol):** when you edit a role doc, announce it in your next
  transmission to the owner with a one-line "Roles update notice: edited `roles/X.md` — <what
  changed>" so the change is relayed to whoever it affects. Silent contract edits are how teams
  drift apart.

Contracts win over passdowns. If a passdown and a role doc disagree, the role doc wins.
