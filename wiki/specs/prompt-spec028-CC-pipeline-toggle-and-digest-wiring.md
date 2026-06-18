# Prompt — Spec 028-CC — App: real-vs-fake pipeline toggle + digest wiring

You are **CC**. Execute Spec 028-CC (`wiki/specs/028-CC-pipeline-toggle-and-digest-wiring.md`). Read it first.
This is the **capstone** — it depends on 019 (`AnthropicTriageDecider`), 024 (`AnthropicSearcher`,
`AnthropicMessagesClient`), and 025 (`FileDigestNotifier`), all already on `main`.

## Setup
- `git fetch --prune origin`; branch from origin: `git checkout -b feature/cc-pipeline-toggle origin/main`.
- Prefix every dotnet call: `PATH="$HOME/.dotnet:$PATH" dotnet …`. Confirm `dotnet --version`.

## Steps
1. `dotnet add src/AmetekWatch.App reference src/AmetekWatch.Anthropic`.
2. Add config per spec Decision 2 (`Pipeline:UseRealApi` default false; `Notify:DigestPath` optional) to
   `appsettings.json` + an options record.
3. In `Program` (Decision 3): when `UseRealApi==true`, build `AnthropicMessagesClient` →
   `AnthropicSearcher` (clock `() => DateTimeOffset.UtcNow`) + `AnthropicTriageDecider`; **if
   `ANTHROPIC_API_KEY` is unset, print a clear one-line warning and fall back to the fakes** (don't crash,
   don't silently fake). Log the active pipeline (real/fake). Else use `FakeSearcher`+`FakeTriageDecider`.
   Inject the chosen pair into `SweepHost`.
4. Wire the digest (Decision 4): after the sweep, call `FileDigestNotifier(DigestPath, () =>
   DateTimeOffset.UtcNow)` when `DigestPath` is set, else `NullDigestNotifier`, with the digest.
5. Add a test to `tests/AmetekWatch.Tests/` (Decision 5): fakes + temp DB + temp `DigestPath` → one sweep
   persists to SQLite and writes the digest file (assert existence + a content line); a selection helper
   resolves the **real** Anthropic types when `UseRealApi=true` and fakes when false — assert resolved
   types **without invoking them** (no network, no key). Confirm a test can fail then revert.
6. Run `PATH="$HOME/.dotnet:$PATH" dotnet run --project src/AmetekWatch.App` (default fakes) — capture stdout;
   confirm the digest file is written.
7. Do **not** make a live API call, hardcode a key, or edit the `.sln`.

## The gate
Each separately, real counts: `PATH="$HOME/.dotnet:$PATH" dotnet build -c Release` ; `… dotnet format --verify-no-changes` ; `… dotnet test`.

## Versioning ritual
**This may warrant the first user-facing milestone** — but DEFAULT to internal (`<Version>` stays `0.1.0`,
no `CHANGELOG`) unless you have a strong reason; flag it for the Manager instead of bumping unilaterally.
Append a dated `CLAUDE.md` → `### Unreleased` entry (below existing ones).

## Report-back format
Write `wiki/reports/report-spec028-CC-pipeline-toggle-and-digest-wiring.md` per `wiki/rituals/report-format.md`
(all sections; "None." where N/A), plus: the `dotnet run` stdout + confirmation the digest file was written;
a gate table (real counts, before/after, can-fail, clean SHA, `dotnet --version`); and confirm the **live API
path is still not exercised** (no key). Do **not** self-merge; push `feature/cc-pipeline-toggle` and end with
the tip SHA + a one-line build/format/test summary. Never print or commit secrets/keys.
