# Spec 031-CX2 — Integrate CC2's spec-030 email digest sink

> Self-contained integration spec (no separate prompt). Same shape as [`026-CX2`](026-CX2-integrate-digest-notifier.md).

## Status
- Doc type: integration (cross-model gate + review). Executes: **CX2**; CX2 issues VERDICT; **CM lands on PASS**.
- Number 031 verified free (highest spec file = 030; 031 = 030 + 1).
- Reviewing: CC2's `feature/cc2-email-digest` (origin tip `2789007`). Author CC2 (Claude) ≠ integrator CX2 (Codex).
- Final on-disk: `wiki/reviews/review-spec031-CX2-email-sink.md`; on PASS, lands to `main`.

## What to do
1. `git fetch --prune origin`; `git checkout -b feature/cx2-integrate-030 origin/feature/cc2-email-digest`.
   Prefix every dotnet call: `PATH="$HOME/.dotnet:$PATH" dotnet …`.
2. Gate, each separately, real counts: `dotnet build -c Release`; `dotnet format --verify-no-changes`;
   `dotnet test`. Confirm a test can fail (invert one assertion, observe, revert).
3. Review `src/AmetekWatch.Core/Notify/` + tests against spec 030, citing `file:line`:
   - The renderer was **extracted with no behaviour change** — `FileDigestNotifier`'s 025 tests still pass
     **unchanged** (confirm they're present and green); `DigestMarkdownRenderer` keeps **friendly-names-only**.
   - `EmailDigestNotifier` renders via the shared renderer, builds a **friendly subject**, uses an **injected
     timestamp** (no `DateTimeOffset.Now`), and calls `IEmailSender.SendAsync`; tested with a **fake**
     `IEmailSender` (no network). Empty-digest behaviour is defined + tested.
   - `SmtpEmailSender` is the **only** untested code (expected), reads the SMTP password **from env**
     (`AMETEK_WATCH_SMTP_PASSWORD`), and **never hardcodes/commits a secret**. **Grep the diff for any
     literal password/secret — confirm none.**
   - No App/DI wiring; the Anthropic projects and `.sln` are untouched.
4. Write `wiki/reviews/review-spec031-CX2-email-sink.md`: PASS/HOLD headline; branch SHAs; gate table (real
   counts + can-fail + clean SHA + `dotnet --version`); correctness checks (`file:line`); a no-secret
   confirmation; HOLD blockers or "None."; final line exactly `VERDICT: PASS` or `VERDICT: HOLD`. Commit on
   `feature/cx2-integrate-030`, push. **Do not merge to main.**

## Definition of done
- [ ] Gate re-run (real counts, can-fail); review with `file:line`; secret-scan clean; `FileDigestNotifier` tests confirmed green.
- [ ] Review ends `VERDICT: PASS`/`HOLD`; branch pushed; tip SHA + verdict reported.

## Deliverable / report-back
The review file; end your final message with the `feature/cx2-integrate-030` tip SHA + verdict.
