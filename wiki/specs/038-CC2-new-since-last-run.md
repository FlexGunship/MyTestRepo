# Spec 038-CC2 — "New since last run" digest (don't re-report known findings)

## Status
- Doc type: implementation (avoid re-notifying findings already seen in prior runs)
- Executes: **CC2**; pushes `feature/cc2-new-since-last-run`; **CX** integrates (cross-model); CM lands on PASS. No self-merge.
- Number 038 verified free (search `wiki/specs/`; this is highest + 1). **Independent of 036** (Core, not App).
- Paired prompt: prompt-spec038-CC2-new-since-last-run.md
- Final on-disk: `src/AmetekWatch.Core/Pipeline/SweepRunner.cs` + a test in `tests/AmetekWatch.Tests/`.

## Background
The store upserts by `Url`, so a finding re-found on a later sweep updates the same row — and today's digest
(all worth-reporting findings of **this** sweep) would **re-report** it every run. For a periodic daemon that
emails/writes a digest, that's noisy. This adds an opt-in **"only new" digest**: report only worth-reporting
findings whose `Url` was **not already in the store before this sweep**. Backward-compatible (default = current).

## Decisions made
1. **`SweepRunner` (backward-compatible):** add **one optional** ctor param `bool digestOnlyNew = false`
   (default `false` = current behaviour — all worth-reporting in the digest; keep existing tests green).
   Keep the 034 params; existing construction must still compile.
2. **Newness computation:** at the **start** of `RunAsync` (before persisting), snapshot the set of `Url`s
   already in the store via the existing `IFindingStore.GetAllAsync()` (**no interface change**). A triaged
   finding is **new** iff its `Url` is **not** in that snapshot. **Persist all** triaged findings as today
   (upsert). When `digestOnlyNew == true`, the returned digest = worth-reporting **AND new**; when `false`,
   the digest is worth-reporting (unchanged). Ordering (most-recent `DiscoveredAt` first) unchanged.
3. **No App wiring here** (a later tiny App spec adds `Sweep:OnlyReportNew` config + passes the flag). Don't
   change the App/Anthropic projects, the `IFindingStore` interface, or the `.sln`.
4. **Tests** (`tests/AmetekWatch.Tests/`): pre-seed the store (via the in-memory store or a fake) with one of
   the worth-reporting `Url`s, run a sweep with `digestOnlyNew=true` → that known `Url` is **excluded** from
   the digest (but still persisted), while genuinely-new worth-reporting findings are **included**; the same
   sweep with `digestOnlyNew=false` → all worth-reporting in the digest (current). Confirm existing
   `SweepRunner`/end-to-end tests still pass (default false). Hand-computed; confirm a test can fail then revert.

## Out of scope
- App config wiring (`Sweep:OnlyReportNew`) — later. The live API. Changing the store interface. Time-window
  "new" (this is store-membership-based, which is the right primitive).

## Definition of done
- [ ] `SweepRunner` optional `digestOnlyNew` (default false, backward-compatible); newness via pre-sweep `GetAllAsync` snapshot; persist-all preserved.
- [ ] Tests (only-new excludes known + includes new; default unchanged); existing tests green; can-fail confirmed.
- [ ] Gate green (build/format/test, each separately, real counts). Branch pushed; tip SHA reported.

## Deliverable / report-back
See the prompt file.
