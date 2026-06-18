# Ritual: Contributing learnings back to the kit

This project was scaffolded from the **agentic-start-kit** (a GitHub template repo). The kit is a
**living template** — it should get better after every project. At the end of a project (or at a major
milestone), the experienced **CM** harvests the genuinely-generic lessons this project earned and
contributes them **back** to the kit via a pull request, so the next project starts from a sharper
framework than this one did.

## What to promote (generic only)
Promote a lesson **only if it would help an unrelated future project** — and strip every project
specific:
- A new **best-practice** (an anti-drift lesson proven here) — neutralize the example (no project names,
  no spec numbers; "a fault-table claim" not "spec 033").
- A **sharper ritual or contract wording** (a gate step, a conflict-resolution check, a review tactic).
- A **tooling fix or new helper** (e.g. an improvement to `tools/doc_check.py` / `tools/cite_audit.py`).
- A **role/onboarding** clarification.

## What to NEVER push back
- Any project content (specs, reports, reviews, passdowns, Q&A, the `docs/` deliverable, `reference/`
  source, the charter).
- This project's accumulated **surface learnings** (`wiki/surfaces/*`) — those are per-project; only the
  *generalizable* lesson inside them gets promoted to `best-practices.md`.
- Project-named status/roadmap, toolchain pins, or owner-action specifics.

## How
1. `git clone` the kit repo (or your fork).
2. Apply the genericized lesson to the right file(s); keep diffs small and one-theme-per-PR.
3. Run the kit's own `python3 tools/doc_check.py` (it must stay gate-green) and confirm no project
   tokens leaked (`grep -rinE '<your project name>|<subject names>' .` → empty).
4. Open a PR titled for the lesson; in the body, state the project-neutral rationale (the *why*, not the
   project). The kit maintainer (you, the owner) reviews and merges.

## Why this matters
The compounding loop is the whole point: personal lesson → proven general on a project → promoted to the
team lore → **shipped to every future project via the kit.** A kit that doesn't get fed goes stale; a kit
that does makes each project cheaper and cleaner than the last.

## Changelog
- (kit) Initial — establishes the back-contribution loop so the template improves over time.
