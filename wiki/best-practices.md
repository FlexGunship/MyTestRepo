# Best Practices — Engineering Anti-Drift Lore

These are the hard-won lessons the team carries forward. They are not style preferences; each
one is here because ignoring it cost someone real time on a prior project. Read them; they're
the difference between a report you can trust and one you can't.

---

## Verification

- **Verify against an external oracle, not the incumbent.** Correctness means matching an
  *independent* source of truth — the spec, a hand-worked expected result, or a trusted
  reference implementation. "It matches the previous implementation" only proves you preserved
  the previous implementation's bugs.
- **Exercise real, representative data.** Synthetic test-helper fixtures prove the code runs;
  they don't prove it's right. Use realistic inputs.
- **Expected values come from the spec, not the implementation.** When a test's expected value
  disagrees with the code, do not edit the expected value to make the test pass. Trace which
  side is wrong.
- **Assertion-free tests are a trap.** A test with no meaningful assertion passes
  unconditionally and validates nothing. Every test you add must be able to fail.
- **Confirm tests actually run.** Test runners silently skip files outside their configured
  roots. A "passing" suite that never executed your new test is worse than no test.

## Reports & honesty

- **Report polish is not report correctness.** A longer, more confident report is not more
  correct. For high-stakes findings, route the same brief to two surfaces independently; a
  divergence is itself a finding.
- **Exact SHAs, real counts.** From `git rev-parse HEAD` and actual runner output. Never
  "approximately."
- **"Probably fine" is a characterization, not a diagnosis.** Trace it to a cause or name it as
  unresolved. Don't launder uncertainty into false confidence.
- **If it doesn't reconcile, that's the headline.** Surprises and inconsistencies go at the top
  of the report, not in a footnote.

## Pre-merge review (when the gate isn't enough)

- **A green gate ≠ correct.** Every test in this suite uses fakes that never throw, never time
  out, and never re-authenticate. The gate proves the code compiles, types align, and the fake
  paths run — it cannot see real-device timing, error paths, sign-out races, or orphan-cleanup
  gaps. Integrators landing risky lanes should remember this: **green is necessary, not
  sufficient.**
- **For cross-model merges of risky surface area** (anything touching async state, auth, capture
  pipeline, security rules, user-isolation), run an **adversarial pre-merge review** before
  pushing — parallel reviewers fanning out across slices, per-finding adversarial verification,
  bucketed synthesis. The Workflow tool is the standard delivery shape. Spec 052 caught a
  verified BLOCKER (theme-restore race) the green gate could not see — a `Completer`-gated fake
  was structurally required to expose it. Review 01 found 5 BLOCKERs on a feature-complete
  branch. **The pattern works; reach for it before the user does.**
- **Verify findings before acting on them.** A reviewer's claim is a hypothesis until the cited
  lines actually prove it. Read the code. Reports 052 and 057 both record near-misses where
  spot-checking the claim against the file caught either a false positive or a misstated
  severity. (See [`memory/verify-before-reporting`](../).)

## Scope & changes

- **Flag, don't silently fix.** Out-of-scope problems and stale docs get flagged in the report,
  never bundled into a scoped commit. **Bad documentation is worse than no documentation** — but
  fixing it is its own (small) change, not a rider on unrelated work.
- **Never bundle out-of-scope changes** into a scoped commit.
- **Leave files intact.** Delete or rewrite only with explicit direction, or when something is
  clearly marked throwaway. Don't delete what you didn't create.
- **Stash, don't discard, someone else's uncommitted work** when switching branches.
- **Specs are hypotheses about existing behavior.** Trace the code; if the spec's premise is
  wrong, raise it before implementing.

## Citation discipline — for documentation / reverse-engineering projects

*Applies to **citation-bearing deliverables**: a project whose claims rest on a read-only subject under
`reference/`. (Build projects can skip this section.)* When the
[charter](templates/product-charter.md) north star is a synthesis/requirements doc, document *now* so
synthesis is cheap later — thread these through every deliverable:

- **Tag intent provenance — and cite the line even when inferred.** When you state a feature's
  *intended use* (the why), mark it **sourced** (cite `path:line` — a usage-string/comment/readme line
  states it), **inferred** (you read it off naming/UI/structure — **still cite the `path:line` the
  inference rests on** and label it inferred; an inferred claim is *never* citation-free), or
  **needs-SME** (the why isn't recoverable from the code — cite what made you look, then flag it).
  **The one guiding rule: no claim about behavior inferred from the code may live without a citation to
  the line(s) it rests on.** Never present inferred intent as fact; for safety/precision-critical
  features, inferred is **not** acceptable — flag it for owner/SME validation.
- **Cite the load-bearing line, don't flood.** One representative `path:line` — the definition, or the
  decisive use — backs a claim. Do **not** enumerate every appearance of a variable/symbol; accuracy is
  *per-claim citation*, not exhaustive occurrence-listing. A doc drowned in citations is as hard to
  trust as one with none.
- **For inventory/catalog pages, self-audit citation completeness — don't wait for review to sample.**
  A page of many compound rows (each naming several entities) hides *systematic* under-citation that
  sampling can't prove gone — on the repo-map, review spot-checks kept finding more (7 of 30, then 3
  of 8) and never converged. Before first review, **script the audit**: extract every named code
  entity from every cell and prove each is cited, iterating to zero. Classify each token as code
  entity (needs a `path:line`) vs. data/content (a directory citation is fine). *(Lesson from the
  repo-map overview, specs 005→011.)*
- **Cite the member's own line, and make the audit verify the line *contains* it.** A claim about an
  enum *member*, a `[DllImport]`, a `<WrapperTool>`, or a struct field cites **that member's
  definition line** — not its container's (the `enum`/`class` declaration, the `<COMReference>`). A
  name-presence self-audit is not enough: it passes when the citation merely names the enclosing
  file/type, so the audit must confirm the **cited line actually contains the named token**. *(014: an
  architecture page self-reported "0 uncited" yet 12 entities were cited to their container's line —
  e.g. enum members cited to the `enum` declaration, `dbghelp.dll` to the `class` line.)*
- **Dump every cited line and read it — don't trust a "clever" self-audit script.** Hand-rolled
  audit scripts kept reporting `uncited=0` while missing real wrong-line/enumeration mistakes the
  cross-model review then caught (026, 033, 036) — the bug is always in the script's extraction/matching,
  not in the eyeballing. Run the shared dump helper **`python3 tools/cite_audit.py <page.md>`**, which
  prints each citation beside the actual source line it points at, and read each `SRC:` line against its
  claim. (Advisory, not the gate — but it removes the error-prone step.)
- **An enumeration claim cites every member it names.** A claim that lists several members (fault
  codes, thread handles, a file set, enum values) cites **each member's line** — or states the explicit
  lines/range (e.g. `…cs:22, :24, :26`). Citing only the first member's line under-cites the rest, and a
  self-audit that accepts the claim when *any one* member matches will pass it falsely. Treat **each
  enumerated member as its own named entity**. *(033: a safety fault-decode claim listing six faults
  cited only the line of the first — `Temp_Fault` at `:22`, while air-pressure/bus-voltage/… are on
  `:24`–`:31`.)*
- **Match the claim *type* to what the line proves.** "Assembly X", "namespace X", "project `X.csproj`",
  and "class X" are **different source facts** about a component — they do not interchange. An
  "assembly X" claim must cite a line containing `<AssemblyName>X`; a class claim, the class
  declaration; a project-file claim, the `.csproj` path. The line-contains-token audit must respect the
  *kind* of token, not just the string. *(016: a driver's project file is `ClsLaserMarker.csproj` but
  its `<AssemblyName>` is `ClsPMDILaserMarker` — calling `ClsLaserMarker` "the assembly" was wrong.)*
- **Separate the *what* from the *how* where you can.** When a behavior looks like a hardware/era artifact
  rather than a real requirement — a specific tick rate, a platform-specific IPC mechanism, a fixed buffer size —
  note it (*"implementation artifact (platform / era hardware) — likely not a replacement requirement"*) vs.
  behavior that is clearly essential (precision, safety interlocks, the control law's intent). You needn't
  resolve it; flagging it seeds the requirements synthesis.
- **Mark black-box behaviors as requirement-gaps.** When real behavior lives in a prebuilt binary not in the
  import (the PMDi motion servers), say so, capture what the *interface* implies, **do not invent internals**,
  and flag it "behavior to recover" — it is a requirement a replacement must meet.
- **Capture non-functional requirements as you meet them.** Real-time/timing guarantees, safety interlocks,
  precision/thermal, failure modes (EStop, amplifier-disable, watchdog/warning daemons) — note them where you
  find them, tagged, so the dedicated NFR pass collects rather than rediscovers them.

## Git

- **Pull `main` at the start of every work unit.**
- **`git fetch --prune` before any branch enumeration or sync** (stale remote refs mislead).
- **Audit the current `main`, never a stale checkout** — and record the audited SHA.
- **Never report green tests on a broken build.** Run the build gate too.
- **Run gate commands separately, never chained** — a chain hides which step failed.
- **No force-push.** Renumber with `git mv`, never history rewrites.

## Spec numbering

The Manager issues a new spec number at `(highest existing) + 1`. **Search `wiki/specs/` for the
highest number before drafting** — the committed wiki, with its git history, is the single source
of truth, so two sessions reading it converge on the same answer rather than colliding. Numbers
are never reused, even for withdrawn specs.

> Historical note: an earlier model used a *random gap* (`+ random(1..5)`) to keep
> memory-compacted Manager instances from colliding on "highest + 1". The wiki doesn't have that
> failure mode — the durable, committed record is authoritative over any session's memory — so
> plain monotonic +1 is correct here.

## Communication

- **The owner is always the relay.** Manager and developers never cue each other directly.
- **Q&A is for memorialized nuance**, not routine clarifications.
- **Concise, peer-to-peer, honest.** No sycophancy, no theatrical apology, no celebratory
  padding. Push back with reasoning. Own mistakes cleanly. Don't perform a persona.
- **Roles update notice.** When you edit a role doc, announce it for relay.

## Independence (no self-approval)

The Manager and the three developers — including Claude Dev, which runs on its **own machine** —
are separate surfaces, so no agent authors, executes, and approves the same code. **Always:**
spec-and-prompt before code; code merges reviewed by an independent integrator at onboarding. The
author and the integrator must be different surfaces. Doc-only ships (reports, Q&A, wiki docs) go
straight to `main` — the independence rule is about code, not docs.

---

## Changelog
- 2026-06-13 — Promoted the 033/036 lessons: **an enumeration claim cites every member it names** (not
  just the first member's line), and **dump-and-read every cited line via `tools/cite_audit.py` rather
  than trusting a clever self-audit script** (hand-rolled scripts kept false-reporting `uncited=0`).
- 2026-06-13 — Promoted the 016 lesson: **match the claim type to what the cited line proves** —
  assembly (`<AssemblyName>`) / namespace / project filename / class are different source facts and
  don't interchange (a driver's `.csproj` was `ClsLaserMarker.csproj` but its assembly was
  `ClsPMDILaserMarker`).
- 2026-06-13 — Promoted the 014 lesson: **cite the member's own line and make the self-audit verify
  the cited line *contains* the named token** (a name-presence audit passes on container-line cites —
  an architecture page self-reported 0 uncited yet had 12 members cited to their enum/class line).
- 2026-06-13 — Promoted CC1's cross-cutting lesson (specs 005→011): **inventory/catalog pages get a
  scripted citation-completeness self-audit before review** — sampling can't prove systematic
  under-citation gone (repo-map: 7/30 then 3/8 never converged); extract every named code entity and
  prove each cited, iterating to zero.
- 2026-06-13 — Owner's **one guiding rule** made explicit in *Documenting toward the requirements doc*:
  **every claim inferred from the code carries a per-line citation** (inferred ≠ citation-free — cite the
  `path:line` the inference rests on, labeled inferred), while citing the **one load-bearing line, not
  every occurrence** of a symbol (don't flood the doc).
- 2026-06-05 — Added **Documenting toward the requirements doc**: the end goal is a replacement requirements
  doc (charter north star), so thread an intent-provenance tag (sourced/inferred/needs-SME), an
  essential-vs-incidental (*what* vs *how*) flag, black-box-as-requirement-gap marking, and inline NFR/safety
  capture through every deliverable — cheap now, expensive to retrofit.
- 2026-06-02 — Added **Pre-merge review (when the gate isn't enough)** section codifying the
  adversarial-workflow pattern proven in Spec 052 (caught a verified theme-restore BLOCKER behind
  a green gate) and Review 01 (5 BLOCKERs on a feature-complete branch). Pattern: parallel
  reviewers + per-finding adversarial verification + verify-claims-against-the-code before
  acting. A green gate is necessary but not sufficient for cross-model merges of risky surface
  area.
- 2026-05-29 — Retired the "two-hat" framing: `CC` runs on its own machine, so Manager↔developer
  independence is structural, not a same-instance discipline. Section renamed to *Independence*.
- 2026-05-28 — Initial.
