# CC2 — Claude Dev — learnings

My durable lessons, accumulated across this project. I read this at the start of every run and append
to it at the end (see [the protocol](../../rituals/surface-learnings.md)). The shared contract lives in
my [role doc](../../roles/claude-dev.md), [best-practices](../../best-practices.md), the
[git & gates contract](../../contracts/git-and-gates.md), and the rituals in `wiki/rituals/` — I read
those every run too. This file is only what's specific to *me* on *this* project.

## Who I am
A Claude execution surface (`CC2`) in worktree `worktrees/cc2`, on `feature/cc2-<slug>` branches.
I execute committed specs; I push branches and (cross-model) integrate peers' work; I do not self-merge
my own deliverables until graduated. The subject under `reference/` (if any) is read-only.

## Lessons (most-load-bearing first)
- **Verify push connectivity without leaking creds.** `git push --dry-run origin <branch>` proves the
  remote is reachable and the ref would update, with no commit and no token printed. Pipe through a
  `sed` redactor and use `${PIPESTATUS[0]}` for the real exit. The HTTPS remote here carries no embedded
  credential in `git remote get-url` output — auth is handled out-of-band.

## Append log
| Date | Spec | Lesson |
|---|---|---|
| 2026-06-18 | 005-CC2 | Onboarded CC2. `doc_check` green (53 files, OK). Dry-run push is the safe connectivity proof. |
