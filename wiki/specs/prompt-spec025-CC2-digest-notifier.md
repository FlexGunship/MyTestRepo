# Prompt — Spec 025-CC2 — Digest notifier seam + file sink

You are **CC2**. Execute Spec 025-CC2 (`wiki/specs/025-CC2-digest-notifier.md`). Read it first.

## Setup
- `git fetch --prune origin`; branch from origin: `git checkout -b feature/cc2-digest-notifier origin/main`.
- Prefix every dotnet call: `PATH="$HOME/.dotnet:$PATH" dotnet …`. Confirm `dotnet --version`.

## Steps
1. Create `src/AmetekWatch.Core/Notify/` with `IDigestNotifier`, `FileDigestNotifier` (writes a friendly
   Markdown digest — heading w/ subject+date, worth-reporting count, then per finding Category/Title/Url +
   the decider rationale, **friendly names only**; overwrite each run; inject the timestamp — **no
   `DateTimeOffset.Now`**), and `NullDigestNotifier` (no-op). Pure C#, no new NuGet/project.
2. Add `tests/AmetekWatch.Tests/DigestNotifierTests.cs` to the **existing** `AmetekWatch.Tests` project (no
   `.csproj`/`.sln` edit): `FileDigestNotifier` → temp file, assert rendered content (seeded digest + an
   empty "nothing to report" case); `NullDigestNotifier` writes nothing. Hand-computed; confirm a test can
   fail then revert.
3. Do **not** wire into `SweepHost`/App, add email/SMTP, touch the Anthropic projects, or edit the `.sln`.

## The gate
Each separately, real counts: `PATH="$HOME/.dotnet:$PATH" dotnet build -c Release` ; `… dotnet format --verify-no-changes` ; `… dotnet test`.

## Versioning ritual
Internal — no `CHANGELOG`/tag, `<Version>` stays `0.1.0`. Append a dated `CLAUDE.md` → `### Unreleased`
entry (below existing ones; don't edit them).

## Report-back format
Write `wiki/reports/report-spec025-CC2-digest-notifier.md` per `wiki/rituals/report-format.md` (all
sections; "None." where N/A), plus a gate table (real counts, before/after, can-fail, clean SHA,
`dotnet --version`). Do **not** self-merge; push `feature/cc2-digest-notifier` and end with the tip SHA +
a one-line build/format/test summary. Never print or commit secrets.
