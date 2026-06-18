# Prompt — Spec 004-CX — Integrate CC's spec-001 vertical slice

You are **CX**, the cross-model integrator. Execute Spec 004-CX
(`wiki/specs/004-CX-integrate-vertical-slice.md`). **Read the spec first.** You are reviewing CC's work,
not co-authoring it — if you find a problem, you HOLD and document it; you do not fix CC's code.

## Setup
- In your worktree (`worktrees/CX`): `git fetch --prune origin`.
- You **cannot** check out `feature/cc-vertical-slice` (CC's worktree holds it). Branch from origin:
  `git checkout -b feature/cx-integrate-001 origin/feature/cc-vertical-slice`.
- `export PATH="$HOME/.dotnet:$PATH"` (the .NET 8 SDK CC installed is shared). Confirm `dotnet --version`.

## Steps
1. **Re-run the gate, each command separately**, capturing real output + counts:
   - `dotnet build -c Release`
   - `dotnet format --verify-no-changes`
   - `dotnet test`
2. **Independently confirm a test can fail:** invert one assertion, run `dotnet test`, observe the
   failure count, then revert and re-run green. Report what you inverted.
3. **Run the app:** `dotnet run --project src/AmetekWatch.App` — capture the printed digest.
4. **Correctness review** per spec Decision 3 — read the Core/App/test sources and verify each item,
   citing `file:line`. Be adversarial: a green gate is necessary, not sufficient.
5. **Write the review** → `wiki/reviews/review-spec004-CX-vertical-slice.md` (structure below). End the
   file with a line that is exactly `VERDICT: PASS` or `VERDICT: HOLD`.
6. Commit the review on `feature/cx-integrate-001` (message `004-CX integration review`) and push to
   origin. **Do not merge to main** — CM lands on PASS.

## The gate
The three commands in Step 1, run separately (see `wiki/contracts/git-and-gates.md`). You are *verifying*
CC's gate claim, not producing a new deliverable.

## Versioning ritual
N/A — integration review produces no version bump and no `CHANGELOG`. (On PASS, CM's landing of the slice
carries CC's already-written `CLAUDE.md` Status entry; you add nothing version-related.)

## Report-back format
The review file `wiki/reviews/review-spec004-CX-vertical-slice.md` must contain:
- **Headline:** PASS or HOLD, one line.
- **Branch state:** `feature/cx-integrate-001` tip SHA; the reviewed `origin/feature/cc-vertical-slice` SHA.
- **Gate results:** table of the three commands → ✓/✗ with real counts; the can-fail check (what you
  inverted, the failure observed); the SHA the gate ran clean at; `dotnet --version`.
- **App run:** the captured digest stdout.
- **Correctness review:** each spec-Decision-3 item → ✓/finding, with `file:line` citations.
- **Findings / HOLD blockers:** concrete, `file:line`, why it matters. "None." if clean.
- **VERDICT: PASS** or **VERDICT: HOLD** (exact, final line).

End your final message to me with: the `feature/cx-integrate-001` tip SHA and the verdict (PASS/HOLD).
