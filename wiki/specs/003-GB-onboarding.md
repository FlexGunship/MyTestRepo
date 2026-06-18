# Spec 003-GB — Grok Build (GB) onboarding

> Self-contained onboarding spec (no paired prompt — environment-only, no deliverable ships). The
> operational instructions are this file plus [`../rituals/agent-onboarding.md`](../rituals/agent-onboarding.md)
> (including its **Grok Build surface-specific notes**).

## Status
- Doc type: onboarding (environment-only; no deliverable)
- Executes: **GB**; merge: **integrator-merged** — GB pushes `agent/gb`; **CM lands the report**
  mechanically (process doc). GB does **not** self-merge (no self-merge until graduation).
- Number 003 verified free (wiki/specs/ highest = 001-CC-vertical-slice; 001/002 already consumed by the
  CC/CX onboarding **reports**; next clean number = 003).
- Paired prompt: none (self-contained).
- Final on-disk locations after merge: `wiki/reports/report-spec003-gb-onboarding.md` (and an optional
  lesson appended to `wiki/surfaces/gb/GROK.md`).

## Background
GB (Grok Build, xAI) is the optional fifth surface (see [`../roles/grok-build.md`](../roles/grok-build.md)
and [`../roles/surfaces.md`](../roles/surfaces.md)). The owner has activated it as the third independent
model — a cross-model tie-breaker, an extra `GB`-tagged executor, and for native image/diagram work. This
spec brings it online: prove it can see the repo, run the project's tooling, and push from its own
worktree. **The onboarding report is the proof.** The structural rows (its `GROK.md` learnings file, the
`surfaces/README.md` and `roles/README.md` rows) already exist; CM has flipped GB's status in
`surfaces.md` from reserve to onboarding.

Runtime is the **`grok` CLI** (not `claude`/`codex`), model **`grok-build`** — preflighted by CM:
grok 0.2.56, logged in, headless `grok -p … -m grok-build` smoke test returned OK.

## Decisions made
1. **Gate for onboarding = the docs gate** `python3 tools/doc_check.py` — same lightweight environment
   proof CC and CX used. The product `.NET` build/test gate is **not** part of onboarding (the .NET
   solution is not on `main` yet, and toolchain setup for GB is a separate later concern if GB does build
   work). Onboarding proves environment + repo access + a green gate + push, not the product build.
2. **Worktree:** `worktrees/GB` on home branch `agent/gb` (matches the CC/CM/CX convention). Deliverable
   work later uses `feature/gb-<slug>` branches off `main`.
3. **No deliverables, no `.exe`, no product code.** Environment only.

The developer implements as stated unless it finds a strong reason to deviate — in which case it flags it
in the report rather than silently choosing otherwise.

## Scope — what to do
0. **PREFLIGHT (fail fast):** `git ls-remote origin` succeeds; `python3 --version` ≥ 3.8. If either
   fails, STOP and report the exact failing item.
1. In `worktrees/GB` (branch `agent/gb`): set `git config user.name=GB`, `user.email=gb@agents.local`;
   `git fetch --prune origin`.
2. Read, in order: [`../roles/grok-build.md`](../roles/grok-build.md), the other role docs in
   `wiki/roles/`, your learnings file [`../surfaces/gb/GROK.md`](../surfaces/gb/GROK.md),
   [`/CLAUDE.md`](../../CLAUDE.md) Status & Roadmap, the latest passdown (`wiki/passdowns/`, may be empty),
   open Q&A (`wiki/qanda/`, may be empty), [`../../README.md`](../../README.md), the
   [git & gates contract](../contracts/git-and-gates.md), and [`../best-practices.md`](../best-practices.md).
3. Run the gate from the worktree: `python3 tools/doc_check.py` (expect exit 0, "OK"). Capture real output.
4. Append a dated lesson to `wiki/surfaces/gb/GROK.md` if you learned anything (optional).
5. Write `wiki/reports/report-spec003-gb-onboarding.md` per the report-back format below; commit on
   `agent/gb` and push to `origin/agent/gb`. Do **not** self-merge.

## Out of scope
- Any product/`.NET` code, the build-and-test gate, the `.exe`, image generation. (Environment only.)
- Self-merge / landing to `main` — CM lands the report mechanically.

## Definition of done
- [ ] Preflight passed (or the exact blocker reported).
- [ ] Git identity set; context read in order.
- [ ] `python3 tools/doc_check.py` green from the GB worktree (real output recorded).
- [ ] Report written, committed on `agent/gb`, pushed to `origin/agent/gb`; tip SHA reported; no secrets printed.

## Deliverable / report-back
Write `wiki/reports/report-spec003-gb-onboarding.md` per
[`../rituals/agent-onboarding.md`](../rituals/agent-onboarding.md) step 5 and
[`../rituals/report-format.md`](../rituals/report-format.md):
- branch (`agent/gb`) + current SHA; `python3 --version`; `grok --version` + model used (`grok-build`);
- the real `doc_check.py` output;
- confirm `git push` works **without printing any token**;
- any blocker with the exact failing step. **No deliverables ship in onboarding.**
