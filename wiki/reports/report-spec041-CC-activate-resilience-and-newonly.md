# Report — Spec 041-CC: Activate retry + new-only in the live pipeline

**Headline outcome:** Implemented and **pushed** on `feature/cc-activate-resilience`; **not merged**
(CX integrates cross-model, author ≠ integrator). 034's `RetryPolicy` + per-finding triage isolation
and 038's opt-in new-only digest are now turned on **by config** in the running app. No version bump
(`<Version>` stays `0.1.0`, internal). Gate green on Linux .NET 8.0.422: build 0 warn / format clean
/ **test 116/116** (was 99). No live API/SMTP call, no secret printed or committed.

## 1. Branch / merge state
- Pre-merge `main` SHA: `bc5884f62e03bb9dcf38754fa54669b02d36e421` (= `origin/main`)
- Feature branch: `feature/cc-activate-resilience` (branched from `origin/main`)
- Working commit(s): `7683cfb52bc7e9843adde6f63e71dcb20d80ae58` (implementation + tests + CLAUDE.md);
  this report is a follow-up commit on the same branch.
- Post-merge `main` SHA (pushed): N/A — not self-merged (CX integrates).
- Merge mechanic: pushed branch; **cross-model integrator merges** (no self-merge).

## 2. Changes
| File | Change |
|------|--------|
| `src/AmetekWatch.Anthropic/AnthropicTransient.cs` | **New.** Pure `static bool IsTransient(Exception?)` — the retry `shouldRetry` predicate over the SDK `Anthropic.Exceptions` hierarchy. |
| `src/AmetekWatch.App/PipelineOptions.cs` | Added `RetryOptions Retry { get; init; } = new()` and a new `RetryOptions` record (`MaxAttempts=3`, `BaseDelayMs=500`). |
| `src/AmetekWatch.App/SweepOptions.cs` | Added `bool OnlyReportNew { get; init; }` (default `false`). |
| `src/AmetekWatch.App/appsettings.json` | Added `Sweep:OnlyReportNew=false` and `Pipeline:Retry:{MaxAttempts:3, BaseDelayMs:500}`. |
| `src/AmetekWatch.App/SweepComposer.cs` | Selects `RetryPolicy` (real tier) / `NoRetryPolicy` (fake); builds `SweepRunner` with that policy, `digestOnlyNew: Sweep.OnlyReportNew`, and an `onTriageError` logging via the warn callback. Exposes `Runner` and the selected `IRetryPolicy RetryPolicy` on `SweepComposition`. |
| `src/AmetekWatch.App/SweepHost.cs` | Added optional 6th ctor param `SweepRunner? runner = null` (defaults to the prior `new SweepRunner(searcher, triage, store)`); `RunOnceAsync` now uses the injected runner. Backward-compatible. |
| `src/AmetekWatch.App/Program.cs` | Passes `c.Runner` to both the one-shot and daemon `SweepHost` constructions. |
| `tests/AmetekWatch.Anthropic.Tests/AnthropicTransientTests.cs` | **New.** 14 predicate oracles. |
| `tests/AmetekWatch.Tests/SweepComposerResilienceTests.cs` | **New.** 3 composer-wiring oracles. |
| `CLAUDE.md` | Appended a dated `### Unreleased` entry (below existing ones). |

## 3. Resolved transient-exception types used
SDK exception types confirmed via the `/claude-api` skill (it directed to the official `Anthropic`
.NET SDK) **plus reflection over `Anthropic.Exceptions` in the installed `Anthropic` 12.29.1 package**
(the skill doc gives the Python/TS names; the C# names differ). Verified hierarchy:

- `AnthropicException : Exception` (base)
  - `AnthropicIOException` — wraps an `HttpRequestException` (network/transport)
  - `AnthropicInvalidDataException` — malformed/unexpected response (parse-time)
  - `AnthropicServiceException`
    - `AnthropicApiException` — carries `HttpStatusCode StatusCode` (a **required** member the SDK sets from the response)
      - `Anthropic4xxException` → `AnthropicBadRequestException` (400), `AnthropicUnauthorizedException` (401), `AnthropicForbiddenException` (403), `AnthropicNotFoundException` (404), `AnthropicUnprocessableEntityException` (422), `AnthropicRateLimitException` (429)
      - `Anthropic5xxException` (5xx server errors, including overloaded 529)
      - `AnthropicUnexpectedStatusCodeException`

**Treated as transient (retry):**
- `AnthropicRateLimitException` (429) — matched by type.
- `Anthropic5xxException` (any 5xx incl. overloaded **529**) — matched by type.
- Any other `AnthropicApiException` with `StatusCode == 429` or `>= 500` (covers `AnthropicUnexpectedStatusCodeException` carrying a 5xx/529; 529 has no named `HttpStatusCode` member but its integer value round-trips through the cast).
- `AnthropicIOException` (SDK transport failure).
- Raw `System.Net.Http.HttpRequestException`, `TaskCanceledException`, `TimeoutException` (network/timeout — an `HttpClient` request timeout surfaces as a `TaskCanceledException`).

**Treated as NOT transient (fail fast):**
- Other 4xx `AnthropicApiException` subclasses (400/401/403/404/422) — request/auth/not-found errors a retry can't fix.
- `AnthropicInvalidDataException` (parse-time), `FormatException` (the parsers' failure mode — `TriageVerdictParser`/`SearchResponseParser`).
- `ArgumentException` and any other non-API exception; `null` → `false`.

Design notes / judgment calls:
- **Conservative by design** — retry only clearly-transient cases; the default branch returns `false`.
- Typed `is AnthropicRateLimitException or Anthropic5xxException` checks come **before** the
  `StatusCode` check so the predicate is robust to how the exception is populated, while the
  `StatusCode` branch still catches the unexpected-status subclass and correctly rejects the other 4xx.
- `TaskCanceledException` is treated as transient per the spec (timeout). A genuine caller
  cancellation still surfaces promptly: `RetryPolicy` awaits its injected delay with the same
  `CancellationToken`, which rethrows immediately on a cancelled token — so cancellation is not
  swallowed even though the predicate would have allowed a retry.

## Gate results
Each command run **separately**; real counts from runner output. `dotnet --version` = **8.0.422**.
Clean SHA: `7683cfb52bc7e9843adde6f63e71dcb20d80ae58` (this report is a later commit; it adds no code).

| Gate command (prefixed `PATH="$HOME/.dotnet:$PATH"`) | Result |
|------|--------|
| `dotnet build -c Release` | ✓ 0 warnings, 0 errors |
| `dotnet format --verify-no-changes` | ✓ exit 0, no changes |
| `dotnet test` | ✓ **116 passed, 0 failed, 0 skipped** |

Per-project test counts (after): AmetekWatch.Tests **66**, AmetekWatch.Anthropic.Tests **44**,
AmetekWatch.Storage.Tests 4, AmetekWatch.Web.Tests 2.

- **Before:** 99 total (AmetekWatch.Tests 63, AmetekWatch.Anthropic.Tests 30, Storage 4, Web 2).
- **After:** 116 total (+17: **14** AnthropicTransient + **3** SweepComposerResilience).
- **Can-fail confirmed:** forced `digestOnlyNew: false` in `SweepComposer` →
  `SweepComposerResilienceTests.OnlyReportNewTrue_SecondRunOverSameStoreReportsNothing` failed
  (`Assert.Empty()` — collection not empty); reverted, suite green again.

**`dotnet run` confirmation** (default config: FAKE tier, `OnlyReportNew=false`):
`PATH="$HOME/.dotnet:$PATH" dotnet run --project src/AmetekWatch.App` printed the FAKE pipeline,
`Digest sink: File -> FileDigestNotifier`, **persisted 4**, **worth-reporting digest 3**, **exit 0**,
and wrote `ametek-watch-digest.md` (3 items; gitignored).

**Files changed NOT in the spec's files-to-change list:** none beyond those the spec named
(`AnthropicTransient.cs`, `SweepComposer` + options + appsettings, tests) plus the two ritual files
the prompt requires (`CLAUDE.md` entry, this report). `SweepHost.cs` and `Program.cs` were touched —
see Surprises.

## Sources beyond the brief / surprises
- **`SweepHost`/`Program` had to be touched, against the literal "don't change the SweepHost seam."**
  The spec says SweepComposer should *construct* the `SweepRunner`, but `SweepHost.RunOnceAsync`
  builds its own runner internally — so the composer-built runner could never take effect without a
  change reaching `SweepHost`. Resolution: `SweepHost` gained an **optional** 6th ctor param
  `SweepRunner? runner = null` that defaults to the exact prior `new SweepRunner(searcher, triage,
  store)`, so every existing 4-/5-arg construction and all prior tests are byte-for-byte unchanged in
  behaviour. This is the same backward-compatible pattern spec 028 used to add the optional notifier
  param ("the seam stays compatible"). `Program` passes `c.Runner` to both `SweepHost` sites. The
  `SweepRunner`/`SweepHost` *interfaces/seams* (`ISearcher`/`ITriageDecider`/`IFindingStore`/
  `IDigestNotifier`) and the notifier impls are untouched. Flagging for the integrator.
- **Exposed the selected `IRetryPolicy` on `SweepComposition`.** The `SweepRunner` does not expose its
  policy, so to assert "fake tier → `NoRetryPolicy`" directly (the spec's first composer assertion)
  the composition now carries the resolved `RetryPolicy`. This is App-internal metadata; it adds no
  public-API surface beyond the App.
- **C# SDK exception names differ from the skill's table.** `/claude-api`'s `shared/error-codes.md`
  lists Python/TS names (`RateLimitError`, `OverloadedError`, …). The C# SDK uses
  `Anthropic.Exceptions.Anthropic*Exception`. I confirmed the actual C# names + hierarchy + the
  `required HttpStatusCode StatusCode` member by reflecting over the installed 12.29.1 assembly before
  writing the predicate.

## Deferred / not done
- **Live Anthropic + SMTP paths** — still not exercised (need an `ANTHROPIC_API_KEY` / SMTP creds).
  The real `RetryPolicy(IsTransient)` branch is selected and unit-covered offline, but its behaviour
  against actual 429/5xx/timeout responses is unverified by definition (no network in scope).
- **Windows runtime** — gate run on Linux .NET 8.0.422; `win-x64` publish not re-run (artifact, not
  part of the gate; unchanged from prior specs).

## Standing flags
- None new. The pre-existing live-key/live-SMTP deferral (charter's only remaining wiring step) stands.

## Roles update notice
- No role docs (`wiki/roles/`) edited this session.

## Author == executor disclosure
- N/A — authored by CM (spec 041), executed by CC; independent cross-model integration by CX pending.
