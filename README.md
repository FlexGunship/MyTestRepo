# Agentic Start Kit

A project-agnostic **starter kit** for running a software project on a multi-agent operating model:
a **Claude Manager (CM)** who plans and lands, with **Claude Dev (CC)** and **Codex Dev (CX)** surfaces
who execute — every deliverable reaching an always-green `main` only through a **gate + independent
cross-model review** (author ≠ integrator). It carries the working *structure, rituals, roles, and
anti-drift lore* — and **nothing** tied to any past project.

It is a **flexible core, not a fixed pipeline.** The same architecture runs whether you are:
- **documenting / reverse-engineering** an existing codebase (read-only subject in `reference/`, a cited
  `docs/` as-built wiki, a docs gate), or
- **building** new software (source in `lib/`/`src/`, a build-and-test gate, versioning/CHANGELOG).

Claude Manager picks the specifics from your **charter**, which it asks you for on first boot — so you
only ever talk to CM.

## Start a new project (one-time per project)
1. On GitHub, click **"Use this template"** on this repo → creates a fresh repo for your project
   (or, all-CLI: `gh repo create <you>/<project> --private --template <you>/agentic-start-kit`).
2. On your machine (incl. headless): **`git clone <your new project repo>`** and `cd` in.
   *(Or download the single [`bootstrap.sh`](bootstrap.sh) into an empty folder and run it — it clones
   the kit's contents in and scaffolds for you.)*
3. Open **Claude Manager** in the folder and say **"bootstrap this project."** CM reads
   [`wiki/bootstrap/first-boot.md`](wiki/bootstrap/first-boot.md), **interviews you for the charter**
   (what / who / why / north-star / scope / mode), writes `wiki/product-charter.md`, pins the gate in
   `CLAUDE.md`, sets up the developer worktrees, and you're running.

## What's inside
```
CLAUDE.md                 operating context + the (flexible) gate + Status & Roadmap skeleton
bootstrap.sh              single-file bootstrap (download → run → talk to CM)
tools/                    doc_check.py (docs gate) · cite_audit.py (citation dump helper)
reference/                read-only subject home (documentation/RE projects; delete for build projects)
docs/                     the deliverable wiki home (shape it per project)
wiki/                     the operating manual:
  README.md  best-practices.md  glossary.md  collaboration-workflow.md
  contracts/git-and-gates.md      the integration contract (flexible gate)
  roles/     manager.md (CM) · claude-dev.md (CC) · codex-dev.md (CX) · surfaces.md
  rituals/   spec-format · report-format · agent-onboarding · surface-learnings ·
             lanes · passdown · qanda · wiki-page-history · subsystem-deep-dive (optional pattern) ·
             owner-action-checklist · contributing-back
  templates/ spec · prompt · report · passdown · product-charter · …
  surfaces/  per-surface learnings (empty seeds: cc1/cc2 CLAUDE.md, cx1/cx2 CODEX.md)
  specs/ reports/ reviews/ passdowns/ qanda/ backlog/ lanes/   (empty content homes)
```

## This kit is a living template — feed wisdom back
After a project, the experienced CM should contribute its genuinely-generic lessons **back** to this
kit via a PR (see [`wiki/rituals/contributing-back.md`](wiki/rituals/contributing-back.md)) — so every
future project starts from a better framework than the last. Keep project-specifics out; promote only
what generalizes.
