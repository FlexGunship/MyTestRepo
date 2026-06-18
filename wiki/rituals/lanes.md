# Ritual: Lanes (long-running, multi-spec work)

A **lane** (a.k.a. epic) is the unit of delegation when a feature is several specs deep. Instead of
handing an agent one spec, waiting for the report, then authoring the next, the Manager authors a
**dependency-ordered queue of 2–4 specs up front** and an agent runs the whole chain on **one
long-lived branch**, integrating to `main` **once at a milestone**. This cuts the owner-relay touch
points from one-per-spec to roughly one-per-milestone, and lets independent agents run at their own
pace for a long stretch.

## The rules

1. **One lane = one agent = one long-lived branch.** Branch `feature/<agent>-<lane>` (e.g.
   `feature/cx1-foundation`). The first spec creates it; later specs **continue** on it; it is **not**
   merged between specs.
2. **Disjoint directory ownership.** Each lane owns a declared set of directories and touches nothing
   outside them. This is what makes parallel lanes safe — branches don't share files, so they don't
   conflict. The owned dirs are recorded in the lane's charter and enforced by every spec's *Out of
   scope*.
3. **Manager authors the whole chain first.** A charter in [`wiki/lanes/`](../lanes/) plus the 2–4
   chained spec+prompt pairs, in dependency order, with each spec independently gateable. The chain
   should be adversarially checked (order sound, ownership disjoint, each spec leaves the branch
   green) before dispatch — a bad chain runs an agent several specs in the wrong direction unattended.
4. **Self-gate every spec; rebase per boundary; keep the branch local until the milestone.** The
   agent runs the full pinned gate (`python tools/doc_check.py` + an independent source-grounded review)
   at each spec's close and rebases the branch on `main` at each spec boundary, so drift stays small
   and the branch is always green. **Do not push the lane branch between specs** — push it **once**,
   at the milestone (rule 5). Pushing it per spec and then rebasing creates non-fast-forward history,
   which forces either a banned force-push or an ugly `ours`-merge workaround. Per-spec **reports**
   still go straight to `main` (doc-only), so the branch itself never needs an early push.
5. **Integrate once, at the milestone.** When the lane's milestone (defined in the charter) is met,
   the agent pushes the branch + tip SHA and an **independent integrator** runs the gate and `--no-ff`
   merges the whole branch — the normal [merge authority](../contracts/git-and-gates.md#merge-authority)
   rules apply (onboarding ⇒ integrator merges; the author never integrates their own code).
6. **Surface only at milestones or real blockers.** Between dispatch and the milestone the agent
   works autonomously; it interrupts the relay only for a genuine decision, a blocker, or the
   milestone merge. Reports are still written per spec (doc-only → straight to `main`).

## Concurrency, integration & graduation

- **Assignment.** The Manager assigns each lane to the surface that best fits the work's difficulty
  and current load — harder, higher-stakes lanes go to the more-capable surfaces.
- **Concurrency.** Run at most **2–3 lanes at once**, and only while their owned directories are
  **disjoint** — the Manager confirms disjointness before dispatching lanes in parallel.
- **Integrator.** **Claude Dev (`CC`) is the default milestone integrator** (the senior surface is
  the standing quality gate). When the lane being integrated is **CC's own**, **Codex Dev (`CX`)**
  integrates instead — the author never integrates their own code (merge authority is unchanged).
- **Cross-lane dependencies.** A lane that needs another lane's output is **sequenced after** that
  lane integrates to `main` — lanes never build against another lane's un-merged work. (The data
  Foundation lane is the universal dependency, so it runs first.) A genuine mid-chain dependency is a
  Manager call made at charter time, never improvised by the agent.
- **Graduation credit.** A **clean lane milestone** — the integrator independently reproduces the
  green gate from a clean checkout and the branch hygiene is clean — counts toward a surface earning
  graduated [self-merge authority](../contracts/git-and-gates.md#merge-authority), the same bar as a
  clean self-merge audition.

## Lane charter (in `wiki/lanes/<lane>.md`)
Owner; branch; owned directories; the ordered spec chain (one line each); the **milestone**
(the integration-ready definition); new dependencies; and any chain-review notes to carry into
execution. (the project's first lane charter will be the worked example once a multi-spec lane is authored.)

## When NOT to use a lane
One-off changes, docs, and anything a single spec covers. Lanes are for genuinely multi-spec,
internally-dependent feature work that one agent can own end-to-end on a disjoint slice of the tree.
