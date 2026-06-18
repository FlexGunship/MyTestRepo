# Ritual: Surface learnings (read-at-start, append-at-end)

Every dispatched surface is **stateless** beyond what is committed — a headless run starts cold.
This ritual is how a surface **carries experience forward** so its behavior improves over time
instead of resetting each run. Each surface owns a durable learnings file under
[`wiki/surfaces/`](../surfaces/README.md): [CC1](../surfaces/cc1/CLAUDE.md) ·
[CC2](../surfaces/cc2/CLAUDE.md) · [CX1](../surfaces/cx1/CODEX.md) · [CX2](../surfaces/cx2/CODEX.md).

## At run start (after `git fetch --prune`, before touching the spec's work)
Read, in this order:
1. **Your role doc** in `wiki/roles/` (Claude Dev / Codex Dev) and skim the others.
2. [`best-practices.md`](../best-practices.md) — the shared anti-drift lore (the guiding rule:
   *every claim inferred from the code carries a per-line citation; cite the load-bearing line,
   don't flood*).
3. The [git & gates contract](../contracts/git-and-gates.md) — what ships where, the gate, merge rules.
4. The **rituals** relevant to the work (`wiki/rituals/`): [spec format](spec-format.md),
   [report format](report-format.md), this file.
5. **Your own learnings file** in `wiki/surfaces/`.
6. The latest [passdown](../passdowns/) and your spec + prompt.

## At run end (closing step, alongside your report)
- **Append any lesson you learned** to your learnings file: a dated row (`| date | spec | lesson |`)
  and, if load-bearing, a bullet in the right section. Capture what you'd want your next cold start
  to know — a citation you got wrong, a gate gotcha, a boundary you drifted across, a review tactic
  that worked.
- **Keep it lean.** Lessons, not a journal. One citation/example per lesson. Don't duplicate
  `best-practices.md`; link to it.
- **Promote shared lessons.** If a lesson would help *every* surface (not just you), say so in your
  report — **flag it for the Manager to land into [`best-practices.md`](../best-practices.md)**. That
  is the compounding path: personal lesson → proven general → team lore. (Surfaces never edit
  `best-practices.md` directly mid-deliverable; CM lands shared lore.)

## Standing correctives (manager → surface)

When a surface **repeatedly** falls short of a guideline, the Manager issues a numbered
**corrective-guidance spec** addressed to it. If your surface is listed below, **read your corrective
spec at run-start** (in addition to your own learnings) and **acknowledge it in your next authoring
report** ("applied corrective NNN", stating how). Correctives are standing until CM retires them.

| Surface | Corrective | Topic |
|---|---|---|
| _(none yet)_ | | CM adds a row here linking to a numbered corrective-guidance spec under `wiki/specs/` when a surface repeatedly falls short. |

## Why per-surface (owner's call, 2026-06-13)
Learnings are **per-surface** so each file stays that surface's own voice and instance-specific
quirks. The shared layer already exists — the role docs, best-practices, contracts, and rituals — and
cross-cutting lessons compound there via promotion. Two tiers: personal here, shared in the lore.

## Changelog
- 2026-06-13 — Initial. Created the `wiki/surfaces/` per-surface learnings files + this read/append
  protocol, after the owner observed surfaces weren't carrying experience between headless runs.
