# Prompt — Spec 019-CC — Anthropic triage decider adapter (Opus 4.8)

You are **CC**. Execute Spec 019-CC (`wiki/specs/019-CC-anthropic-triage-adapter.md`). Read it first.

**SDK grounding:** this uses the official Anthropic .NET SDK. **Invoke the `/claude-api` skill** (or read it)
for the exact C# bindings — do NOT guess SDK type/method names. The spec lists the bindings I'm confident
about (`AnthropicClient`, `MessageCreateParams { Model = Model.ClaudeOpus4_8, … }`, `OutputConfig.Format =
new JsonOutputFormat { Schema = … }`, `System = List<TextBlockParam>` with `CacheControlEphemeral`); confirm
the precise signatures against the installed SDK and adapt if they differ. Model id is `claude-opus-4-8`.

## Setup
- `git fetch --prune origin`; branch from origin: `git checkout -b feature/cc-anthropic-triage origin/main`.
- Prefix every dotnet call: `PATH="$HOME/.dotnet:$PATH" dotnet …`. Confirm `dotnet --version`.

## Steps
1. `dotnet new classlib -n AmetekWatch.Anthropic -o src/AmetekWatch.Anthropic`; reference Core; `dotnet add …
   package Anthropic` (let it resolve the current version; nuget.org is reachable). `dotnet sln add` it.
2. Implement per spec Decisions 2–5: the `IMessagesClient` seam (real `AnthropicMessagesClient` wrapping
   `AnthropicClient` — keep it minimal; reads `ANTHROPIC_API_KEY` from env; this is the only line not
   unit-tested), `TriageRequestFactory` (pure), `TriageVerdictParser` (pure), `AnthropicTriageDecider :
   ITriageDecider`.
3. `dotnet new xunit -n AmetekWatch.Anthropic.Tests -o tests/AmetekWatch.Anthropic.Tests`; reference the
   Anthropic project (+ Core); `dotnet sln add`. Write the tests from spec Decision 6 with a
   `FakeMessagesClient` — **no network**. Hand-computed; confirm a test can fail then revert.
4. Do **not** make any live API call, hardcode a key, or touch other projects' source / the sweep host.

## The gate
Each separately, real counts: `PATH="$HOME/.dotnet:$PATH" dotnet build -c Release` ; `… dotnet format --verify-no-changes` ; `… dotnet test`.

## Versioning ritual
Internal — no `CHANGELOG`/tag, `<Version>` stays `0.1.0`. Append a dated `CLAUDE.md` → `### Unreleased`
entry (below existing ones; don't edit them).

## Report-back format
Write `wiki/reports/report-spec019-CC-anthropic-triage-adapter.md` per `wiki/rituals/report-format.md` (all
sections; "None." where N/A), plus: the resolved `Anthropic` NuGet version; a gate table (real counts, test
count before/after, can-fail, clean SHA, `dotnet --version`); and an explicit note that **the live API path
is not exercised** (no key) — the real `AnthropicMessagesClient` compiles but is not unit-tested. Do **not**
self-merge; push `feature/cc-anthropic-triage` and end with the tip SHA + a one-line build/format/test
summary. Never print or commit secrets (especially no API key).
