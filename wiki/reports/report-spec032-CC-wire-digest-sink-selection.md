# Report — Spec 032-CC: Wire digest-sink selection (File/Email/None) into the App

**Headline outcome:** NOT merged (no self-merge — CX integrates). App-only wiring that makes digest
delivery config-selectable (`Notify:Sink` = File / Email / None) via a new pure
`DigestNotifierFactory`. No version bump (`<Version>` stays `0.1.0`). Branch
`feature/cc-wire-digest-sink` pushed. Gate green on Linux .NET 8.0.422: build 0 warn, format clean,
test **89/89** (was 81).

## 1. Branch / merge state
- Pre-merge `main` SHA: `019d8fc33e9d91a6e001ae3063f353093067b418`
- Feature branch: `feature/cc-wire-digest-sink`; working commit(s): `b770568` (code + CLAUDE.md),
  plus a follow-up commit adding this report; branch deleted: n
- Post-merge `main` SHA (pushed): N/A — not merged (CX integrates, cross-model; author ≠ integrator)
- Merge mechanic: pushed branch; integrator merges. **No self-merge.**

## 2. Changes
| File | Change |
|---|---|
| `src/AmetekWatch.App/NotifyOptions.cs` | Added `Sink` (`"File"` default / `"Email"` / `"None"`) and `EmailOptions? Email`; updated XML-doc. |
| `src/AmetekWatch.App/DigestNotifierFactory.cs` | **New** pure helper: resolves `IDigestNotifier` by `Sink` (construction-only, invokes nothing); `IsEmailUsable` validation; warns via injected `Action<string>?`. |
| `src/AmetekWatch.App/Program.cs` | Binds `Notify` explicitly (tolerant `TryBindEmail`); replaced the inline file-only sink selection with `DigestNotifierFactory.Create(…, Console.WriteLine)`; sink line now logs `Sink -> <notifier type>`. |
| `src/AmetekWatch.App/appsettings.json` | Added `Notify:Sink="File"` and a disabled `Email` template block (`Enabled:false`, placeholder host/from/to, `SubjectPrefix:"AMETEK Watch"`). |
| `tests/AmetekWatch.Tests/DigestSinkSelectionTests.cs` | **New** 8 selection tests (resolved types only; no SMTP/network). |
| `CLAUDE.md` | Appended dated `### Unreleased` entry (below existing; none edited). |
| `wiki/reports/report-spec032-CC-wire-digest-sink-selection.md` | **New** this report. |

## 3. What was built (spec-specific)
**Selection helper (Decision 2).** `DigestNotifierFactory.Create(NotifyOptions notify, string subject,
Func<DateTimeOffset> clock, Action<string>? warn = null)` — mirrors `PipelineFactory`: it builds
objects but invokes nothing, so it is fully exercised offline (no network, no key, no send). Mapping:
- `File` + non-empty `DigestPath` → `FileDigestNotifier(DigestPath, subject, clock)`.
- `Email` + usable config → `EmailDigestNotifier(new SmtpEmailSender(EmailOptions), EmailOptions, new
  DigestMarkdownRenderer(), subject, clock)`. The live `SmtpEmailSender` is **constructed only** on
  this path — no `SendAsync`, no SMTP, no network.
- `None`, an unrecognized sink, a `File` sink with empty path, or an incomplete/disabled email →
  `NullDigestNotifier` with a clear warning through the injected callback (default no-op; `Program`
  passes `Console.WriteLine`). `IsEmailUsable` requires: `Enabled` + non-blank `SmtpHost`/`From`/
  `SubjectPrefix` + ≥1 non-blank recipient.

**Config + binding.** `NotifyOptions` gained `Sink` and `Email` (bound to the existing 030
`EmailOptions`). `appsettings.json` ships `Notify:Sink="File"`, the existing `DigestPath`, and a
disabled `Email` template. `Program` binds `Notify` **explicitly** (Sink/DigestPath via the config
indexer; Email via a tolerant `TryBindEmail`) — see Surprises for why `Get<NotifyOptions>()` could not
be used.

**Tests (Decision 4).** `DigestSinkSelectionTests` — 8 tests asserting resolved **types** only,
nothing invoked: File→`FileDigestNotifier`; File-no-path→`NullDigestNotifier`(+warn);
Email-complete→`EmailDigestNotifier` (constructed, never invoked — no SMTP/network);
Email-disabled→`Null`(+warn); Email-incomplete-host→`Null`(+warn); Email-no-config→`Null`;
None→`Null`; unknown-sink→`Null`(+warn). The 028 end-to-end test (default File) passes unchanged.

**`dotnet run` (default File) — stdout:**
```
AMETEK Watch — sweep for "AMETEK"
Pipeline:               FAKE (deterministic; Pipeline:UseRealApi=false)
Store (SQLite):         ametek-watch.db
Digest sink:            File -> FileDigestNotifier
Persisted findings:     4
Worth-reporting digest: 3
...
```
Exit 0. The file digest `ametek-watch-digest.md` (gitignored) was written with real content
(heading + `**3 items worth reporting.**` + the 3 sections). Confirmed.

## Gate results
| Command | Result |
|---|---|
| `dotnet build -c Release` | ✓ 0 warning, 0 error |
| `dotnet format --verify-no-changes` | ✓ (exit 0) |
| `dotnet test` (`AmetekWatch.sln`) | ✓ 89/89 (81 before → 89 after) — Storage 4, Anthropic 30, Web 2, Core 53 (45→53, +8) |
- Can-fail confirmed: flipped the `None_ResolvesNullNotifier` oracle to expect `EmailDigestNotifier`
  → 1 failed (52/53 in AmetekWatch.Tests); reverted to 89/89.
- Clean-gate SHA: `b770568` (working commit; this report is doc-only, added after).
- `dotnet --version`: 8.0.422.
- Files changed not in the spec's list: `CLAUDE.md` (required by the versioning ritual) and this
  report. No `.sln`, `SweepHost`, notifier-impl, or Anthropic-project changes (per Decision 3).

## Sources beyond the brief / surprises
- **`Get<NotifyOptions>()` crashes on an empty/partial `Email` block.** `EmailOptions` (030) is a
  positional record whose constructor parameters (`To`, `From`, …) have no defaults, so the config
  binder throws `InvalidOperationException` ("parameter 'To' has no matching config") when the `Email`
  section has an empty array `"To": []` or omits a required key. An incomplete email config is exactly
  the **graceful-fallback** case the spec wants (warn → Null), not a crash. Resolution (App-only, no
  Core/030 change): `Program` binds `Notify` explicitly and binds `Email` via `TryBindEmail`, which
  catches that exception and degrades to `null` (→ factory warns → `NullDigestNotifier`). The shipped
  default `Email` block is populated-but-disabled so it also binds cleanly. **Flag for Manager/CX:**
  the binder fragility is intrinsic to the positional `EmailOptions` record; if other call sites later
  bind it, they need the same tolerance (or `EmailOptions` could gain binder-friendly defaults — a
  Core change, deliberately not made here).
- The warning is surfaced through an injected `Action<string>?` (default no-op) rather than an ambient
  logger, to keep the helper pure/unit-testable while still satisfying "clear logged warning"
  (`Program` injects `Console.WriteLine`).

## Deferred / not done
- Live SMTP send (`SmtpEmailSender.SendAsync`) — constructed but never invoked offline; awaits SMTP
  creds (`AMETEK_WATCH_SMTP_PASSWORD`). Out of scope per the spec.
- Live Anthropic pipeline — unchanged, still awaits `ANTHROPIC_API_KEY`.
- HTML email, scheduling/Windows packaging, the server-tool continuation loop — out of scope.

## Standing flags
- The capstone (028) flagged a candidate first user-facing milestone (0.1.0 → 1.0.0 + `CHANGELOG`);
  still the Manager's call. Not touched here.

## Roles update notice
None — no role doc edited this session.
