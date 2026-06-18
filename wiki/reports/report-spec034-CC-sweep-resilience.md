# Report — Spec 034-CC: Sweep resilience — per-finding triage isolation + retry helper

**Headline outcome:** Built on `feature/cc-sweep-resilience`; **NOT merged** (no self-merge — CX2
integrates). New `Core/Resilience/` (`IRetryPolicy`/`RetryPolicy`/`NoRetryPolicy`) plus a
backward-compatible `SweepRunner` (optional retry policy + per-finding triage isolation). Gate green,
each command run separately, real counts: build 0 warn / 0 err, format clean, test **94/94** (was 89,
+5). No version bump (`<Version>` stays `0.1.0`). Existing `SweepRunner`/end-to-end tests pass
unchanged under the defaults. No App/Anthropic/`.sln` changes.

## 1. Branch / merge state
- Pre-branch `origin/main` SHA: `bf83db4` ("Author spec 034-CC …") — branched from it.
- Feature branch: `feature/cc-sweep-resilience`; working commit: see tip SHA in the closing summary;
  branch deleted post-merge: n/a (not merged).
- Post-merge `main` SHA (pushed): n/a — not self-merged; pushed feature branch for CX2 to integrate.
- Merge mechanic: pushed branch; cross-model integrator (CX2) merges on PASS.

## 2. Changes
| File | Description |
| --- | --- |
| `src/AmetekWatch.Core/Resilience/IRetryPolicy.cs` | New seam: `Task<T> ExecuteAsync<T>(Func<CancellationToken,Task<T>> op, CancellationToken ct)`. |
| `src/AmetekWatch.Core/Resilience/RetryPolicy.cs` | New: exponential-backoff retry; ctor `maxAttempts`, base delay, `Func<Exception,bool> shouldRetry`, **injected** `Func<TimeSpan,CancellationToken,Task>` delay (default `Task.Delay`). Retries only when an attempt remains AND `shouldRetry(ex)` (via `catch … when` filter); rethrows the last exception after `maxAttempts`. |
| `src/AmetekWatch.Core/Resilience/NoRetryPolicy.cs` | New: single-attempt passthrough — the `SweepRunner` default. |
| `src/AmetekWatch.Core/Pipeline/SweepRunner.cs` | Backward-compatible: two new **optional** ctor params (`IRetryPolicy? retryPolicy = null` → `NoRetryPolicy`; `Action<Finding,Exception>? onTriageError = null` → no-op). Searcher call wrapped in `retryPolicy.ExecuteAsync` (propagates on final failure). Per-finding triage isolation: each `JudgeAsync` runs under the policy in try/catch — on throw, `onTriageError(finding, ex)` + skip (no persist, no abort), continue. Persist + digest successes only. |
| `tests/AmetekWatch.Tests/RetryPolicyTests.cs` | New: 4 tests (transient-then-success; give-up-after-maxAttempts-rethrow; non-retryable not retried; `NoRetryPolicy` once-and-propagate) — **no-op injected delay**. |
| `tests/AmetekWatch.Tests/SweepRunnerResilienceTests.cs` | New: 1 test — decider throws on one of three findings → that finding absent from persisted + digest, the other two persisted/digested most-recent-first, callback fired once for it, sweep returns normally. |
| `CLAUDE.md` | Appended a dated `### Unreleased` entry (below existing; existing untouched). |
| `wiki/reports/report-spec034-CC-sweep-resilience.md` | This report. |

## 3. Backward-compatibility evidence
- `SweepRunner`'s original 3-arg ctor signature is preserved; the two new params are optional with
  defaults (`NoRetryPolicy`, no-op callback) that reproduce the prior behaviour exactly.
- `tests/AmetekWatch.Tests/SweepRunnerTests.cs` (the 7 original SweepRunner tests using 3-arg
  construction) and `SweepHostTests.cs` / `PipelineToggleAndDigestTests.cs` (end-to-end) pass
  **unchanged** — they were not edited. Confirmed in the full-suite run (58/58 in AmetekWatch.Tests).

## 4. Design notes / decisions
- **Retry predicate via `catch … when`:** `RetryPolicy` uses an exception filter
  (`catch (Exception ex) when (attempt < _maxAttempts && _shouldRetry(ex))`) so a non-retryable or
  final-attempt exception is never caught — it propagates with its original stack, no rethrow plumbing.
- **Backoff formula:** wait before retry `n` (1-based) is `baseDelay * 2^(n-1)`, computed in ticks.
  Not exercised for real time in tests (no-op delay injected) — the formula is documented, not asserted,
  since the spec's tests target attempt-count/outcome behaviour, not wall-clock timing.
- **Searcher retry propagates:** per spec Decision 2, a searcher that still fails after the policy gives
  up propagates out of `RunAsync` (a sweep can't proceed without results) — caller's concern.
- **Guards:** `RetryPolicy` validates `maxAttempts >= 1` and non-null `shouldRetry`/`op`; `NoRetryPolicy`
  null-guards `op`. Matches existing Core null-guard discipline.

## Gate results
| Command (run separately) | Result | Counts |
| --- | --- | --- |
| `PATH="$HOME/.dotnet:$PATH" dotnet build -c Release` | ✓ | 0 warning, 0 error |
| `PATH="$HOME/.dotnet:$PATH" dotnet format --verify-no-changes` | ✓ | clean (exit 0) |
| `PATH="$HOME/.dotnet:$PATH" dotnet test` | ✓ | **94 passed** / 0 failed / 0 skipped |

- Test count **before**: 89 (AmetekWatch.Tests 53 + Storage 4 + Anthropic 30 + Web 2).
- Test count **after**: 94 (AmetekWatch.Tests **58** + Storage 4 + Anthropic 30 + Web 2) — **+5**
  (4 `RetryPolicyTests` + 1 `SweepRunnerResilienceTests`).
- Can-fail confirmed: temporarily flipped the callback-count oracle in
  `SweepRunnerResilienceTests` to `Assert.Equal(2, errors.Count)` → 1 failure (Expected 2, Actual 1),
  then reverted. Final suite green.
- `dotnet --version`: **8.0.422** (Linux).
- Clean SHA at which the gate ran: the feature-branch tip (closing summary).
- Files changed NOT in the spec's files-to-change list: `CLAUDE.md` (required Unreleased entry) and
  this report — both expected by the prompt's versioning/report-back steps. No App/Anthropic/`.sln` edits.

## Sources beyond the brief / surprises
None. The 034 spec + prompt were present on `origin/main` (authoring commit `bf83db4`) and matched the
dispatch instructions exactly.

## Deferred / not done
- Live API behaviour (real Anthropic transient errors / `529`) — not exercised; auth still deferred.
- Wiring `RetryPolicy` (with an Anthropic-transient `shouldRetry` predicate) and `onTriageError` logging
  into the real App pipeline — explicitly out of scope per spec Decision 3 (a later small App spec).
- Real-time backoff verification — intentionally not asserted (no-op delay injected per the spec).

## Standing flags
None new. The capstone (028) closed the end-to-end loop and remains flagged to the Manager as a
candidate first user-facing milestone (0.1.0 → 1.0.0 + `CHANGELOG`); unaffected by this spec.

## Roles update notice
None — no role doc edited this session.
