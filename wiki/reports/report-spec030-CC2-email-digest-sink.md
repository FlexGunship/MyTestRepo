# Report — Spec 030-CC2: Email digest sink

**Headline outcome:** Built offline. **Not merged** (no self-merge — CX2 integrates). Branch
`feature/cc2-email-digest` pushed; no version bump (`<Version>` stays `0.1.0`). Gate green on Linux
.NET 8.0.422 (build 0 warn / format clean / test 81/81). The shared renderer extraction left
`FileDigestNotifier`'s four 025 tests passing **unchanged**, and the live SMTP path is **not**
exercised (deferred until creds exist).

## 1. Branch / merge state
- Pre-merge `main` SHA: `784a5d87df571c0c6b04d7b5c0e44f2b9d75ea3a`
- Feature branch: `feature/cc2-email-digest`; working commit: `01844c27a2dd289c82558aa55a65c813987dc0d9`; branch deleted post-merge: n
- Post-merge `main` SHA (pushed): N/A — not merged (CX2 integrates cross-model; no self-merge)
- Merge mechanic: pushed branch; integrator (CX2) merges via gated `--no-ff`

## 2. Changes
| File | Change |
| --- | --- |
| `src/AmetekWatch.Core/Notify/DigestMarkdownRenderer.cs` | **New.** Pure shared renderer — friendly Markdown digest (heading + run time + count + one section/finding with friendly kind/title/link/rationale). Extracted verbatim from `FileDigestNotifier.Render` (025). No I/O, no clock (run time injected), friendly-names-only. |
| `src/AmetekWatch.Core/Notify/FileDigestNotifier.cs` | **Refactor, no behaviour change.** Now composes `DigestMarkdownRenderer` instead of its own `Render`/`FriendlyKind` (both removed). Public ctor/`NotifyAsync` signatures unchanged. |
| `src/AmetekWatch.Core/Notify/IEmailSender.cs` | **New.** Transport seam: `Task SendAsync(string subject, string body, CancellationToken ct)`. |
| `src/AmetekWatch.Core/Notify/SmtpEmailSender.cs` | **New.** Live `IEmailSender` over BCL `System.Net.Mail.SmtpClient` (no NuGet), configured from `EmailOptions`, `EnableSsl=true`. SMTP password read **only** from env `AMETEK_WATCH_SMTP_PASSWORD`. The **only** code not unit-tested. |
| `src/AmetekWatch.Core/Notify/EmailDigestNotifier.cs` | **New.** `IDigestNotifier` that renders via the shared renderer and sends a friendly subject (`"<prefix> — N findings worth reporting"`, `"… — nothing to report"` when empty). Injected timestamp provider (no `DateTimeOffset.Now`). Empty digest still sends a clean notice (documented). |
| `src/AmetekWatch.Core/Notify/EmailOptions.cs` | **New.** Record `(bool Enabled, string SmtpHost, int SmtpPort, string From, string[] To, string SubjectPrefix)`. Config-only; carries no secrets. |
| `tests/AmetekWatch.Tests/EmailDigestNotifierTests.cs` | **New.** 5 tests with a fake `IEmailSender` + the real renderer. |
| `CLAUDE.md` | Appended dated `### Unreleased` entry (below existing; none edited). |
| `wiki/reports/report-spec030-CC2-email-digest-sink.md` | This report. |

## 3. Renderer extraction — behaviour preservation
- `DigestMarkdownRenderer.Render(digest, subject, runTime)` is the byte-for-byte logic moved from
  `FileDigestNotifier.Render` (025): same heading, same `_Generated … UTC_` line (run time → UTC,
  invariant culture), same `**N items worth reporting.**` count with singular/plural noun, same
  per-item `## <FriendlyKind>: <title>` + `- Link:` + `- Why it matters:` shape, same empty
  "Nothing to report this run." form. `FriendlyKind` moved with it (Opinion / Social, Financial
  Report, Other).
- **`FileDigestNotifier`'s four 025 tests pass unchanged** — confirmed by
  `dotnet test --filter FullyQualifiedName~Tests.DigestNotifierTests` → 4/4 passed. Those tests
  drive `FileDigestNotifier.NotifyAsync` (not `Render` directly), so the extraction is invisible to
  them. The test file (`DigestNotifierTests.cs`) was **not** edited.
- Friendly-names-only discipline preserved: the seeded email test asserts none of
  `OpinionSocial / FinancialReport / WorthReporting / Rationale / DiscoveredAt / TriagedFinding /
  Verdict / Category` appears in the subject **or** body.

## 4. Email seam — design notes
- `IEmailSender` mirrors the Anthropic `IMessagesClient` split: pure notifier logic (subject + body
  composition, fully tested) vs. one untestable live line (the SMTP send).
- `SmtpEmailSender`: password sourced from env `AMETEK_WATCH_SMTP_PASSWORD` only; if absent the
  client sends without explicit credentials. **No secret is hardcoded, printed, or committed.**
- `EmailDigestNotifier` empty-digest handling: **sends** a clean "nothing to report" email (subject
  `"<prefix> — nothing to report"`, body = renderer's notice). Documented in XML doc; callers who
  prefer to skip empty runs can guard upstream.

## 5. Tests (5 new, hand-computed oracles)
1. `SendsFriendlySubjectAndRenderedBody_ForSeededDigest` — two-item digest; subject
   `"AMETEK Watch — 2 findings worth reporting"`; exact full-string body; +02:00 stamp normalises to
   `14:30 UTC`; no internal names leak in subject/body; exactly 1 send.
2. `SendsNothingToReportEmail_ForEmptyDigest` — subject `"AMETEK Watch — nothing to report"`; exact
   notice body; 1 send.
3. `UsesSingularNoun_ForOneFinding` — subject `"AMETEK Watch — 1 finding worth reporting"`.
4. `RendererRendersExpectedMarkdown` — renderer directly, seeded digest, exact full-string oracle.
5. `RendererRendersNothingToReport_ForEmptyDigest` — renderer directly, empty digest, exact oracle.
- Fake `IEmailSender` captures subject/body/count — **no live SMTP**.
- Can-fail confirmed: flipped the subject count oracle (`2 findings` → `99 findings`) →
  `Failed: 1, Passed: 4`; reverted (file restored from backup, verified by re-run).

## Gate results
Run separately, prefixed `PATH="$HOME/.dotnet:$PATH"`. Clean SHA: `01844c27a2dd289c82558aa55a65c813987dc0d9`. `dotnet --version`: **8.0.422**.

| Command | Result | Notes |
| --- | --- | --- |
| `dotnet build -c Release` | ✓ | 0 Warning(s), 0 Error(s) |
| `dotnet format --verify-no-changes` | ✓ | clean (no output) |
| `dotnet test` | ✓ | **81/81** passed (before: **76**, after: **81**, +5) |

Per-project after: AmetekWatch.Tests 45 (was 40, +5) · AmetekWatch.Storage.Tests 4 · AmetekWatch.Anthropic.Tests 30 · AmetekWatch.Web.Tests 2.

Can-fail: confirmed (subject-count oracle flip → 1 fail) then reverted.

Files changed NOT in the spec's files-to-change list: none beyond the expected `CLAUDE.md` entry and
this report. No `.sln` edit (the new Core files compile under the existing `AmetekWatch.Core` project;
the new test compiles under the existing `AmetekWatch.Tests` project).

## Sources beyond the brief / surprises
None. The 025 `FileDigestNotifier.Render` was `internal static` and only ever called through
`NotifyAsync` — no external caller of `Render` existed, so its removal in favour of the shared
renderer touched nothing outside the file.

## Deferred / not done
- **Live SMTP send** — `SmtpEmailSender` compiles but is **not** exercised (needs real creds;
  deferred like the Anthropic key).
- **App/Program/DI wiring** — `EmailDigestNotifier`/`EmailOptions` are not registered or bound to
  config; deferred to a later tiny wiring spec per the spec's "Out of scope".
- **HTML email** — out of scope; Markdown body only.

## Standing flags
None new. The 028 capstone's flag to the Manager (candidate first user-facing milestone, `0.1.0` →
`1.0.0` + `CHANGELOG`) still stands and is unaffected by this spec.

## Roles update notice
None — no role doc edited this session.
