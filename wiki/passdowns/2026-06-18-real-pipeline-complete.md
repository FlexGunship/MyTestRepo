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

## Remaining backlog (prioritized — next session)
1. **Live verification (needs `ANTHROPIC_API_KEY` + budget).** Set the key, run `dotnet run` with
   `Pipeline:UseRealApi=true`, confirm a real sweep returns/triages/persists findings. **This is the gate to v1.0.**
2. **Live server-tool continuation loop.** `AnthropicMessagesClient.CreateMessageTextAsync` does a single
   `Messages.Create`; the live `web_search` searcher will emit `pause_turn`/`server_tool_use` continuations
   that must be looped (re-send with the assistant turn appended). **Deferred deliberately — it can only be
   verified against the live API**, so do it in the live-verification session. (Noted in 024's XML-docs.)
3. **Email digest sink** (charter's "email hooks") — *(in flight as spec 030 at write time)*: an `IEmailSender`
   seam + tested `EmailDigestNotifier` + untested live `SmtpEmailSender`, same pattern as the adapters.
4. **Scheduling / Windows packaging** — wrap `SweepHost.RunAsync` as a proper hosted service / Task Scheduler
   job; run the `win-x64` single-file exe on actual Windows.
5. **Open charter items (owner):** confirm the project name, default sweep cadence, and the reputable-source
   seed list for the triage rubric.

## Gotchas (carry-forward)
- Same as the prior passdown: branch from `origin/main`; integrators branch from `origin/<author-branch>`;
  prefix every dotnet call `PATH="$HOME/.dotnet:$PATH" dotnet …`; `CLAUDE.md` changelog entries conflict when
  branches land in parallel — CM keeps all entries and re-runs the gate. The `IMessagesClient` seam is the
  reusable Anthropic abstraction — **don't add a second client abstraction**. See [[agent-dispatch-toolchain]],
  [[charter-status]], and `wiki/passdowns/2026-06-18-build-arc-complete.md`.
