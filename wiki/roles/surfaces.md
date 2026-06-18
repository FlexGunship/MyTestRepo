# Developer surface register — the project

The current roster of surfaces, their branch namespace, and merge authority. Updated as surfaces
onboard and earn graduation.

| Surface | Runtime | Branch namespace | Merge authority | Status (2026-06-12) |
|---|---|---|---|---|
| **CM** | Claude Code (manager seat) | n/a (process docs direct to `main`) | authors specs; does not write deliverables | active (bootstrapping the project) |
| **CC** | Claude Code | `feature/cc-<slug>` | integrator-merged (onboarding); cross-model integrator for Codex work | pending onboarding (the project) |
| **CC2** | Claude Code (2nd instance) | `feature/cc2-<slug>` | integrator-merged (no self-merge) | pending onboarding (the project) |
| **CX** | Codex | `feature/cx-<slug>` | integrator-merged (onboarding) | pending onboarding (the project) |
| **CX2** | Codex (2nd instance) | `feature/cx2-<slug>` | integrator-merged (no self-merge) | pending onboarding (the project) |
| **GB** | Grok (xAI), native image-gen | `feature/gb-<slug>` | integrator-merged (no self-merge until graduation) | **optional 5th surface** — activate via onboarding when a 3rd model is wanted (tie-breaker / `GB`-tagged dev / image-gen). See [`grok-build.md`](grok-build.md). |

## Rules
- **One spec routes to exactly the one surface** named in its filename. Integration/review by another
  surface is its **own** numbered spec titled with that surface's call-sign.
- **Author ≠ integrator**, and the integrator is a **different model** by default (Claude↔Codex) so every
  merge gets cross-model review.
- Each surface works in its own **git worktree** of the project clone
  (`~/workspace/the project/{repo-master, worktrees/<tag>}`); never check out a branch another worktree holds.
- **Graduation** (self-merge authority) is earned per surface by track record on its own auditions; it
  does not transfer between surfaces.

## Notes
- **Balanced pair (roster design).** The intended roster is **2 Claude (CC, CC2) + 2 Codex (CX, CX2)** for
  the deep-dive phase. The balance is deliberate: integration is **author ≠ integrator, cross-model**, so a 2:2 roster
  means Claude work is integrated by a Codex and Codex work by a Claude **without either model becoming an
  integration chokepoint**. **CC2** follows the [Claude Dev contract](claude-dev.md); **CX2** follows the
  [Codex Dev contract](codex-dev.md) — same gate, branch discipline, and report rigor as their siblings.
- **Naming disambiguation.** the project's `CC2`/`CX2` are simply the second Claude/Codex *on this project*. They
  are **not** the historical prior-project `CX1`/`CX2` lineage referenced in [`codex-dev.md`](codex-dev.md);
  that is unrelated history.
- **GB is the optional fifth surface** (see [`grok-build.md`](grok-build.md)). Add it when you want a third
  independent model: a cross-model **tie-breaker** (when CC↔CX disagree), an extra `GB`-tagged executor
  under load, or for Grok's native **image/diagram** generation. It adopts the Claude Dev contract and does
  **not** self-merge until it earns graduation. If you keep the team to the 2 Claude + 2 Codex core for
  budget reasons, leave GB dormant — its role doc and onboarding notes are ready when you want it.
- A surface comes online via the [agent-onboarding ritual](../rituals/agent-onboarding.md) + a thin
  `NNN-XX-<tag>-onboarding` spec. **Grok Build has surface-specific onboarding notes** in that ritual
  (the `grok` CLI dispatch, model selection, autonomy flag).
