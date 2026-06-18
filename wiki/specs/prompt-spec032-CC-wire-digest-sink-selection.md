# Prompt — Spec 032-CC — Wire digest-sink selection into the App

You are **CC**. Execute Spec 032-CC (`wiki/specs/032-CC-wire-digest-sink-selection.md`). Read it first.

## Setup
- `git fetch --prune origin`; branch from origin: `git checkout -b feature/cc-wire-digest-sink origin/main`.
- Prefix every dotnet call: `PATH="$HOME/.dotnet:$PATH" dotnet …`. Confirm `dotnet --version`.

## Steps
1. Extend `NotifyOptions` with `Sink` (`"File"` default | `"Email"` | `"None"`); bind an `Email` section to the
   existing `EmailOptions` (030). Update `appsettings.json` accordingly (`Notify:Sink="File"`, an `Email`
   block with `Enabled:false`).
2. Add a small **pure selection helper** (like `PipelineFactory`) that builds the `IDigestNotifier` from
   config: `File`→`FileDigestNotifier`; `Email`→`EmailDigestNotifier(new SmtpEmailSender(EmailOptions), …)`
   (constructed only; **no live send**); `None` or incomplete/disabled email→`NullDigestNotifier` with a clear
   logged warning. Use it in `Program`. Don't change `SweepHost`, the notifier impls, the Anthropic projects, or the `.sln`.
3. Tests in `tests/AmetekWatch.Tests/`: assert the helper resolves the right `IDigestNotifier` **type** for
   File/Email/None/incomplete-email **without invoking** it (no SMTP/network). Confirm the 028 end-to-end test
   (default File) still passes. Confirm a test can fail then revert.
4. Run `PATH="$HOME/.dotnet:$PATH" dotnet run --project src/AmetekWatch.App` (default File) — capture stdout;
   confirm the file digest is still written.

## The gate
Each separately, real counts: `PATH="$HOME/.dotnet:$PATH" dotnet build -c Release` ; `… dotnet format --verify-no-changes` ; `… dotnet test`.

## Versioning ritual
Internal — no `CHANGELOG`/tag, `<Version>` stays `0.1.0`. Append a dated `CLAUDE.md` → `### Unreleased`
entry (below existing ones; don't edit them).

## Report-back format
Write `wiki/reports/report-spec032-CC-wire-digest-sink-selection.md` per `wiki/rituals/report-format.md` (all
sections; "None." where N/A), plus the `dotnet run` stdout + file-digest confirmation and a gate table (real
counts, before/after, can-fail, clean SHA, `dotnet --version`). Do **not** self-merge; push
`feature/cc-wire-digest-sink` and end with the tip SHA + a one-line build/format/test summary. Never print or
commit secrets.
