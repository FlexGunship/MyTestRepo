# Report — Spec 038-CC2: "New since last run" digest

**Headline outcome:** Built — **not merged** (no self-merge; CX integrates). `SweepRunner` gains one
optional backward-compatible ctor param `bool digestOnlyNew = false`; when set, the digest reports only
worth-reporting findings whose `Url` was absent from the store before the sweep. Persist-all unchanged,
default behaviour unchanged. No version bump (`<Version>` stays `0.1.0`). Branch
`feature/cc2-new-since-last-run` pushed; gate green (build 0 warn / format clean / 97 tests pass).

## 1. Branch / merge state
- Pre-merge `main` SHA: `3165910d1a01553aba9c7e3d6a973487e3175a2f` (origin/main, has 034's `SweepRunner`).
- Feature branch: `feature/cc2-new-since-last-run`, branched from `origin/main`.
- Working commit(s): `a6b888e18447f809f9b58f7571947429c4dded98` (code + test + CLAUDE.md), plus a docs
  commit adding this report (tip reported at end of run).
- Branch deleted post-merge: n/a (not merged).
- Merge mechanic: pushed branch; **cross-model integrator (CX) merges** on PASS. Author ≠ integrator.

## 2. Changes
| File | Change |
| --- | --- |
| `src/AmetekWatch.Core/Pipeline/SweepRunner.cs` | Added optional ctor param `bool digestOnlyNew = false` (after the 034 `retryPolicy`/`onTriageError` params) + `_digestOnlyNew` field; `RunAsync` snapshots store `Url`s via `GetAllAsync()` at the start when the flag is set, and adds a `!_digestOnlyNew \|\| !knownUrls.Contains(Url)` predicate to the digest filter. XML-doc updated. |
| `tests/AmetekWatch.Tests/SweepRunnerOnlyNewTests.cs` | New file: 3 hand-computed tests for the only-new digest. |
| `CLAUDE.md` | Appended a dated `### Unreleased` entry below the existing ones. |
| `wiki/reports/report-spec038-CC2-new-since-last-run.md` | This report. |

## 3. Spec-specific: design & behaviour
- **Backward-compatible ctor:** `digestOnlyNew` is the **6th** param, default `false`, after the 034
  `retryPolicy`/`onTriageError` optionals. Existing 3-arg (and 4/5-arg) construction compiles and behaves
  identically — confirmed by the unchanged `SweepRunnerTests` (7) and `SweepRunnerResilienceTests` passing.
- **Newness primitive:** at the **start** of `RunAsync`, *before* any `SaveAsync`, the runner snapshots the
  set of `Url`s already in the store via `IFindingStore.GetAllAsync(ct)` (**no interface change**). A
  triaged finding is *new* iff its `Url` is not in that snapshot. The snapshot is only taken when
  `digestOnlyNew == true` (no extra store read in the default path).
- **Persist-all preserved:** every triaged survivor is still upserted (`SweepRunner.cs:88`); only the
  *returned digest* is filtered.
- **Digest filter:** `Where(WorthReporting).Where(!onlyNew || isNew).OrderByDescending(DiscoveredAt)`.
  Ordering (most-recent first) unchanged.

### Hand-computed oracle (from `FakeSearcher.Canned`)
Worth-reporting subset, desc by `DiscoveredAt`: `url-b`(10:00), `url-a`(09:00), `url-d`(08:00). `url-c`
(Other) is not worth-reporting. Pre-seeding the store with `url-b`:
- `digestOnlyNew=true` → digest = `[url-a, url-d]` (known `url-b` excluded; `url-b`/`url-c` still persisted).
- `digestOnlyNew=false` → digest = `[url-b, url-a, url-d]` (unchanged).
- empty store + `digestOnlyNew=true` → `[url-b, url-a, url-d]` (everything is new).

### Existing tests still pass with the default
`SweepRunnerTests` (7) and `SweepRunnerResilienceTests` construct `SweepRunner` without `digestOnlyNew`
(default `false`) and all pass unchanged — confirming the new flag is inert by default.

## Gate results
Run on Linux, `dotnet --version` = **8.0.422**, each command separately, clean tree at
`a6b888e18447f809f9b58f7571947429c4dded98`.

| Gate command | Result | Counts |
| --- | --- | --- |
| `dotnet build -c Release` | ✓ | 0 warnings, 0 errors |
| `dotnet format --verify-no-changes` | ✓ | clean (no output) |
| `dotnet test` | ✓ | **97/97 pass** (was 94) |

- Test count **before**: 94 (AmetekWatch.Tests 58 + Storage 4 + Anthropic 30 + Web 2).
- Test count **after**: 97 (AmetekWatch.Tests **61** + Storage 4 + Anthropic 30 + Web 2) — +3 new.
- Can-fail: flipped the only-new digest oracle to expect `[url-b, url-a, url-d]` → 1 failure
  ("Assert.Equal() Failure: Collections differ"); reverted; suite green again.
- Files changed NOT in the spec's files-to-change list: none beyond `CLAUDE.md` (versioning ritual) and
  this report (report-back ritual), both expected.

## Sources beyond the brief / surprises
- **`InMemoryFindingStore` appends, it does not upsert by `Url`.** So pre-seeding `url-b` and then sweeping
  leaves *two* `url-b` rows in the in-memory store (the real `SqliteFindingStore` upserts). This does not
  affect the spec's semantics — newness is computed from the **pre-sweep** snapshot, and the "still
  persisted" assertion uses `Assert.Contains`, not an exact count. Noted so a future App-wiring spec
  doesn't assume in-memory dedupe.

## Deferred / not done
- **App config wiring (`Sweep:OnlyReportNew`)** — explicitly out of scope per the spec; a later tiny App
  spec adds the config key and passes the flag.
- **Live API path** — unchanged; still deferred to the key-present wiring.

## Standing flags
- None new. (Pre-existing standing items — live `ANTHROPIC_API_KEY` verification, the `web_search`
  server-tool continuation loop — are untouched by this Core-only change.)

## Roles update notice
None — no role doc edited this session.
