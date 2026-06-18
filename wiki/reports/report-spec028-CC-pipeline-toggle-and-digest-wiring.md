# Report — Spec 028-CC: App real-vs-fake pipeline toggle + digest wiring (capstone)

**Headline outcome:** Built on branch `feature/cc-pipeline-toggle`; **NOT merged** (no self-merge —
CX integrates cross-model). `<Version>` stays **0.1.0** (internal) — but this capstone closes the
end-to-end product loop and is **FLAGGED to the Manager as a candidate first user-facing milestone**
(see Standing flags). Gate green on Linux .NET 8.0.422: build 0 warn / format clean / **76 tests pass**
(was 73). The **live Anthropic API path is still NOT exercised** (no key).

## 1. Branch / merge state
- Pre-merge `main` SHA: `ce46b4593d9b57262839aac471e79f185413989e` (= `origin/main`)
- Feature branch: `feature/cc-pipeline-toggle`; working commit: **see final hand-off line** (tip SHA
  reported after commit/push); branch deleted post-merge: **n** (CX integrates).
- Post-merge `main` SHA (pushed): **n/a** — not merged by author.
- Merge mechanic: **pushed branch; cross-model integrator (CX) merges** (author ≠ integrator).

## 2. Changes
| File | Change |
| --- | --- |
| `src/AmetekWatch.App/AmetekWatch.App.csproj` | Added `ProjectReference` to `AmetekWatch.Anthropic` (via `dotnet add … reference`; **no `.sln` edit**). |
| `src/AmetekWatch.App/appsettings.json` | Added `Pipeline:{UseRealApi:false}` and `Notify:{DigestPath:"ametek-watch-digest.md"}`. |
| `src/AmetekWatch.App/PipelineOptions.cs` | **New** options record bound from the `Pipeline` section (`UseRealApi`, default false). |
| `src/AmetekWatch.App/NotifyOptions.cs` | **New** options record bound from the `Notify` section (`DigestPath`, optional). |
| `src/AmetekWatch.App/PipelineFactory.cs` | **New** pure selection helper: `Create(useRealApi, realClientFactory?, clock?)` → `(ISearcher, ITriageDecider)`; real adapters when true, fakes when false. Invokes nothing. |
| `src/AmetekWatch.App/SweepHost.cs` | Added optional 5th ctor param `IDigestNotifier? notifier = null` (defaults to `NullDigestNotifier`); `RunOnceAsync` delivers the digest through it after persisting. |
| `src/AmetekWatch.App/Program.cs` | Binds `Pipeline`/`Notify`; selects real-vs-fake (env-key fallback w/ warning); logs active pipeline; wires `FileDigestNotifier`/`NullDigestNotifier`; constructs `SweepHost` with the notifier. |
| `tests/AmetekWatch.Tests/PipelineToggleAndDigestTests.cs` | **New** 3 tests (selection real/fake + digest-written). No new test project, no `.sln` edit. |
| `.gitignore` | Ignore the local `ametek-watch-digest.md` runtime digest file (alongside the existing `.db`). |
| `CLAUDE.md` | Appended a dated `### Unreleased` entry (capstone). |

## 3. Pipeline selection & env-key fallback (Decision 3)
- `PipelineFactory.Create` (`src/AmetekWatch.App/PipelineFactory.cs:30`) does **type selection only** —
  builds objects, invokes nothing. The real branch wires `AnthropicSearcher` (clock
  `() => DateTimeOffset.UtcNow`) + `AnthropicTriageDecider` over a single shared `IMessagesClient`.
- The **env-key check + warn-and-fall-back is in `Program`** (`src/AmetekWatch.App/Program.cs:34-58`),
  deliberately kept out of the helper so the real path is type-resolvable with no key (the test relies
  on this). When `UseRealApi==true` and `ANTHROPIC_API_KEY` is unset, `Program` prints a one-line
  WARNING and falls back to the fakes — **no crash, no silent fake** — and logs `FAKE (fell back: …)`.

## 4. Digest wiring (Decision 4)
- `SweepHost.RunOnceAsync` (`src/AmetekWatch.App/SweepHost.cs`) calls
  `IDigestNotifier.NotifyAsync(digest, ct)` after persisting. The notifier is an **optional** ctor param
  defaulting to `NullDigestNotifier`, so the 015 `SweepHostTests` (4-arg construction) are untouched.
- `Program` builds `FileDigestNotifier(DigestPath, Subject, () => DateTimeOffset.UtcNow)` when `DigestPath`
  is set, else `NullDigestNotifier`. (The notifier ctor takes `subject` too — the prompt's shorthand
  `FileDigestNotifier(DigestPath, () => …)` is honoured with `options.Subject` supplied for the heading.)

## 5. `dotnet run` stdout + digest-file confirmation (default fakes)
`PATH="$HOME/.dotnet:$PATH" dotnet run -c Release --project src/AmetekWatch.App` → **exit 0**:
```
AMETEK Watch — sweep for "AMETEK"
Pipeline:               FAKE (deterministic; Pipeline:UseRealApi=false)
Store (SQLite):         ametek-watch.db
Digest sink:            ametek-watch-digest.md
Persisted findings:     4
Worth-reporting digest: 3

[1] FinancialReport — AMETEK reports Q2 earnings beat
    url:       https://ir.example.com/ametek-q2-earnings
    rationale: Category FinancialReport is reportable under the slice rule.
[2] OpinionSocial — AMETEK shares climb on upbeat analyst note
    url:       https://news.example.com/ametek-analyst-note
    rationale: Category OpinionSocial is reportable under the slice rule.
[3] FinancialReport — AMETEK files Form 10-Q
    url:       https://sec.example.com/ametek-10q
    rationale: Category FinancialReport is reportable under the slice rule.
```
**Digest file confirmed written** (`ametek-watch-digest.md`, resolved against CWD; gitignored). Content:
```
# AMETEK Watch digest

_Generated Thursday, 18 June 2026 21:44 UTC_

**3 items worth reporting.**

## Financial Report: AMETEK reports Q2 earnings beat
- Link: https://ir.example.com/ametek-q2-earnings
- Why it matters: Category FinancialReport is reportable under the slice rule.
…(Opinion/Social + 10-Q sections follow)
```

## Gate results
Each command run **separately**, real counts, clean SHA = the pushed branch tip (final line).
| Gate (Linux .NET **8.0.422**) | Result |
| --- | --- |
| `dotnet build -c Release` | ✓ 0 warnings, 0 errors |
| `dotnet format --verify-no-changes` | ✓ clean (no output) |
| `dotnet test` | ✓ **76 passed**, 0 failed (Storage 4 + Anthropic 30 + AmetekWatch.Tests 40 + Web 2) |

- Test count **before**: 73 (AmetekWatch.Tests 37). **After**: 76 (AmetekWatch.Tests **40**, +3 this spec).
- **Can-fail confirmed**: broke the digest count oracle (`3 items` → `999 items`) → 1 fail in
  `RunOnce_WithDigestPath_…`; reverted; full suite green again.
- Files changed **not in the spec's files-to-change list**: `.gitignore` (added the digest-file ignore,
  mirroring the existing `.db` ignore) and `CLAUDE.md` (required roadmap entry). Both expected/benign.

## Sources beyond the brief / surprises
- **`FileDigestNotifier` ctor needs a `subject`** (3 args: path, subject, timestamp), not the 2-arg
  shorthand in the prompt. Supplied `options.Subject` for the heading — faithful to intent.
- **Default `DigestPath` is non-empty** (`ametek-watch-digest.md`). Decision 2 calls `DigestPath`
  "optional/empty", but the DoD requires the default `dotnet run` to **write** the digest file — so the
  shipped default points at a real (gitignored) path. Empty/absent still selects `NullDigestNotifier`
  (capability preserved); only the shipped default is non-empty.
- **Relative path resolution**: `DigestPath`/`DbPath` are relative and resolve against the process CWD
  (repo root under `dotnet run`), while `appsettings.json` is read from `AppContext.BaseDirectory`. Both
  runtime files are gitignored. Pre-existing behaviour for the DB (015); the digest now matches it.

## Deferred / not done
- **Live Anthropic API path NOT exercised** — needs `ANTHROPIC_API_KEY`. `AnthropicMessagesClient`'s live
  call (and the 024 `web_search` `pause_turn`/`stop_reason` continuation loop) remain untested offline, as
  scoped out by the spec. The selection test resolves the real adapter **types** only; no call is made.
- The real-with-no-key fallback branch in `Program` is verified by reasoning + the default-fakes run, not
  by an automated test (would require manipulating process env for a top-level program); the selection
  helper itself is unit-tested.

## Standing flags
- **Versioning — FLAGGED to the Manager.** Per the versioning ritual I defaulted to internal
  (`<Version>` stays `0.1.0`, no `CHANGELOG`). But 028 is the **capstone**: the App now runs a genuine
  end-to-end product (config-selectable real Sonnet→Opus pipeline + persisted findings + emitted digest),
  offline-complete and one live key away from production. This is a plausible **first user-facing
  milestone** (e.g. `0.1.0 → 1.0.0` + a `CHANGELOG`). I did **not** bump unilaterally — Manager's call.

## Roles update notice
None — no role doc edited this session.
