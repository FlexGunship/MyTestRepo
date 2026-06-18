# Spec 004-CX — Integrate CC's spec-001 vertical slice

## Status
- Doc type: integration (cross-model gate + review of a peer's deliverable branch)
- Executes: **CX**; merge: **CX issues VERDICT; CM lands on PASS** (`--no-ff` from repo-master). CX does
  **not** merge to main.
- Number 004 verified free (highest spec file = 003-GB-onboarding; 004 = 003 + 1). *(002 is a gap from
  the onboarding-report naming — see the passdown.)*
- Paired prompt: prompt-spec004-CX-integrate-vertical-slice.md
- Final on-disk locations after merge: `wiki/reviews/review-spec004-CX-vertical-slice.md`; on PASS, CC's
  slice (`AmetekWatch.sln`, `src/…`, `tests/…`, `Directory.Build.props`, `.gitignore`, the `CLAUDE.md`
  Status entry, CC's report) lands to `main`.

## Background
CC (Claude) authored and built the spec-001 vertical slice on `feature/cc-vertical-slice` (tip
`e9a3b40`): the .NET solution scaffold, pipeline seams, `SweepRunner`, fakes, in-memory store, console
host, and an xUnit suite. CC self-reported the gate green (build 0/0, format clean, 7/7 tests). Per the
git & gates contract, a deliverable reaches `main` only through an **independent cross-model integrator**
that re-runs the gate and reviews correctness. CX (Codex, a different model) is that integrator.

## Decisions made
1. **Branch (worktree-aware):** CX cannot check out `feature/cc-vertical-slice` (it's held by CC's
   worktree). From the CX worktree: `git fetch --prune origin`, then
   `git checkout -b feature/cx-integrate-001 origin/feature/cc-vertical-slice`.
2. **Re-run the full gate, each command SEPARATELY**, using the shared SDK on PATH
   (`export PATH="$HOME/.dotnet:$PATH"`): `dotnet build -c Release`; `dotnet format --verify-no-changes`;
   `dotnet test`. Record **real** counts. Independently confirm a test can fail (invert one assertion,
   observe the failure, revert) — do not take CC's word for it.
3. **Correctness review** against spec 001 — verify, citing `file:line`:
   - dedupe is by `Url`, first occurrence wins;
   - the digest is exactly the `WorthReporting == true` subset, ordered most-recent `DiscoveredAt` first,
     while **all** triaged findings are persisted;
   - `SweepRunner` orchestration matches the spec (search → dedupe → triage → persist → digest);
   - the fakes are deterministic and cover the spec's cases (duplicate URL, each `FindingCategory`);
   - **Core has no Anthropic-SDK / network dependency** (auth deferred);
   - `Directory.Build.props` sets `Nullable=enable`, `TreatWarningsAsErrors=true`, and `Version=0.1.0` as
     the single version source; `.gitignore` covers `bin/ obj/ dist/`; `dist/` is **not** tracked.
4. **Run the app:** `dotnet run --project src/AmetekWatch.App` prints a digest; capture stdout.
5. **Verdict:** write `wiki/reviews/review-spec004-CX-vertical-slice.md` ending with a line exactly
   `VERDICT: PASS` or `VERDICT: HOLD`. On HOLD, enumerate each blocker concretely (`file:line` + why) so
   CM can route a fix spec to CC. A green gate is necessary but not sufficient — flag any correctness gap.
6. Commit the review on `feature/cx-integrate-001` and push. **Do not merge to main.**

CX implements as stated unless it finds a strong reason to deviate — flag in the review rather than
silently choosing otherwise.

## Out of scope
- Editing CC's product code (if a fix is needed, that's a new CC spec — do not "fix it while you're in
  there"; the integrator reviews, it does not co-author).
- Merging to `main` (CM lands on PASS).
- Anything beyond the 001 slice's scope (SQLite, dashboard, real API — later specs).

## Working model
(Detail in the prompt file — prompt-spec004-CX-integrate-vertical-slice.md.)

## Definition of done
- [ ] `feature/cx-integrate-001` created from `origin/feature/cc-vertical-slice`.
- [ ] Full gate re-run separately, real counts recorded; can-fail independently confirmed.
- [ ] Correctness review completed with `file:line` citations.
- [ ] `wiki/reviews/review-spec004-CX-vertical-slice.md` written, ending `VERDICT: PASS` or `VERDICT: HOLD`.
- [ ] Branch pushed; tip SHA reported. (CM lands on PASS.)

## Deliverable / report-back
See the prompt file for the report-back format.
