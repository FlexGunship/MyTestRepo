# First boot — Claude Manager's project-bootstrap ritual

**You are CM (Claude Manager) on a fresh project scaffolded from the agentic-start-kit.** The owner has
said something like "bootstrap this project." Your job is to turn this generic skeleton into *this*
project — interviewing the owner for the charter and pinning the specifics — so the owner only ever
talks to you. Work conversationally; don't make the owner edit files.

## Step 1 — Interview the owner for the charter
Ask, concisely (batch the questions; accept brief answers):
1. **Name** of the project (and the GitHub repo, if not already created).
2. **Mode:** are we **documenting / reverse-engineering existing code**, **building new software**, or
   **both**? (This sets the gate and whether `reference/` is used.)
3. **What** — one or two sentences on what this project produces.
4. **Who** — the audience / users.
5. **Why** — the problem it solves; what done looks like.
6. **North star** — the end goal beyond the immediate deliverable (e.g. a shipped product; a
   replacement-requirements doc traceable to a legacy system; an as-built reference). For RE projects,
   confirm whether the north star is documentation alone or a requirements/replacement doc.
7. **Scope** — what's in v1, what's explicitly out (for now).
8. **Toolchain** (build mode) — language/framework, so you can pin the gate commands.
9. **Surfaces** — confirm the core roster (CM + CC + CX); note any extras the owner plans to add.

## Step 2 — Write the charter
From the answers, write **`wiki/product-charter.md`** using
[`../templates/product-charter.md`](../templates/product-charter.md). It is the authoritative
What/Who/Why/North-star/Principles/Scope/Success. Commit it.

## Step 3 — Pin the specifics
- **`CLAUDE.md`:** set the project name; fill **The gate** with the real commands (build-and-test for
  build mode — see `wiki/contracts/git-and-gates.md`; the docs gate `python3 tools/doc_check.py` + the
  source-grounded review for RE/doc mode; both for hybrid). Start the **Status & Roadmap** with a
  bring-up entry.
- **`reference/`:** keep it (RE/doc mode — tell the owner to paste the subject source; it's read-only,
  byte-faithful) **or delete it** (build mode; source goes in `lib/`/`src/`).
- **`docs/`:** shape its Coverage to the deliverable, or remove if the project keeps no doc deliverable.
- **`CHANGELOG.md`:** create at first release (build/product mode); skip for doc-only projects.

## Step 4 — Stand up the team (worktree topology)
Establish git (if not already a repo with a remote), then create developer worktrees off `repo-master`
so CM holds `main` and each dev has its own checkout:
```
# from repo-master, with main pushed:
git worktree add ../worktrees/cc1 -b feature/cc1-onboard
git worktree add ../worktrees/cc2 -b feature/cc2-onboard
git worktree add ../worktrees/cx1 -b feature/cx1-onboard
git worktree add ../worktrees/cx2 -b feature/cx2-onboard
```
Then author the **onboarding specs** (one per surface) per
[`../rituals/agent-onboarding.md`](../rituals/agent-onboarding.md) and dispatch each surface to prove it
can see the repo, run the gate, and push. (Headless dispatch: `claude -p '<prompt>' … < /dev/null` /
`codex exec … < /dev/null` — always redirect stdin so codex can't hang.)

## Step 5 — First real work
Author the first numbered specs per the project's arc:
- **RE/doc mode:** import the subject (spec 001) → overview pass (repository map + tech stack →
  architecture) → subsystem deep-dives → registers → requirements synthesis + traceability.
- **Build mode:** a thin vertical slice first (prove the toolchain + gate end-to-end), then feature lanes.

## Step 6 — Delete this file's assumptions, keep the kit honest
Once bootstrapped, this `wiki/bootstrap/` ritual has done its job; leave it for reference. As the project
teaches you generic lessons, remember to **contribute them back to the kit**
([`../rituals/contributing-back.md`](../rituals/contributing-back.md)) at the end of the project.
