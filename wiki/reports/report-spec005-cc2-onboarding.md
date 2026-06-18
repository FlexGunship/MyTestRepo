# Report — Spec 005-CC2: Claude Code 2nd instance (CC2) onboarding

**Headline outcome:** CC2 onboarded cleanly — environment-only, no deliverable. Preflight passed,
git identity set, context read in order, `doc_check.py` green from the CC2 worktree (53 files, OK).
Report committed on `agent/cc2` and pushed; **not** self-merged (CM lands process docs). No secrets
printed at any step.

## 1. Branch / merge state
- Branch: **`agent/cc2`** (home branch for CC2; deliverable work later uses `feature/cc2-<slug>` off `main`).
- Pre-commit tip SHA (this report committed on top of it): `c5f76080883a7ec7e0bb197c5db8b48072491e7d`
- Merge mechanic: **pushed branch; CM lands the report.** No self-merge (per spec + manager.md
  "Process docs under worktrees").
- Post-commit/push tip SHA: recorded in the closing line of this report after commit.

## 2. Changes
| File | Description |
|---|---|
| `wiki/reports/report-spec005-cc2-onboarding.md` | This onboarding report (new). |
| `wiki/surfaces/cc2/CLAUDE.md` | Appended one dated lesson + append-log row (spec step 4, optional). |

## 3. Onboarding evidence

### Preflight (spec step 0 — fail fast)
- `git ls-remote origin` → **succeeds** (exit 0).
- `python3 --version` → **Python 3.14.4** (≥ 3.8 ✓).

### Identity + sync (step 1)
- `git config user.name=CC2`, `git config user.email=cc2@agents.local` set in this worktree:
  `CC2 <cc2@agents.local>`.
- `git fetch --prune origin` → OK.

### Context read, in order (step 2)
`wiki/roles/claude-dev.md` → other role docs (`manager.md`, `codex-dev.md`, `grok-build.md`,
`surfaces.md`) → `wiki/surfaces/cc2/CLAUDE.md` → `/CLAUDE.md` Status & Roadmap → latest passdown
(`wiki/passdowns/2026-06-18-build-arc.md`) → `wiki/qanda/` (**empty — no open questions**) →
`README.md` → `wiki/contracts/git-and-gates.md` → `wiki/best-practices.md`. All read.

### doc_check (step 3 — real output)
```
$ python3 tools/doc_check.py
doc_check: scanned 53 markdown file(s).
doc_check: OK - links resolve, no placeholders in docs/.
exit=0
```

### Push connectivity (step 5 — no token printed)
`git push --dry-run origin agent/cc2` returned exit 0 with `* [new branch] agent/cc2 -> agent/cc2`,
piped through a redactor — no credential appears in any output. `git remote get-url origin` is a plain
HTTPS URL with no embedded credential (auth handled out-of-band). The real (non-dry-run) push of this
commit is the closing step.

## Gate results
Onboarding gate is the docs gate (`doc_check.py`), not the .NET build gate (per spec Decision 1).

| Command | Result |
|---|---|
| `git ls-remote origin` (preflight) | ✓ exit 0 |
| `python3 --version` (preflight) | ✓ Python 3.14.4 (≥ 3.8) |
| `git fetch --prune origin` | ✓ OK |
| `python3 tools/doc_check.py` | ✓ exit 0 — 53 files scanned, "OK - links resolve, no placeholders" |
| `git push --dry-run origin agent/cc2` | ✓ exit 0 — new branch, no token printed |

- Test count: N/A — onboarding is environment-only; no product code, no .NET test suite run.
- SHA the gate ran clean at: `c5f76080883a7ec7e0bb197c5db8b48072491e7d` (pre-commit).
- Files changed outside the spec's list: none (report + optional learnings lesson, both named in scope).

## Sources beyond the brief / surprises
None. Q&A was empty as the spec anticipated. Roster register (`surfaces.md`) already lists CC2 as
onboarding (spec 005); structural rows pre-existed as the spec stated.

## Deferred / not done
- The mechanical landing of this report to `main` is **CM's** step (no self-merge) — not a deferral on
  my part, a separation-of-duties requirement.
- No deliverable / product code / `.exe` — out of scope by design.

## Standing flags
None.

## Roles update notice
No role docs edited this session.

— Claude Dev 2 (CC2)
