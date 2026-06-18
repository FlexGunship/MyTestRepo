# Ritual: Spec & Prompt Format

A unit of **Now** work is **two files**, both committed to `wiki/specs/`:

- **Spec** — `NNN-XX-slug.md` — *what* and *why*. Durable; the stable statement of intent.
- **Prompt** — `prompt-specNNN-XX-slug.md` — *how to execute*: the ceremony, the gate, the
  report-back format, the versioning ritual. This is the message handed to the developer.

`NNN` = the spec number (see [numbering](../README.md#numbering)). `XX` = the router tag
(`CC` / `CX1` / `CX2`) saying who executes. `slug` = short kebab-case title.

> Pure investigation or audit specs may be a **single self-contained file** with no prompt, if
> there is no code to ship — the spec body carries the audit instructions directly.

Copy-paste starting points: [`templates/spec.md`](../templates/spec.md) and
[`templates/prompt.md`](../templates/prompt.md).

---

## Spec structure (`NNN-XX-slug.md`)

```markdown
# Spec NNN-XX — <title>

## Status
- Doc type: <implementation | investigation | bugfix | ...>
- Executes: <CC | CX1 | CX2>; merge: <self-merge (graduated) | integrator-merged (onboarding) | author==executor flagged>
- Number NNN verified free (highest in wiki/specs/ = MMM; NNN = MMM + 1).
- Paired prompt: prompt-specNNN-XX-slug.md
- Final on-disk locations after merge: <paths>

## Background
Why this work exists. Institutional context. What problem it solves.

## Decisions made
Numbered Manager calls. The developer implements as stated **unless they find a strong reason to
deviate — in which case they flag it in the report rather than silently choosing otherwise.**

## Scope — what to build
Subsections as needed: directory structure, components, data shapes, behavior, tooling.

## Out of scope
Explicit exclusions. What this spec deliberately does NOT touch.

## Working model
(Detail in the prompt file.) A one-line pointer to the prompt.

## Definition of done
A checklist of completion criteria. Always ends with the gate: "Gate green. Merged."
(If no code changes, assert what must remain byte-identical / unchanged.)

## Deliverable / report-back
"See the prompt file for the report-back format."
```

---

## Prompt structure (`prompt-specNNN-XX-slug.md`)

The prompt is the operational half. It carries:

```markdown
# Prompt — Spec NNN-XX — <title>

## Setup
- Pull main; branch `feature/<xx>-<slug>` from main.
- (Audit specs: git fetch && checkout main && pull --ff-only; record the SHA.)

## Steps
Ordered, concrete execution steps.

## The gate
The exact commands to run (separately) before merge. (See contracts/git-and-gates.md.)

## Versioning ritual
(For versioned ships — the ordered ritual from the git & gates contract.)

## User-facing changelog content
(Manager writes this verbatim, for the developer to paste into CHANGELOG.md. Friendly names
only. Omit for non-user-visible work.)

## Report-back format
The exact sections the report must contain (see rituals/report-format.md), plus any
spec-specific sections.
```

---

## Conventions

- **Search before drafting.** Find the highest number in `wiki/specs/`; the new number is
  `highest + 1`. Verify it is free.
- **Numbers never reused**, even for withdrawn specs.
- **One spec = one call-sign = one agent (the human routes by filename).** The `XX` tag in the
  filename is the *sole* routing key: the owner scans the title, sees one call-sign, and relays
  "spec NNN" to that one agent. Any action a *different* agent must take — integrating a peer's
  branch, reviewing it, reconciling it — is **invisible if buried in the spec body** and will
  therefore never be dispatched. It must be its **own numbered spec titled with that agent's
  call-sign** (e.g. integrating CX1's `033` branch is a separate `0NN-CC-integrate-…` spec).
  Numbers are free; never hide a second agent's work inside another agent's spec.
- **Committed specs are immutable (owner rule).** Once a spec is committed, do **not** edit it — not
  to add scope, retask it, correct a premise, or attach an addendum. **Write a new spec at the next
  number** that names what it supersedes (e.g. "Supersedes 008-CX"). Corrections, follow-ups,
  audition/merge changes, and verifications are all **new specs**. *(Ritual / contract / role docs
  are living and still edited in place — this rule applies to `NNN-XX-slug.md` specs and their
  prompts.)*
- **The Manager authors specs and prompts.** A developer may draft a follow-up or integration
  spec at Manager quality, but **never ships a self-authored spec without Manager review.**
- **Specs are hypotheses about existing behavior.** If the spec's claim about how the code
  behaves is wrong, the developer raises it (Q&A or a relayed flag) before implementing — they
  do not silently "correct" the spec by guessing.

---

## Changelog
- 2026-05-30 — Added the **one-spec-one-call-sign** routing rule (owner): the filename tag is the
  sole routing key; a second agent's action (integration/review/reconciliation) is its own numbered
  spec titled with that agent's call-sign, never buried in the body.
- 2026-05-29 — Added the **immutable-specs** rule (owner): committed specs are never edited; new
  work, corrections, addenda, and supersessions each get a new number. Dropped a stale two-hat
  reference.
- 2026-05-28 — Initial.
