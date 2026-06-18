# Prompt — Spec NNN-XX — <title>

You are <CC | GD | CX>. Execute Spec NNN-XX (wiki/specs/NNN-XX-slug.md). Read the spec first.

## Setup
- `git fetch --prune`; `git checkout main`; `git pull --ff-only`.
- Branch `feature/<xx>-<slug>` from main.
- (Audit specs only: record the audited `main` SHA in the report.)

## Steps
1. <concrete step>
2. <concrete step>

## The gate
Run each separately (see wiki/contracts/git-and-gates.md):
- <build>
- <typecheck>
- <lint>
- <test>

## Versioning ritual  (versioned ships only)
Follow the ordered ritual in wiki/contracts/git-and-gates.md: test → build → update CLAUDE.md
Status & Roadmap → update CHANGELOG.md → bump (read live version) → commit → tag → gated merge
+ push → report.

## User-facing changelog content
<Manager writes this verbatim for the developer to paste into CHANGELOG.md. Friendly names
only. Omit for non-user-visible work.>

## Report-back format
Write wiki/reports/report-specNNN-XX-slug.md per wiki/rituals/report-format.md, plus:
- <any spec-specific section>
