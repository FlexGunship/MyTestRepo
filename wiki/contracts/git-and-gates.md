# Contract — Git, Branches, Gates & Versioning

`main` is the single integration trunk and is **always green**. This is the integration contract every
surface is bound by. It is **toolchain-neutral**: the *shape* of the gate flexes to the project (see
[The gate](#the-gate)); the discipline below does not.

## The integration trunk
`main` is modified **only** through a **gate-passed `--no-ff` merge from a feature branch** — never a
direct commit. The gated merge is the only path to `main` for a deliverable. No exceptions for "tiny."

## Branch discipline
- **Branch from `main`** for every work unit; **pull `main` at the start** of every work unit.
- Commit **frequently, in small granular commits**; **merge back as the closing step** with `--no-ff`
  on a fully green gate; keep branches **short-lived** (exception: [lanes](../rituals/lanes.md)).
- **`git fetch --prune` before any branch enumeration or sync** (stale refs cause real wasted work).
- **No force-push.** Renumber with `git mv`, never history rewrites.
- **Branch naming:** `feature/<surface>-<slug>` (e.g. `feature/cc1-…`, `feature/cx2-…`).

## The gate
Before any deliverable merges to `main`, the full gate is green. **Run each command separately — never
chain them** (a chain hides which step failed). The exact commands are **pinned in
[`/CLAUDE.md`](../../CLAUDE.md)** once the toolchain exists. The gate's *shape* depends on what the
project produces — pick what fits (a project may use both):

**Build projects (writing software).** A build-and-test gate:
```
<build>        # compiles/bundles cleanly
<typecheck>    # static types pass (per target if multiple)
<lint>         # linter clean
<test>         # full suite green; assertions must be able to fail
```
Record the exact commands + pass/fail with **counts**, the **SHA the gate ran clean at**, and any files
changed outside the spec's list.

**Documentation / reverse-engineering projects (describing a read-only subject).** A docs gate:
```
python3 tools/doc_check.py     # links resolve, structure sane, no leftover placeholders / artifacts
```
…**plus the real teeth — an independent source-grounded review:** every non-trivial claim **cites the
source** (`reference/<path>:<line>` or a stable symbol); the integrator **spot-checks each citation
against the actual source** and HOLDs anything unsupported. `tools/cite_audit.py` dumps each citation
beside its source line to make that check reliable. An un-cited or source-contradicted claim is a
BLOCKER, exactly like a red build.

**A green gate is necessary, not sufficient** — for risky surface area, run an adversarial pre-merge
review (see [best-practices](../best-practices.md)).

## What ships, and where it goes
| Kind | Path to `main` |
|---|---|
| **Deliverables** (the software in `lib/`/`src/`, or the cited docs in `docs/`) | **Branch → gate → cross-model integrate** (author ≠ integrator) |
| **Process docs** (specs, prompts, reports, reviews, passdowns, Q&A, wiki edits) | **Ship freely** — straight to `main`, no review gate (under worktrees, landed mechanically by CM — see below) |
| **A read-only subject** (`reference/`, if any) | **Read-only input** — documented, never edited except by an explicit owner-approved spec |

## Merge authority
- **Onboarding (default for every new surface):** the developer pushes its branch + tip SHA; an
  **independent integrator — a different surface, cross-model by default (Claude↔Codex)** — runs the
  gate and performs the `--no-ff` merge. The author does **not** self-merge.
- **Graduated (earned):** after a track record of clean, reconciling reports, the owner may grant
  self-merge authority. Author ≠ integrator always holds for a given change.

## Worktree topology (CM-centralized landing)
Surfaces run as **git worktrees off `repo-master`** (`cm/cc1/cc2/cx1/cx2`, one object store). Because a
branch can be checked out in only one worktree, a dev worktree **cannot hold `main`** — so the *review*
and the *merge* are split without weakening independence:
- The **cross-model integrator** checks out the author's branch, runs the gate + review, and writes an
  integration report ending in **`VERDICT: PASS` or `VERDICT: HOLD`** (it owns the accept/reject call).
- **On PASS, CM (holder of `main` in `repo-master`) does the mechanical `--no-ff` landing** (and flips
  any Coverage row). CM does not re-review — the merge act is clerical, forced by the topology; it is
  not self-approval (CM neither authored nor decided). On HOLD, nothing lands; findings route back as a
  new spec.
- **Process docs** the same way: author pushes the branch, CM fast-forwards/`--no-ff` lands to `main`
  (no review gate — "doc-only ships flow freely"; this just satisfies the can't-hold-main constraint).
  *(On a single-checkout repo with no worktrees, authors commit doc-only ships directly to `main`.)*

## Conflict resolution (mandatory pre-commit verification)
After resolving any merge conflict, **before `git commit`**, run both — non-negotiable:
```bash
grep -cE '^(<<<<<<<|=======|>>>>>>>)' <each conflicted file>   # expect 0 — catches silently-failed edits
git diff --name-only --diff-filter=U                           # expect empty — catches forgot-to-add
```
A merge commit with raw conflict markers is the worst kind of broken `main`.

## Auditing existing code
Any spec that audits/traces existing code first runs `git fetch && git checkout main && git pull --ff-only`
and **records the audited SHA** in the report. Auditing a stale checkout draws conclusions about code
that no longer exists.

## Versioning ritual (versioned/build ships only)
For user-visible ships, in order (tool commands per toolchain; the *order + record-keeping* is the
contract): test → build → update `CLAUDE.md` Status & Roadmap → update `CHANGELOG.md` (user-facing voice;
created at first release) → **bump** (read the live version and bump from it — never hardcode) → commit →
**tag** → gated merge + push with tags → author the report. Bump: fix→patch, feature→minor,
breaking→major, docs/internal→patch-or-none. *(Documentation-only projects skip this — track progress by
spec/report numbers + the Status & Roadmap; the owner calls any milestone tag.)*

## Standing rules
- The owner is the **sole relay**; surfaces never cue each other directly.
- **Never print or commit** any secret/token.
- Reconcile an advanced `origin/main` by **merge** (`git pull --no-rebase`), never rebase past a merge
  you intend to keep.
- Do not modify a read-only subject to make anything pass — it is the subject, not a workspace.
