# Ritual: Subsystem deep-dive (page template) — *optional, for documentation/RE projects*

*This is a deliverable pattern for **documentation / reverse-engineering** projects — skip it for build
projects.* A **subsystem deep-dive** is a `docs/subsystems/<slug>.md` page that documents one subsystem
(from the project's architecture-overview page, once it exists) at the depth a maintainer needs: its
components, interfaces, internal flows, external dependencies, and the NFR/safety/intent observations
that feed the requirements/synthesis deliverable (see the [charter](../templates/product-charter.md)).
It is **depth on one subsystem** — it links up to the overview pages for breadth, never re-derives them.

Every deep-dive is **source-grounded**: each non-trivial claim cites `reference/<path>:<line>` per the
[citation discipline](../best-practices.md#citation-discipline--for-documentation--reverse-engineering-projects)
— cite the **load-bearing line** (don't flood), and **build the completeness self-audit in from the
first draft**: run
**`python3 tools/cite_audit.py docs/subsystems/<page>.md`** (dumps each citation beside the actual
source line) and read each `SRC:` line against its claim — the cited line must *contain* the token, the
claim *type* must match (assembly ≠ namespace ≠ project file ≠ class ≠ member), and an **enumeration
claim cites every member it names**, not just the first. Read your role doc, best-practices, the
git & gates contract, and your own `wiki/surfaces/` learnings first (the
[surface-learnings ritual](surface-learnings.md)).

## Page structure

```markdown
# <Subsystem name> — subsystem deep-dive

<1–3 sentence intro: what this subsystem is and does in the project machine-control system; link the
architecture-overview subsystem section and the relevant repository-map rows.>

## 1. Purpose & scope
What the subsystem is responsible for, and its boundary — which projects/dirs it comprises (cited).
What is explicitly NOT in it (link to the neighbouring subsystem that owns it).

## 2. Components
One row per constituent project AND its key classes/types (deeper than the repo-map's per-directory
view). Columns: Component (project/class) · Responsibility (1 line) · Provenance tag · Citation.
Every named code entity carries a citation at the line that defines it.

## 3. Interfaces
The subsystem's public surface: entry points / APIs other subsystems call into, and what it calls out
to (with the establishing `using`/call/declaration line cited). Name the cross-subsystem edges (they
should reconcile with the architecture overview's dependency graph).

## 4. Internal control & data flow
The principal internal flows — threads, lifecycle/state, message/command handling, the sequence of a
representative operation — each step cited or tagged. A render-safe Mermaid diagram is optional; if
used, follow the gate's Mermaid rules and make every edge a cited prose claim.

## 5. External / black-box dependencies
Vendor SDKs, prebuilt binaries, external servers/daemons, hardware, OS/IPC/COM dependencies. Capture what the
*interface* implies; mark **behavior-to-recover**; never invent internals.

## 6. NFR / safety / timing
Real-time/timing guarantees, safety interlocks (EStop, amplifier-disable, watchdog/warning daemons),
precision/thermal, failure/error handling — note where found, tagged. (Feeds the dedicated NFR pass.)

## 7. Requirements observations
Per the charter north star: flag **essential vs. incidental** (a real requirement vs. a hardware/era
artifact), tag intent **sourced / inferred / needs-SME**, and mark black-box behaviors as
requirement-gaps. Cheap to note now, expensive to retrofit.

## 8. Open questions / needs-SME
What the code cannot answer.

## Revision history
| Date | Spec | Agent | Change |   (gate-required footer)
```

## Scope discipline
- **Depth, not exhaustiveness.** Document the key components, interfaces, and flows a maintainer needs
  — cite representative load-bearing lines, not every method. A focused, verified-correct page beats a
  sprawling, plausible one.
- **Boundary.** Link to the overview pages for breadth; do not re-list directories (repo-map), restate
  the tech stack (tech-stack), or re-draw the whole architecture (architecture-overview).
- **Reconcile up.** Cross-subsystem edges you describe must agree with the architecture overview's
  dependency graph; if you find a discrepancy, that is a finding — flag it.
- **One subsystem = one page = one spec**, branch → docs gate → cross-model integrate (author ≠
  integrator). The integrator reproduces the completeness audit independently before PASS.

## Changelog
- (kit) Initial. Deliverable-page template for documenting one subsystem of an existing codebase at
  depth (the depth phase of a documentation/RE project, after the breadth-first overview).
