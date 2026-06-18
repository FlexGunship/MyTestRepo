# Prompt — Spec 011-CC — Triage prompt & rubric builder

You are **CC**. Execute Spec 011-CC (`wiki/specs/011-CC-triage-prompt-builder.md`). Read it first.

## Setup
- `git fetch --prune origin`. Branch from origin (you cannot checkout main):
  `git checkout -b feature/cc-triage-prompt origin/main`.
- Prefix every dotnet call: `PATH="$HOME/.dotnet:$PATH" dotnet …` (env doesn't persist). Confirm `dotnet --version`.

## Steps
1. Create `src/AmetekWatch.Core/Triage/TriageRubric.cs` and `src/AmetekWatch.Core/Triage/TriagePromptBuilder.cs`
   per spec Decision 1 — pure C#, no I/O, no new NuGet, no new project. The rubric system prompt must encode
   the charter weighting (general AMETEK awareness; **special weight on personal/social opinion pieces** and
   **reputable-institution financial reports**), define important/relevant/worth-reporting, and ask for a
   structured verdict (3 booleans + short rationale). `BuildUserContent(Finding)` is deterministic and labels
   Category/Title/Url/Snippet/PublishedAt (null-safe).
2. Add `tests/AmetekWatch.Tests/TriagePromptBuilderTests.cs` to the **existing** `AmetekWatch.Tests` project
   (do NOT edit its `.csproj` or the `.sln`). Tests per spec Decision 3, hand-computed; confirm one can fail
   then revert.
3. Do **not** call any API, add HTTP/Anthropic deps, modify `ITriageDecider`/`FakeTriageDecider`,
   `AmetekWatch.App`, the SQLite/Web projects, or the `.sln`.

## The gate
Each separately, real counts: `PATH="$HOME/.dotnet:$PATH" dotnet build -c Release` ; `… dotnet format --verify-no-changes` ; `… dotnet test`.

## Versioning ritual
Internal — no `CHANGELOG`/tag, `<Version>` stays `0.1.0`. Append a dated `CLAUDE.md` → `### Unreleased`
entry (below existing ones; don't edit them).

## Report-back format
Write `wiki/reports/report-spec011-CC-triage-prompt-builder.md` per `wiki/rituals/report-format.md` (all
sections; "None." where N/A), plus a gate table with real counts (test count before/after — the existing
suite grows), can-fail check, clean SHA, `dotnet --version`. Do **not** self-merge; push
`feature/cc-triage-prompt` and end your message with the tip SHA + a one-line build/format/test summary.
Never print or commit secrets.
