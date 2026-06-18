# Spec 030-CC2 — Email digest sink (offline-buildable)

## Status
- Doc type: implementation (the charter's "email hooks" — a real email sink; live SMTP gated like the API)
- Executes: **CC2**; pushes `feature/cc2-email-digest`; **CX2** integrates (cross-model); CM lands on PASS. No self-merge.
- Number 030 verified free (search `wiki/specs/`; this is highest + 1).
- Paired prompt: prompt-spec030-CC2-email-digest-sink.md
- Final on-disk: new files under `src/AmetekWatch.Core/Notify/` (+ a small refactor of `FileDigestNotifier`) + a test in `tests/AmetekWatch.Tests/` (no `.sln` change).

## Background
The charter left **email delivery hooks open**. 025 added `IDigestNotifier` + a file sink. This adds an
**email** sink the same offline-buildable way: a pure renderer + an `IEmailSender` seam (testable) and a
thin live `SmtpEmailSender` (untested until SMTP creds exist — mirrors the Anthropic `IMessagesClient`
pattern).

## Decisions made
1. **Extract a shared renderer** — pull the friendly digest-Markdown rendering out of `FileDigestNotifier`
   (025) into a small pure `DigestMarkdownRenderer` (in `Core/Notify/`) and have `FileDigestNotifier` use it
   (no behaviour change — its existing tests must still pass unchanged). This avoids duplicating the
   friendly-name rendering. Keep the **friendly-names-only** discipline (no internal type/field/enum names in
   the rendered text).
2. **`IEmailSender`** — `Task SendAsync(string subject, string body, CancellationToken ct)`.
3. **`SmtpEmailSender : IEmailSender`** — the **only** code not unit-tested (like the live Anthropic wrapper):
   uses `System.Net.Mail.SmtpClient` (BCL, no NuGet) configured from `EmailOptions`; reads any SMTP password
   from the environment (**never hardcoded/committed**). Keep it minimal.
4. **`EmailDigestNotifier : IDigestNotifier`** — ctor takes `IEmailSender`, an `EmailOptions` (subject prefix,
   recipients), the shared `DigestMarkdownRenderer`, and an injected timestamp provider (**no
   `DateTimeOffset.Now`**). `NotifyAsync` renders the digest body and calls `IEmailSender.SendAsync` with a
   friendly subject (e.g. "AMETEK Watch — N findings worth reporting"). Empty digest → a clean "nothing to
   report" email (or skip — your call, documented).
5. **`EmailOptions`** record (Enabled, SmtpHost, SmtpPort, From, To[], SubjectPrefix). **No App/DI wiring
   here** (wiring into `Program` is a later tiny spec) — keep this confined to `Core/Notify` + tests.
6. **Tests** (`tests/AmetekWatch.Tests/`): `EmailDigestNotifier` with a **fake `IEmailSender`** (captures the
   call) asserts the friendly subject + that the body contains the rendered digest (and no internal names
   leak), for a seeded digest and the empty case; the shared `DigestMarkdownRenderer` renders the expected
   Markdown (move/keep the 025 oracles). `SmtpEmailSender` is **not** unit-tested (note it). Hand-computed;
   confirm a test can fail then revert. **`FileDigestNotifier`'s existing tests must still pass.**

## Out of scope
- Live SMTP send (needs creds — deferred like the API key). App/DI wiring (later). HTML email (Markdown body is fine).

## Definition of done
- [ ] Shared `DigestMarkdownRenderer`; `FileDigestNotifier` uses it with its tests still green.
- [ ] `IEmailSender` + `SmtpEmailSender` (untested live) + `EmailDigestNotifier` + `EmailOptions`.
- [ ] Tests (email notifier via fake sender + renderer); can-fail confirmed.
- [ ] Gate green (build/format/test, each separately, real counts). Branch pushed; tip SHA reported.

## Deliverable / report-back
See the prompt file.
