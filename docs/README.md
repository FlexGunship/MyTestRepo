# Project deliverable wiki (`docs/`) — *the product, when the project produces documentation*

`docs/` is the **navigable deliverable** of the project — distinct from [`wiki/`](../wiki/README.md),
which is the team's *operating manual* (roles, specs, rituals). `wiki/` is **how the team works**;
`docs/` is **what the team produced**.

> **Shape this to the project.** For a **documentation / reverse-engineering** project, `docs/` is the
> as-built map of the subject under [`reference/`](../reference/README.md): a top-level overview
> descending to per-subsystem and per-component detail, every claim cited. For a **build** project,
> `docs/` typically holds design/architecture/decision records for the software being built (the
> software itself lives in `lib/`/`src/`). Claude Manager sets the shape at first boot; delete this
> file if the project keeps no `docs/` deliverable.

## The one rule (for citation-bearing docs): cite the source
Every non-trivial claim about the subject **cites where it rests** as `reference/<path>:<line>` (or a
stable symbol). An un-cited or source-contradicted claim fails the gate and does not ship. Citations
stay stable because the subject is stored byte-faithful (see [`reference/README.md`](../reference/README.md)).
The dump helper `tools/cite_audit.py` makes the self-check reliable.

## Authoring a page
Short intro → cited body → a **`## Revision history`** footer (date · spec · agent · change), which is
gate-enforced (see [`wiki/rituals/wiki-page-history.md`](../wiki/rituals/wiki-page-history.md)). If the
project targets a synthesis/requirements deliverable, tag intended use **sourced / inferred / needs-SME**
(never inferred-as-fact) and flag **implementation-artifact vs. real-requirement** as you go — see
[best-practices](../wiki/best-practices.md).

## Coverage
Status as the deliverable is built out. (`📋` planned · `🚧` in progress · `✅` landed)

| Section | Page | Status |
|---|---|---|
| _(add rows as the deliverable's shape becomes clear)_ | | 📋 |

## Revision history
| Date | Spec | Agent | Change |
|---|---|---|---|
| — | — | CM | Deliverable-wiki home (starter scaffold); Coverage filled in once work begins. |
