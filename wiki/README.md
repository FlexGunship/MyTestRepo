# Operating manual

This `wiki/` is the team's **operating model** — the roles, contracts, rituals, and lore for running a
project on the CM-plans / CC+CX-execute architecture. It is **flexible**: the same model runs whether the
project **builds new software** (source in `lib/`/`src/`, a build-and-test gate) or **documents /
reverse-engineers** an existing codebase (a read-only subject in [`reference/`](../reference/README.md),
a cited [`docs/`](../docs/README.md) deliverable, a docs gate). The constant is the workflow; the gate and
deliverable flex to the project.

> **Fresh project.** On first boot, Claude Manager writes the [product charter](templates/product-charter.md)
> from the owner interview and shapes the gate + deliverable to the project — see
> [`bootstrap/first-boot.md`](bootstrap/first-boot.md). You only talk to CM.

## The team

- **CM (Manager)** — planning-only seat (Claude Code). Authors specs/prompts, maintains the wiki and the
  `CLAUDE.md` Status & Roadmap, dispatches via the owner, reviews. Does **not** write deliverables.
- **CC (Claude Code)** / **CX (Codex)** — developer surfaces; each works in its own git worktree.
  They execute specs: read source, write analysis docs, self-gate, branch, push for integration.
- **GB (Grok Build)** — developer surface with native **image generation**; reserved for diagram/asset
  work. Held out unless a spec needs generated imagery.
- The **owner** is the **sole relay** — surfaces never cue each other directly. The owner dispatches by
  **spec number** ("run spec 003"), never by pasting prose.

Roles in [`roles/`](roles/). Capability/assignment principle: harder, higher-judgment work to more
capable surfaces; the integrator is always a **different model** than the author.

## How work flows

1. **Spec** — CM writes a numbered `wiki/specs/NNN-XX-slug.md` (+ a paired `prompt-specNNN-…`). Specs are
   **immutable** once committed; corrections become a **new** spec at the next number. One spec routes to
   exactly the one surface (`XX`) in its filename.
2. **Execute** — the owner relays "spec NNN" to surface `XX`. The surface reads the source, writes the
   analysis under `docs/`, self-runs the gate, and pushes its branch (deliverables) or commits direct
   (process docs).
3. **Gate + integrate** — a different surface runs the [gate](contracts/git-and-gates.md)
   (`python tools/doc_check.py` + a **source-grounded review**) and `--no-ff` merges to `main`, or HOLDs.
4. **Report** — every ship gets a `wiki/reports/report-specNNN-XX-slug.md` (real output, citations,
   surprises). Reports are process docs → direct to `main`.

Rituals: [spec format](rituals/spec-format.md), [report format](rituals/report-format.md),
[lanes](rituals/lanes.md) (a multi-spec chain on one branch), [passdown](rituals/passdown.md),
[Q&A](rituals/qanda.md), [agent onboarding](rituals/agent-onboarding.md),
[wiki page history](rituals/wiki-page-history.md) (the `## Revision history` footer on every `docs/` page).
Vocabulary: [glossary](glossary.md). A plain-language overview: [collaboration workflow](collaboration-workflow.md).

## Conventions

- **The legacy source is read-only.** Document it; never edit it (except by an explicit owner-approved
  spec).
- **Cite the source.** Every non-trivial claim in a deliverable points to `path:line` (or a stable
  symbol) so a reviewer can verify it. Un-cited claims fail the gate.
- **`main` is always green** — process docs commit directly; deliverables go branch → gate → integrate.
- Never print or commit secrets/tokens. External/cloud storage is off-limits unless imported by spec.
