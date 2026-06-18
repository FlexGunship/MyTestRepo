# Surface learnings (`wiki/surfaces/`)

Each dev surface keeps its **own durable learnings file** here — its accumulated, hard-won lessons
about how to do this work well, so behavior **improves over time** instead of resetting every
headless run. A dispatched surface is stateless except for what is committed; this is where its
experience lives.

| Surface | File | Model |
|---|---|---|
| CC1 | [`cc1/CLAUDE.md`](cc1/CLAUDE.md) | Claude Dev |
| CC2 | [`cc2/CLAUDE.md`](cc2/CLAUDE.md) | Claude Dev |
| CX1 | [`cx1/CODEX.md`](cx1/CODEX.md) | Codex Dev |
| CX2 | [`cx2/CODEX.md`](cx2/CODEX.md) | Codex Dev |
| GB | [`gb/GROK.md`](gb/GROK.md) | Grok Build (optional 5th surface) |

## The two-tier model

- **Personal (this directory).** Instance-specific lessons — what *this* surface keeps getting
  right/wrong. Per-surface so it stays the surface's own voice.
- **Shared (the team lore).** When a lesson generalizes to everyone, it gets **promoted** to
  [`best-practices.md`](../best-practices.md) (flag it for the Manager to land). That is the
  compounding layer: a personal lesson, once proven general, becomes everyone's.

## The protocol (see [`rituals/surface-learnings.md`](../rituals/surface-learnings.md))

1. **At run start**, after `git fetch`, read — in addition to your spec+prompt — your **role doc**
   (`wiki/roles/`), [`best-practices.md`](../best-practices.md), the
   [git & gates contract](../contracts/git-and-gates.md), the relevant **rituals** (`wiki/rituals/`),
   and **your own file in this directory**.
2. **At run end**, append any lesson you learned to your file (dated, with the spec #). If it would
   help *every* surface, also flag it for promotion to `best-practices.md`.
3. **Keep it lean.** Lessons, not a journal. Don't duplicate `best-practices.md` here — link to it.
