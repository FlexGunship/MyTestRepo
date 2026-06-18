# Spec 043-CC — Fix 042 HOLD on 041 (bless SweepHost runner seam + add 401 test)

## Status
- Doc type: bugfix / HOLD-fix (continues 041; **supersedes 041's "no SweepHost seam change" constraint**).
- Executes: **CC**; pushes `feature/cc-resilience-holdfix`; **CX2** integrates (cross-model, a *different* Codex than 042's CX); CM lands on PASS. No self-merge.
- Number 043 verified free (search `wiki/specs/`; this is highest + 1).
- Paired prompt: prompt-spec043-CC-resilience-holdfix.md
- Builds on CC's `feature/cc-activate-resilience` (origin tip `cbe7170`) — the 041 work CX **HELD** in review 042.

## Background
Integration **042 HELD** spec 041 ([`wiki/reviews/review-spec042-CX-activate-resilience.md`](../reviews/review-spec042-CX-activate-resilience.md))
with two blockers. The work is otherwise correct (gate green, predicate correct, wiring correct). This
spec resolves both:

1. **Blocker 1 — `SweepHost` seam change.** 041 added an optional `SweepRunner? runner = null` ctor param
   to `SweepHost` so the App can inject the retry/`OnlyReportNew`-configured runner; `Program` passes
   `c.Runner` into it. 041's prompt said "no `SweepHost` seam change" — **that constraint was a spec defect**:
   injecting a pre-built `SweepRunner` necessarily requires a way in, and an **optional, backward-compatible**
   `SweepRunner? runner = null` param (null → `SweepHost` builds one as before) is the correct, minimal design.
   **This spec blesses that seam** — keep it as-is. The constraint in 041 is hereby corrected.
2. **Blocker 2 — missing 401 test.** Add an explicit **401 Unauthorized** case to `AnthropicTransientTests`
   asserting `IsTransient(<401>) == false` (the predicate already returns false by logic — this locks it).

## Decisions made
1. **Keep** the optional `SweepRunner? runner = null` seam on `SweepHost` (backward-compatible; existing
   3-arg/no-runner construction still works). Do not revert it. (Optionally add a one-line XML-doc noting it's
   the App-side composed-runner injection point.)
2. **Add** the 401-unauthorized no-retry test (and, if cheap, 403 for symmetry) to
   `tests/AmetekWatch.Anthropic.Tests/AnthropicTransientTests.cs`.
3. No other behaviour change. Don't touch the notifier impls, the `SweepRunner` seam, or the `.sln`.

## Scope — what to do
- Branch from CC's HELD branch (not main): `git checkout -b feature/cc-resilience-holdfix origin/feature/cc-activate-resilience`.
- Add the 401 (+403) `AnthropicTransient` test(s); add the optional XML-doc on the `SweepHost` runner param.
- Gate green; the existing 116 tests + the new one(s) pass.

## Out of scope
- Reverting the seam change. Live API/SMTP. Any new feature.

## Definition of done
- [ ] 401 (and 403) no-retry test(s) added and green; `SweepHost` runner seam kept (blessed) with a doc note.
- [ ] Gate green (build/format/test, each separately, real counts). Branch pushed; tip SHA reported.

## Deliverable / report-back
See the prompt file.
