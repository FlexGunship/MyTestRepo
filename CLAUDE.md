# CLAUDE.md — <PROJECT NAME>

> **Starter-kit skeleton.** On first boot, Claude Manager fills in the project name, the pinned gate
> commands, and the Status & Roadmap from the [first-boot bootstrap](wiki/bootstrap/first-boot.md).
> Until then this is the generic operating context every agent reads at the start of every session.

This file is the **dev-facing onboarding pointer** and the canonical **Status & Roadmap**. Every agent
reads it at the start of every working session, after its role doc.

## Read this first, every session
1. Your role doc in [`wiki/roles/`](wiki/roles/) (CM / CC / CX).
2. The other agents' role docs in the same folder (knowing your counterparts keeps the
   plans-vs-executes separation clean).
3. The most recent passdown in [`wiki/passdowns/`](wiki/passdowns/).
4. [`wiki/best-practices.md`](wiki/best-practices.md), the [git & gates contract](wiki/contracts/git-and-gates.md),
   and the relevant [rituals](wiki/rituals/).
5. Your own learnings file in [`wiki/surfaces/`](wiki/surfaces/README.md), and append to it at run end.
6. Open questions in [`wiki/qanda/`](wiki/qanda/).
7. This file's **Status & Roadmap** below; and the operating manual [`wiki/README.md`](wiki/README.md).

## The gate
`main` is the single integration trunk and is **always green**. The only path to `main` for a
deliverable is a **gated `--no-ff` merge from a feature branch**, performed by an **independent
cross-model integrator** (author ≠ integrator). The gate is **flexible — it is whatever proves this
project correct**, pinned here at first boot. The two proven shapes (see
[git & gates](wiki/contracts/git-and-gates.md)):

```bash
# Build projects (writing software): run each separately, never chained.
<build>        # compiles/bundles cleanly
<typecheck>    # static types pass
<lint>         # linter clean
<test>         # full suite green, assertions that can actually fail

# Documentation / reverse-engineering projects (describing a read-only subject):
python3 tools/doc_check.py     # links resolve, structure sane, no leftover placeholders
#  + the real teeth: an independent source-grounded review — every non-trivial claim cites
#    reference/<path>:<line>; the integrator spot-checks each citation (tools/cite_audit.py helps).
```

A project may run **both** (a build project whose docs are also cited). Doc-only ships (reports, Q&A,
wiki edits) go straight to `main` (no review gate). Pin the exact commands here once the toolchain exists.

## Status & Roadmap
> Dev-facing changelog. ~5–15 lines per entry. Updated as a step in every ship. (User-facing notes,
> if the project releases a product, go in `CHANGELOG.md`.)

### Unreleased
- _(empty — first real entry is written when work begins)_
