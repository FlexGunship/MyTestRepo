# Ritual: Ship Report Format

Every spec closes with a **report**, authored by the executing developer into `wiki/reports/`,
named `report-specNNN-XX-slug.md`. The report is the durable evidence that the work happened,
that the gate was green, and that nothing unexpected was hidden.

A report is a **memorial**: append-only by convention. You don't rewrite a shipped report's
history; if a follow-up correction is needed, it gets its own report or a Q&A.

Copy-paste starting point: [`templates/report.md`](../templates/report.md).

---

## Required structure

> **Section names are load-bearing.** The owner and future agents scan reports by section name to
> find a known piece of evidence (the gate table, the changes table, the surprises). If your
> work doesn't naturally fit a section, write **"None."** or **"N/A — <why>"** rather than
> dropping the section or folding its content into a neighbor. (Lesson from report 057-CC, which
> had to append a format-conformance addendum because the Changes / Surprises / Roles-update
> sections were folded into adjacent ones.)

```markdown
# Report — Spec NNN-XX: <title>

**Headline outcome:** <one or two sentences. MERGED / not merged; version shipped or
"no version bump"; branch state; gate green.>

## 1. Branch / merge state
- Pre-merge `main` SHA: <exact, from git rev-parse>
- Feature branch: <name>; working commit(s): <SHA(s)>; branch deleted post-merge: <y/n>
- Post-merge `main` SHA (pushed): <SHA>
- Merge mechanic: <--no-ff self-merge | pushed branch, integrator merged>

## 2. Changes
Table of files touched + a one-line description of each.

## 3+. Spec-specific sections
Whatever the prompt's "Report-back format" asked for. File + line references where relevant.

## Gate results
A table: each gate command → ✓/✗ with counts.
- Test count **before** and **after** the change (from actual runner output).
- The SHA at which the gate ran clean.
- Any files changed that were NOT in the spec's files-to-change list.

## Sources beyond the brief / surprises
Things you encountered that the spec did not name. Anything that surprised you. Flag for the
Manager.

## Deferred / not done
Verifications or sub-tasks you deferred, named explicitly. "Probably fine" is not a diagnosis.

## Standing flags
Pre-existing, out-of-scope blockers you noticed but did not touch.
```

---

## Required elements (the rigor checklist)

- **Exact commit SHAs** — from `git rev-parse HEAD`, never "approximately."
- **Real test counts** — before and after, from actual runner output.
- **Gate confirmation** — each command, run separately, with its result.
- **Deferred verifications named explicitly** — not glossed.
- **Judgment-heavy decisions flagged** — where you deviated from the spec or made a call the
  spec didn't make, say so and why.
- **Audit findings quoted verbatim** — don't paraphrase a finding into vagueness.
- **Staleness flagged** — stale docs/comments you noticed (flag, don't fix in this commit).
- **Anything that surprised you.**
- **If anything doesn't reconcile, that is the headline** — top of the report, not buried.
- **Roles update notice** — if you edited a role doc this session, note it here for relay.
- **Author == executor disclosure** — if the same surface authored and executed (rare, now that surfaces are separate), say so as the headline so
  the owner knows the independent-review check was not performed.

---

## Changelog
- 2026-06-02 — Added the **"Section names are load-bearing"** emphasis above the required
  structure: when a section doesn't apply, write **"None."** rather than dropping it. Codifies
  the lesson from report 057-CC (Changes / Surprises / Roles-update sections were folded into
  adjacent ones, requiring a format-conformance addendum).
- 2026-05-28 — Initial.
