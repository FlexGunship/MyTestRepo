# Spec 019-CC — Anthropic triage decider adapter (Opus 4.8), offline-buildable

## Status
- Doc type: implementation (the real `ITriageDecider`, built+unit-tested offline; live call gated on a key)
- Executes: **CC**; pushes `feature/cc-anthropic-triage`; **CX** integrates (cross-model); CM lands on PASS. No self-merge.
- Number 019 verified free (highest spec file = 018; 019 = 018 + 1).
- Paired prompt: prompt-spec019-CC-anthropic-triage-adapter.md
- Final on-disk: new `src/AmetekWatch.Anthropic/` + `tests/AmetekWatch.Anthropic.Tests/` + `.sln` updated.

## Background
Everything is built behind seams except the real Anthropic calls (the deferred final step). This builds
the **real `ITriageDecider`** — Opus 4.8 judging a `Finding` against the 011 triage rubric with structured
output — in a way that **compiles and unit-tests fully offline**, deferring only the live network call to
when an `ANTHROPIC_API_KEY` exists. Design splits pure logic (testable) from the one untestable line.

## Decisions made
1. **New project `src/AmetekWatch.Anthropic`** (`net8.0`, references `AmetekWatch.Core`, NuGet **`Anthropic`**
   — the official .NET SDK). Add it + a test project `tests/AmetekWatch.Anthropic.Tests` to `AmetekWatch.sln`.
2. **`IMessagesClient` seam** (in this project): `Task<string> CreateMessageTextAsync(MessageCreateParams
   parameters, CancellationToken ct)` — returns the response's first text block. Two impls: a real
   `AnthropicMessagesClient` wrapping `AnthropicClient` (the **only** code not unit-tested offline — keep it
   ~5 lines), and a `FakeMessagesClient` for tests (returns canned JSON).
3. **`TriageRequestFactory` (pure)** — `Build(Finding) -> MessageCreateParams` using the **011** prompt
   logic and the SDK (verify exact bindings via the **`/claude-api`** skill and the installed SDK):
   - `Model = Model.ClaudeOpus4_8` (`claude-opus-4-8`).
   - `System` = `TriagePromptBuilder.BuildSystemPrompt()` as a **cached** block:
     `System = new List<TextBlockParam> { new() { Text = rubric, CacheControl = new CacheControlEphemeral() } }`
     (prompt-caches the stable rubric — the charter's cost lever).
   - `Messages = [ User: TriagePromptBuilder.BuildUserContent(finding) ]`.
   - **Structured output** — `OutputConfig = new OutputConfig { Format = new JsonOutputFormat { Schema = … } }`
     with a JSON schema `{ important:boolean, relevant:boolean, worthReporting:boolean, rationale:string }`,
     all `required`, `additionalProperties:false`.
   - `MaxTokens` ~1024. (Adaptive thinking optional; if used, `Thinking = new ThinkingConfigAdaptive()`.)
   - **Pure** — builds and returns the params; **no** API call.
4. **`TriageVerdictParser` (pure)** — `Parse(string json) -> TriageVerdict` mapping the structured JSON to
   the 4 fields; throws a clear exception on malformed JSON.
5. **`AnthropicTriageDecider : ITriageDecider`** — ctor takes `IMessagesClient` + the factory + parser.
   `JudgeAsync(Finding, ct)` = `factory.Build` → `client.CreateMessageTextAsync` → `parser.Parse`. Fully
   unit-testable via `FakeMessagesClient`.
6. **Tests** (`tests/AmetekWatch.Anthropic.Tests`, own project): factory asserts model id, the system block
   carries the rubric text **and** cache control, the schema has the 4 fields, user content ==
   `BuildUserContent`; parser maps a known JSON to the expected `TriageVerdict` and throws on garbage;
   `AnthropicTriageDecider` with a `FakeMessagesClient` returning canned JSON yields the expected verdict.
   Hand-computed; confirm a test can fail then revert.

## Out of scope
- **No live API call in the gate** (no key; the real `AnthropicMessagesClient` is not unit-tested — note this
  in the report). The Anthropic **searcher** adapter (later spec). Wiring App/DI to select the real decider
  (later small spec). Prompt-cache *verification* (needs live `usage`).

## Definition of done
- [ ] `AmetekWatch.Anthropic` (factory + parser + `IMessagesClient` + real & fake impls + `AnthropicTriageDecider`)
      and its test project, both added to `.sln`; NuGet `Anthropic` restores.
- [ ] Tests green for factory/parser/decider (via fake); can-fail confirmed. The real client wrapper compiles
      (not unit-tested — documented).
- [ ] Gate green (build/format/test, each separately, real counts). Branch pushed; tip SHA reported.

## Deliverable / report-back
See the prompt file.
