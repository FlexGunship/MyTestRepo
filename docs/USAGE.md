# AMETEK Watch — Usage & Configuration

AMETEK Watch periodically sweeps the web for fresh, relevant material about **AMETEK, Inc.** (NYSE: AME),
weighted toward personal/social opinion pieces and reputable financial reports. A two-tier Claude pipeline
does the work — **Sonnet 4.6** searches and aggregates (server-side `web_search` tool), **Opus 4.8** triages
each finding for importance, relevance, and whether it's worth reporting — then it persists to SQLite and
emits a digest (file and/or email). A local web dashboard browses the findings.

> **Status:** fully built and **verified offline** with deterministic fakes (118 tests green). The real
> Anthropic pipeline and SMTP send are wired but **not yet exercised live** — that needs an API key / SMTP
> credentials (see *Going live*). `Version` is `0.1.0` (internal) until a live smoke test passes.

## Build, test, run
```bash
dotnet build -c Release
dotnet test
dotnet run --project src/AmetekWatch.App        # one sweep (default), prints + writes the digest, exits 0
```
Produce the Windows single-file executable:
```bash
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o dist
# -> dist/ametek-watch.exe   (built here on Linux; run it on Windows)
```
Run the dashboard (separate app):
```bash
dotnet run --project src/AmetekWatch.Web         # serves on localhost; GET / and GET /api/findings
```
The dashboard reads the **same SQLite store** (`Storage:DbPath`) the sweeper writes, so run a sweep first.

## Configuration (`src/AmetekWatch.App/appsettings.json`)
| Key | Default | Meaning |
|---|---|---|
| `Sweep:Subject` | `"AMETEK"` | What to search for. |
| `Sweep:IntervalMinutes` | `1440` | Daemon sweep interval (used when `RunOnce=false`). |
| `Sweep:RunOnce` | `true` | `true` = one sweep then exit (CLI). `false` = long-lived daemon (see below). |
| `Sweep:OnlyReportNew` | `false` | `true` = the digest reports only findings **not already in the store** (no re-reporting across runs). All findings are still persisted. Recommended `true` for a daemon. |
| `Storage:DbPath` | `"ametek-watch.db"` | SQLite database file path. |
| `Pipeline:UseRealApi` | `false` | `false` = deterministic fakes (offline). `true` = the **real** Sonnet/Opus pipeline (needs `ANTHROPIC_API_KEY`; falls back to fakes with a warning if the key is missing). |
| `Pipeline:Retry:MaxAttempts` | `3` | Retry attempts for **transient** API errors (rate-limit 429, overloaded 529, 5xx, network) on the real pipeline. |
| `Pipeline:Retry:BaseDelayMs` | `500` | Exponential-backoff base delay. |
| `Notify:Sink` | `"File"` | Digest delivery: `"File"`, `"Email"`, or `"None"`. (Incomplete/disabled email → no-op with a warning.) |
| `Notify:DigestPath` | `"ametek-watch-digest.md"` | File-sink digest path (Markdown). |
| `Notify:Email:Enabled` | `false` | Enable the email sink. |
| `Notify:Email:{SmtpHost,SmtpPort,From,To[],SubjectPrefix}` | example values | SMTP delivery settings. |

Config can also be overridden by environment variables using the standard .NET convention
(e.g. `Pipeline__UseRealApi=true`, `Sweep__OnlyReportNew=true`).

## Secrets (environment variables only — never put these in config)
| Variable | Needed when |
|---|---|
| `ANTHROPIC_API_KEY` | `Pipeline:UseRealApi=true` — the real Sonnet/Opus pipeline. |
| `AMETEK_WATCH_SMTP_PASSWORD` | `Notify:Sink=Email` (or `Notify:Email:Enabled=true`) — SMTP auth. |

## Run modes
- **One-shot (default, `Sweep:RunOnce=true`):** runs a single sweep, persists, writes the digest, exits 0.
  Good for cron / Task Scheduler triggering, and for tests.
- **Daemon (`Sweep:RunOnce=false`):** a .NET Generic Host background service that sweeps every
  `IntervalMinutes` and **shuts down gracefully** on Ctrl+C / SIGTERM. (Windows-service / Task-Scheduler
  registration is not yet provided.)

## Going live (the remaining manual step)
1. Set `ANTHROPIC_API_KEY` in the environment (and a cost budget you're comfortable with).
2. Set `Pipeline:UseRealApi=true` (and, for a daemon, `Sweep:RunOnce=false`, `Sweep:OnlyReportNew=true`).
3. Run `dotnet run --project src/AmetekWatch.App` and confirm a real sweep returns/triages/persists findings.
4. (Optional) For email digests: set `Notify:Sink=Email`, fill `Notify:Email`, and set `AMETEK_WATCH_SMTP_PASSWORD`.

> **Known live follow-up:** the server-side `web_search` tool may return `pause_turn` continuations on a real
> call; the live message client currently does a single request, so a continuation loop may be needed — this
> can only be verified against the live API. See `wiki/passdowns/2026-06-18-real-pipeline-complete.md`.

## Revision history
- 2026-06-18 — Initial usage/configuration guide (CM), reflecting the offline-complete build through spec 043.
