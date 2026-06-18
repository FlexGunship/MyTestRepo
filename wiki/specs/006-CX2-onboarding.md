# Spec 006-CX2 — Codex 2nd instance (CX2) onboarding

> Self-contained onboarding spec (no paired prompt — environment-only). Same shape as
> [`003-GB-onboarding.md`](003-GB-onboarding.md); see [`../rituals/agent-onboarding.md`](../rituals/agent-onboarding.md).

## Status
- Doc type: onboarding (environment-only; no deliverable)
- Executes: **CX2**; merge: **integrator-merged** — CX2 pushes `agent/cx2`; **CM lands the report**
  mechanically (process doc). No self-merge.
- Number 006 verified free (highest spec file = 005-CC2; 006 = 005 + 1).
- Paired prompt: none (self-contained).
- Final on-disk: `wiki/reports/report-spec006-cx2-onboarding.md` (+ optional lesson in `wiki/surfaces/cx2/CODEX.md`).

## Background
CX2 is the second Codex developer surface, activated to complete the balanced 2-Claude + 2-Codex roster
for **parallel work and parallel cross-model integration** (see [`../roles/surfaces.md`](../roles/surfaces.md)).
It follows the [Codex Dev contract](../roles/codex-dev.md), runs the `codex` CLI, works in its own
worktree `worktrees/CX2` on home branch `agent/cx2`, and does **not** self-merge. The structural rows
(`wiki/surfaces/cx2/CODEX.md`, the `surfaces/README.md` + `roles/README.md` rows) already exist.

## Decisions made
1. **Onboarding gate = the docs gate** `python3 tools/doc_check.py` (same lightweight environment proof
   CC/CX/GB used). The product `.NET` gate is not part of onboarding.
2. **Worktree** `worktrees/CX2` on `agent/cx2`; deliverable work later uses `feature/cx2-<slug>` off `main`.
3. Environment only — no product code, no `.exe`.

## Scope — what to do
0. PREFLIGHT (fail fast): `git ls-remote origin` succeeds; `python3 --version` ≥ 3.8. Else STOP + report.
1. In `worktrees/CX2`: `git config user.name=CX2`, `user.email=cx2@agents.local`; `git fetch --prune origin`.
2. Read, in order: [`../roles/codex-dev.md`](../roles/codex-dev.md), the other role docs in `wiki/roles/`,
   your learnings file [`../surfaces/cx2/CODEX.md`](../surfaces/cx2/CODEX.md),
   [`/CLAUDE.md`](../../CLAUDE.md) Status & Roadmap, the latest passdown in `wiki/passdowns/`, open Q&A in
   `wiki/qanda/`, [`../../README.md`](../../README.md), the [git & gates contract](../contracts/git-and-gates.md),
   and [`../best-practices.md`](../best-practices.md).
3. Run `python3 tools/doc_check.py` from the worktree (expect exit 0, "OK"); capture real output.
4. Optionally append a dated lesson to `wiki/surfaces/cx2/CODEX.md`.
5. Write `wiki/reports/report-spec006-cx2-onboarding.md` (per [`../rituals/report-format.md`](../rituals/report-format.md)):
   branch + SHA; `python3 --version`; the real `doc_check` output; confirm `git push` works **without
   printing any token**; any blocker. Commit on `agent/cx2`, push to `origin/agent/cx2`. Do **not** self-merge.

## Out of scope
- Product/`.NET` code, the build gate, the `.exe`. Self-merge / landing (CM lands).

## Definition of done
- [ ] Preflight passed; identity set; context read.
- [ ] `doc_check.py` green from the CX2 worktree (real output).
- [ ] Report committed on `agent/cx2`, pushed; tip SHA reported; no secrets printed.

## Deliverable / report-back
See Scope step 5 + [`../rituals/agent-onboarding.md`](../rituals/agent-onboarding.md) step 5. No deliverables ship.
