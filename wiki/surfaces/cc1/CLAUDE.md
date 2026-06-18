# CC1 — Claude Dev — learnings

My durable lessons, accumulated across this project. I read this at the start of every run and append
to it at the end (see [the protocol](../../rituals/surface-learnings.md)). The shared contract lives in
my [role doc](../../roles/claude-dev.md), [best-practices](../../best-practices.md), the
[git & gates contract](../../contracts/git-and-gates.md), and the rituals in `wiki/rituals/` — I read
those every run too. This file is only what's specific to *me* on *this* project.

## Who I am
A Claude execution surface (`CC1`) in worktree `worktrees/cc1`, on `feature/cc1-<slug>` branches.
I execute committed specs; I push branches and (cross-model) integrate peers' work; I do not self-merge
my own deliverables until graduated. The subject under `reference/` (if any) is read-only.

## Lessons (most-load-bearing first)
- **Keep the env-key / fallback decision OUT of a pure selection helper.** For the 028 capstone, the
  "which pipeline" type-selection (`PipelineFactory.Create`) had to be testable for the *real* path with
  **no key** — so the `ANTHROPIC_API_KEY` check + warn-and-fall-back lives in `Program`, not the helper.
  A helper that did the key-check would fall back to fakes in a keyless test and you couldn't assert the
  real adapter types. Separate "which types" (pure, testable) from "is the key present" (composition).
- **Make a new collaborator an *optional* ctor param to avoid churning prior tests.** Adding the digest
  notifier to `SweepHost` as `IDigestNotifier? notifier = null` (→ `NullDigestNotifier`) left the 015
  4-arg `SweepHostTests` and behaviour untouched while wiring the new sink.
- **Prompt shorthand ≠ real signature — verify the ctor.** Prompt said `FileDigestNotifier(DigestPath,
  () => …)`; the real ctor is `(path, subject, timestamp)`. Read the file, supply `Subject`.

## Append log
| Date | Spec | Lesson |
|---|---|---|
| 2026-06-18 | 028-CC | Pure selection helper for real-vs-fake (key-check stays in Program); optional notifier ctor param avoids test churn; verify ctor signatures vs prompt shorthand. |
