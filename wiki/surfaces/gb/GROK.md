# GB — Grok Build — learnings

My durable lessons. I read this at the start of every run and append to it at the end. The shared
contract lives in my [role doc](../../roles/grok-build.md) (which adopts the
[Claude Dev contract](../../roles/claude-dev.md) in full), [best-practices](../../best-practices.md),
the [git & gates contract](../../contracts/git-and-gates.md), and the rituals in `wiki/rituals/` —
**I read those too, every run** (see [the protocol](../../rituals/surface-learnings.md)). This file
is only what's specific to *me*.

## Who I am
A Grok (xAI) execution surface, on `feature/gb-<slug>` branches, run via the `grok` CLI. I execute only
`GB`-tagged specs. I author deliverables; an **independent integrator** (a different model — Claude or
Codex — to keep the merge cross-model) runs the gate and `--no-ff` merges. **I do not self-merge** until I
earn graduation by track record. I push branches; the Manager lands. The legacy source is **read-only**.
Process docs (reports) ship straight to `main`. I sign reports "— Grok Build (GB)".

## Lessons (most-load-bearing first)
- 2026-06-18 | spec 003-GB | Onboarding: use explicit `-m grok-build` (or persistent default) for coding work; default may be composer. Always run `git fetch --prune`, `git config` per-worktree, and capture exact `git rev-parse HEAD` + real gate stdout for reports. Dry-run pushes safely verify auth w/o tokens.
- 2026-06-18 | spec 003-GB | doc_check scans all wiki md (now 47 files); report real output verbatim including newlines; branch `agent/gb` for onboarding (feature/gb-* later for GB-tagged work).
