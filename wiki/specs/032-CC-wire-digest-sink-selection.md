# Spec 032-CC — Wire digest-sink selection into the App

## Status
- Doc type: implementation (connect the landed email sink — make digest delivery config-selectable)
- Executes: **CC**; pushes `feature/cc-wire-digest-sink`; **CX** integrates (cross-model); CM lands on PASS. No self-merge.
- Number 032 verified free (search `wiki/specs/`; this is highest + 1).
- Paired prompt: prompt-spec032-CC-wire-digest-sink-selection.md
- Final on-disk: `src/AmetekWatch.App/` (Program/NotifyOptions/appsettings) + a test in `tests/AmetekWatch.Tests/`.

## Background
028 wired the **file** digest sink into `Program`; 030 added an **email** sink (`EmailDigestNotifier` +
`SmtpEmailSender`) but **left it unwired**, so the running app can't use it. This closes that gap: the App
**selects the digest sink by config** — File / Email / None.

## Decisions made
1. **Extend `NotifyOptions`** (028) with a `Sink` selector: `"File"` (default) | `"Email"` | `"None"`. Add an
   `Email` config section bound to the existing `EmailOptions` (030).
2. **`Program` builds the `IDigestNotifier` by `Sink`:**
   - `File` → `FileDigestNotifier(DigestPath, clock)` (current behaviour, default).
   - `Email` → `EmailDigestNotifier(new SmtpEmailSender(EmailOptions), EmailOptions, renderer, clock)`. The
     live `SmtpEmailSender` is constructed only on this path (no live send in tests). If `Sink="Email"` but
     `EmailOptions` is incomplete/disabled, **log a clear warning and fall back to `NullDigestNotifier`** (don't
     crash) — mirror the pipeline-toggle fallback discipline.
   - `None` → `NullDigestNotifier`.
   Keep a small **pure selection helper** (like `PipelineFactory`) so this is unit-testable without sending.
3. **Do not** change `SweepHost`'s seam (it already takes an `IDigestNotifier`), the Anthropic projects, the
   notifier implementations (019/024/025/030), or the `.sln`. App-only wiring.
4. **Tests** (`tests/AmetekWatch.Tests/`): the selection helper resolves `FileDigestNotifier` for `File`,
   `EmailDigestNotifier` for `Email` (constructed but **not invoked** — no SMTP send/network), and
   `NullDigestNotifier` for `None`/incomplete-email; assert resolved **types** only. The existing 028
   end-to-end test (default File) must still pass. Hand-computed; confirm a test can fail then revert.

## Out of scope
- Live SMTP send / live API. HTML email. Scheduling/Windows packaging. The server-tool continuation loop.

## Definition of done
- [ ] `NotifyOptions.Sink` (File/Email/None) + `EmailOptions` bound; `Program`/selection helper builds the
      right sink (email-incomplete → Null with a warning).
- [ ] `dotnet run` (default File) still runs one sweep, persists, writes the file digest, exit 0.
- [ ] Selection tests (File/Email/None, no send); 028 end-to-end test still green; can-fail confirmed.
- [ ] Gate green (build/format/test, each separately, real counts). Branch pushed; tip SHA reported.

## Deliverable / report-back
See the prompt file.
