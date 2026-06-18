# Prompt — Spec 030-CC2 — Email digest sink

You are **CC2**. Execute Spec 030-CC2 (`wiki/specs/030-CC2-email-digest-sink.md`). Read it first.

## Setup
- `git fetch --prune origin`; branch from origin: `git checkout -b feature/cc2-email-digest origin/main`.
- Prefix every dotnet call: `PATH="$HOME/.dotnet:$PATH" dotnet …`. Confirm `dotnet --version`.

## Steps
1. Extract a pure `DigestMarkdownRenderer` (in `Core/Notify/`) from `FileDigestNotifier` (025) and make
   `FileDigestNotifier` use it — **no behaviour change; its existing tests must still pass unchanged**.
   Friendly-names-only discipline preserved.
2. Add `IEmailSender` (`Task SendAsync(string subject, string body, CancellationToken ct)`), `SmtpEmailSender`
   (`System.Net.Mail.SmtpClient`, configured from `EmailOptions`; any SMTP password from env — never
   hardcoded; the only code not unit-tested), `EmailDigestNotifier : IDigestNotifier` (renders via the shared
   renderer, friendly subject, injected timestamp — **no `DateTimeOffset.Now`**; empty-digest handling
   documented), and an `EmailOptions` record (Enabled, SmtpHost, SmtpPort, From, To[], SubjectPrefix).
3. Add tests to `tests/AmetekWatch.Tests/`: `EmailDigestNotifier` with a **fake `IEmailSender`** asserts the
   friendly subject + body contains the rendered digest (no internal names leak), seeded + empty cases; the
   renderer renders expected Markdown. Confirm a test can fail then revert. **Do not** wire into App/Program,
   send live email, touch the Anthropic projects, or edit the `.sln`.

## The gate
Each separately, real counts: `PATH="$HOME/.dotnet:$PATH" dotnet build -c Release` ; `… dotnet format --verify-no-changes` ; `… dotnet test`.

## Versioning ritual
Internal — no `CHANGELOG`/tag, `<Version>` stays `0.1.0`. Append a dated `CLAUDE.md` → `### Unreleased`
entry (below existing ones; don't edit them).

## Report-back format
Write `wiki/reports/report-spec030-CC2-email-digest-sink.md` per `wiki/rituals/report-format.md` (all
sections; "None." where N/A), plus a gate table (real counts, before/after, can-fail, clean SHA,
`dotnet --version`) and confirmation that `FileDigestNotifier`'s tests still pass after the renderer
extraction and that the live SMTP path is not exercised. Do **not** self-merge; push `feature/cc2-email-digest`
and end with the tip SHA + a one-line build/format/test summary. Never print or commit secrets.
