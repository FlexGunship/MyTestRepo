# Spec 005-CC2 — Claude Code 2nd instance (CC2) onboarding

> Self-contained onboarding spec (no paired prompt — environment-only). Same shape as
> [`003-GB-onboarding.md`](003-GB-onboarding.md); see [`../rituals/agent-onboarding.md`](../rituals/agent-onboarding.md).

## Status
- Doc type: onboarding (environment-only; no deliverable)
- Executes: **CC2**; merge: **integrator-merged** — CC2 pushes `agent/cc2`; **CM lands the report**
  mechanically (process doc). No self-merge.
- Number 005 verified free (highest spec file = 004-CX; 005 = 004 + 1).
- Paired prompt: none (self-contained).
- Final on-disk: `wiki/reports/report-spec005-cc2-onboarding.md` (+ optional lesson in `wiki/surfaces/cc2/CLAUDE.md`).

## Background
CC2 is the second Claude Code developer surface, activated to complete the balanced 2-Claude + 2-Codex
roster for **parallel work and parallel cross-model integration** (see [`../roles/surfaces.md`](../roles/surfaces.md)).
It follows the [Claude Dev contract](../roles/claude-dev.md), runs the `claude` CLI, works in its own
worktree `worktrees/CC2` on home branch `agent/cc2`, and does **not** self-merge. The structural rows
(`wiki/surfaces/cc2/CLAUDE.md`, the `surfaces/README.md` + `roles/README.md` rows) already exist.

## Decisions made
1. **Onboarding gate = the docs gate** `python3 tools/doc_check.py` (same lightweight environment proof
   CC/CX/GB used). The product `.NET` gate is not part of onboarding.
2. **Worktree** `worktrees/CC2` on `agent/cc2`; deliverable work later uses `feature/cc2-<slug>` off `main`.
3. Environment only — no product code, no `.exe`.

## Scope — what to do
0. PREFLIGHT (fail fast): `git ls-remote origin` succeeds; `python3 --version` ≥ 3.8. Else STOP + report.
1. In `worktrees/CC2`: `git config user.name=CC2`, `user.email=cc2@agents.local`; `git fetch --prune origin`.
2. Read, in order: [`../roles/claude-dev.md`](../roles/claude-dev.md), the other role docs in `wiki/roles/`,
   your learnings file [`../surfaces/cc2/CLAUDE.md`](../surfaces/cc2/CLAUDE.md),
   [`/CLAUDE.md`](../../CLAUDE.md) Status & Roadmap, the latest passdown in `wiki/passdowns/`, open Q&A in
   `wiki/qanda/`, [`../../README.md`](../../README.md), the [git & gates contract](../contracts/git-and-gates.md),
   and [`../best-practices.md`](../best-practices.md).
3. Run `python3 tools/doc_check.py` from the worktree (expect exit 0, "OK"); capture real output.
4. Optionally append a dated lesson to `wiki/surfaces/cc2/CLAUDE.md`.
5. Write `wiki/reports/report-spec005-cc2-onboarding.md` (per [`../rituals/report-format.md`](../rituals/report-format.md)):
   branch + SHA; `python3 --version`; the real `doc_check` output; confirm `git push` works **without
   printing any token**; any blocker. Commit on `agent/cc2`, push to `origin/agent/cc2`. Do **not** self-merge.

## Out of scope
- Product/`.NET` code, the build gate, the `.exe`. Self-merge / landing (CM lands).

## Definition of done
- [ ] Preflight passed; identity set; context read.
- [ ] `doc_check.py` green from the CC2 worktree (real output).
- [ ] Report committed on `agent/cc2`, pushed; tip SHA reported; no secrets printed.

## Deliverable / report-back
See Scope step 5 + [`../rituals/agent-onboarding.md`](../rituals/agent-onboarding.md) step 5. No deliverables ship.
