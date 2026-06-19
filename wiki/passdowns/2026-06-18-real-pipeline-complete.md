# Passdown — 2026-06-18 — AMETEK Watch real pipeline complete (offline-verified)

**Headline:** AMETEK Watch is **functionally complete end-to-end**. In a second autonomous CM-driven loop,
the deferred real Anthropic pipeline was built behind the existing seams and wired in — the app now runs the
genuine **two-tier Claude pipeline** (Sonnet 4.6 + server-side `web_search` → Opus 4.8 structured-output
triage with a prompt-cached rubric), **config-selectable** against the deterministic fakes, persisting to
SQLite, serving the dashboard, and exporting a Markdown digest. **76 tests green on `main`** (SHA ~e648020);
every unit cross-model integrated (author ≠ integrator). **The only thing left for a live run is
`ANTHROPIC_API_KEY`.**

## Real-pipeline units landed this loop
- **019** `AnthropicTriageDecider` (Opus 4.8) — `TriageRequestFactory` (cached 011 rubric + structured-output
  schema) + `TriageVerdictParser` + decider, behind `IMessagesClient`; offline-tested via a fake. NuGet `Anthropic` 12.29.1.
- **024** `AnthropicSearcher` (Sonnet 4.6 + `web_search`) — `SearchRequestFactory` (013 queries + JSON-array
  schema) + `SearchResponseParser` + searcher, reusing `IMessagesClient`, mapping via the real 013 mapper.
- **020** searcher heuristic refinement (social source wins over financial title).
- **025** `IDigestNotifier` + `FileDigestNotifier` (friendly Markdown digest) + `NullDigestNotifier`.
- **028 (capstone)** App `Pipeline:UseRealApi` toggle → real adapters vs fakes (**env-key fallback to fakes
  with a clear warning + active-pipeline logging**); `Notify:DigestPath` → digest written after each sweep.

**Design that made it offline-buildable:** every real adapter splits **pure request-factory + response-parser**
(fully unit-tested) from a thin `IMessagesClient`/seam wrapper whose only untested line is the live SDK call
(reads `ANTHROPIC_API_KEY` from env; no key in repo — CX secret-scanned every adapter).

## Versioning decision (CM)
`<Version>` stays **0.1.0 / internal** — **no user-facing v1.0 or `CHANGELOG` yet.** Rationale: the live
pipeline has **not** been verified against the real API (no key), and the Windows `.exe` has been built but
**never executed on Windows** (dev hosts are Linux). Cut the first user-facing release only after a live
smoke test passes and the exe runs on Windows.

## Production-hardening loop — DONE (2026-06-18, specs 034–044; `main` @ b33fa9c, 118 tests)
Since the real pipeline landed, a second loop hardened it into a production-grade unattended monitor — all
cross-model integrated, `main` green throughout:
- **Sweep resilience (034 + 041/043):** a `RetryPolicy` (transient-only via `AnthropicTransient.IsTransient`
  — 429/529/5xx/network retry; 4xx + argument/parse don't) + per-finding triage isolation (one bad finding
  is skipped, the sweep completes). **Active** in the real pipeline via `Pipeline:Retry`. *(042 HOLD on the
  over-strict "no SweepHost seam" constraint was resolved by 043, which blessed the optional runner-injection
  seam + added 401/403 no-retry tests — a clean demonstration of the cross-model gate catching a real issue.)*
- **New-since-last-run (038):** opt-in `Sweep:OnlyReportNew` — a pre-sweep `GetAllAsync` snapshot means the
  digest reports only findings not already stored (no re-reporting); all are still persisted.
- **Scheduled daemon (036):** `Sweep:RunOnce=false` runs a Generic-Host `SweepBackgroundService` looping the
  sweep with graceful Ctrl+C/SIGTERM shutdown. (Refactored the wiring into a shared `SweepComposer`.)
- **Usage guide:** [`docs/USAGE.md`](../../docs/USAGE.md) — all config keys, env vars, run modes, go-live steps.

**The offline backlog is now exhausted** — everything below needs a live key, a Windows host, or owner input.

## Remaining backlog (prioritized — needs key / Windows / owner)
1. **Live verification (needs `ANTHROPIC_API_KEY` + budget).** Set the key, run `dotnet run` with
   `Pipeline:UseRealApi=true`, confirm a real sweep returns/triages/persists findings. **This is the gate to v1.0.**
2. **Live server-tool continuation loop.** `AnthropicMessagesClient.CreateMessageTextAsync` does a single
   `Messages.Create`; the live `web_search` searcher will emit `pause_turn`/`server_tool_use` continuations
   that must be looped (re-send with the assistant turn appended). **Deferred deliberately — it can only be
   verified against the live API**, so do it in the live-verification session. (Noted in 024's XML-docs.)
3. ~~Email digest sink~~ — **DONE** (spec 030 landed `e4ab708`): shared `DigestMarkdownRenderer` +
   `IEmailSender`/`EmailDigestNotifier` (tested via a fake sender) + live `SmtpEmailSender` (password from
   env `AMETEK_WATCH_SMTP_PASSWORD`, untested until creds exist). **Not yet wired into `Program`** — wiring it
   (and selecting file-vs-email via `EmailOptions.Enabled`) is a small follow-up like the capstone.
4. ~~Wire the email sink into `Program`~~ — **DONE** (spec 032 landed `860873f`): a `DigestNotifierFactory`
   selects `File`/`Email`/`None` by `Notify:Sink` (incomplete/disabled email → `NullDigestNotifier` + a
   warning). Digest delivery is now **config-complete**.
5. **Windows packaging / run on Windows** — the hosted-service daemon is **done** (036); what remains needs a
   Windows host: register it as a Windows service / Task-Scheduler job, and run the `win-x64` single-file exe on
   **actual Windows** (built, never
   executed on Windows — dev hosts are Linux). The hosted-service part is offline-buildable; the Windows run is not.
6. **Open charter items (owner):** confirm the project name, default sweep cadence, and the reputable-source
   seed list for the triage rubric.

## Gotchas (carry-forward)
- Same as the prior passdown: branch from `origin/main`; integrators branch from `origin/<author-branch>`;
  prefix every dotnet call `PATH="$HOME/.dotnet:$PATH" dotnet …`; `CLAUDE.md` changelog entries conflict when
  branches land in parallel — CM keeps all entries and re-runs the gate. The `IMessagesClient` seam is the
  reusable Anthropic abstraction — **don't add a second client abstraction**. See [[agent-dispatch-toolchain]],
  [[charter-status]], and `wiki/passdowns/2026-06-18-build-arc-complete.md`.
