# Report — Spec 019-CC: Anthropic triage decider adapter (Opus 4.8)

**Headline outcome:** Built the real `ITriageDecider` (Opus 4.8 + structured output) as a new
`AmetekWatch.Anthropic` class library, **fully unit-tested offline** behind an `IMessagesClient`
seam, with only the ~5-line live-SDK wrapper left untested. **Not merged** (no self-merge): pushed
`feature/cc-anthropic-triage` for CX to integrate cross-model. No version bump (`<Version>` stays
`0.1.0`). Gate green — build (0 warn) / format / test (48/48, +13). The live API path is **not
exercised** (no key).

## 1. Branch / merge state
- Pre-merge `main` SHA: `f0677178aaaab4139554f74faa84212f52f23178`
- Feature branch: `feature/cc-anthropic-triage` (branched from `origin/main`); working commit: see
  the one-line summary at the end of this report (tip SHA).
- Post-merge `main` SHA (pushed): N/A — not merged (author ≠ integrator; CX integrates).
- Merge mechanic: pushed branch; integrator (CX) merges.

## 2. Changes
| File | Change |
|---|---|
| `src/AmetekWatch.Anthropic/AmetekWatch.Anthropic.csproj` | New classlib (`net8.0`); refs `AmetekWatch.Core`; NuGet `Anthropic` 12.29.1. |
| `src/AmetekWatch.Anthropic/IMessagesClient.cs` | Seam: `Task<string> CreateMessageTextAsync(MessageCreateParams, ct)` → first text block. |
| `src/AmetekWatch.Anthropic/AnthropicMessagesClient.cs` | Real impl wrapping the SDK `AnthropicClient` (reads `ANTHROPIC_API_KEY` from env). **Only code not unit-tested.** |
| `src/AmetekWatch.Anthropic/TriageRequestFactory.cs` | Pure `Build(Finding)` → `MessageCreateParams` (model id, cached rubric system block, user message, structured-output schema, `MaxTokens`). |
| `src/AmetekWatch.Anthropic/TriageVerdictParser.cs` | Pure `Parse(string)` → `TriageVerdict`; throws `FormatException` on bad JSON. |
| `src/AmetekWatch.Anthropic/AnthropicTriageDecider.cs` | `ITriageDecider`; ctor `IMessagesClient` + factory + parser; `JudgeAsync` = build → call → parse. |
| `tests/AmetekWatch.Anthropic.Tests/AmetekWatch.Anthropic.Tests.csproj` | New xUnit project; refs the Anthropic project + Core. |
| `tests/AmetekWatch.Anthropic.Tests/FakeMessagesClient.cs` | Test double: canned JSON, records last request. |
| `tests/AmetekWatch.Anthropic.Tests/TriageRequestFactoryTests.cs` | 5 tests (model id, cached rubric system block, schema fields, user content, null guard). |
| `tests/AmetekWatch.Anthropic.Tests/TriageVerdictParserTests.cs` | 5 tests (known JSON, garbage, missing field, wrong type, null guard). |
| `tests/AmetekWatch.Anthropic.Tests/AnthropicTriageDeciderTests.cs` | 3 tests (canned JSON → verdict, sends Opus-4.8 request, null guard). |
| `AmetekWatch.sln` | Both new projects added. |
| `CLAUDE.md` | Dated `### Unreleased` entry appended (below existing). |
| `wiki/reports/report-spec019-CC-anthropic-triage-adapter.md` | This report. |

## 3. Spec-specific details

**Resolved `Anthropic` NuGet version:** **12.29.1** (`dotnet add package Anthropic`, let it resolve
the current version; nuget.org reachable).

**The live API path is NOT exercised.** No `ANTHROPIC_API_KEY` is used, read in tests, or committed.
`AnthropicMessagesClient` compiles (verified by the Release build) but is **not unit-tested** — it is
the single class that requires a live key. Everything else (factory, parser, decider) is driven
offline through `FakeMessagesClient`.

**SDK bindings (verified against the installed SDK, not guessed).** Invoked the `/claude-api` skill
for the C# bindings, then reflected the installed `Anthropic.dll` to confirm exact member shapes
before writing/asserting:
- `client.Messages.Create(MessageCreateParams, CancellationToken)` → `Task<Message>`;
  `Message.Content` is `IReadOnlyList<ContentBlock>`; `ContentBlock.TryPickText(out TextBlock?)`.
  Wrapper reads the first text block via `.Select(b => b.Value).OfType<TextBlock>().First().Text`.
- `MessageCreateParams.System` is the `MessageCreateParamsSystem` union; assigned a
  `List<TextBlockParam>` (the array-literal implicit conversion does **not** apply — concrete
  `List<>` required); read back in tests via `System.TryPickTextBlockParams(out var blocks)`.
- `TextBlockParam.CacheControl` is `CacheControlEphemeral` (a typed property, not a union).
- `OutputConfig.Format` is `JsonOutputFormat`; `JsonOutputFormat.Schema` is
  `IReadOnlyDictionary<string, JsonElement>` (built with `JsonSerializer.SerializeToElement`).
- `MessageParam.Content` is the `MessageParamContent` union; read back via `TryPickString`.
- The model-id assertion uses `request.Model.ToString()`, which serializes to `"claude-opus-4-8"`.

**Judgment call — decider ctor takes the factory and parser as injected instances.** Spec Decision 5
says "ctor takes `IMessagesClient` + the factory + parser." To honor that literally, `TriageRequestFactory`
and `TriageVerdictParser` are **instance** classes (still pure — no I/O, no state) injected into the
decider, rather than `static` helpers in the style of `TriagePromptBuilder`. This matches the spec
text and keeps each collaborator independently testable; flagged here because it diverges from the
Core convention of `static` pure builders.

**Schema shape:** `{ "type":"object", "properties": { important, relevant, worthReporting : {boolean},
rationale : {string} }, "required": [all four], "additionalProperties": false }`.

## Gate results
Run on Linux, .NET SDK **8.0.422**, each command separately, `PATH="$HOME/.dotnet:$PATH"`:

| Gate command | Result |
|---|---|
| `dotnet build -c Release` | ✓ Build succeeded — **0 warnings**, 0 errors |
| `dotnet format --verify-no-changes` | ✓ Clean (exit 0) |
| `dotnet test` | ✓ **48/48 passed**, 0 failed, 0 skipped |

- Test count **before**: 35 (29 `AmetekWatch.Tests` + 4 Storage + 2 Web).
- Test count **after**: 48 (35 prior + **13** new `AmetekWatch.Anthropic.Tests`).
- Per-suite after: Anthropic 13, Core/`AmetekWatch.Tests` 29, Storage 4, Web 2.
- **Can-fail confirmed:** flipped the decider's `WorthReporting` oracle `true→false` → 1 failure
  (12/13); reverted → 13/13 green.
- SHA at which the gate ran clean: the branch tip (see final line).
- **Files changed NOT in the spec's files-to-change list:** none beyond what the spec named — the
  two new projects, the `.sln`, `CLAUDE.md`, and this report. No other project's source touched; the
  sweep host (015) untouched.

## Sources beyond the brief / surprises
- The SDK's `System`, `OutputConfig`, and `OutputConfig.Format` properties are **nullable reference
  types**; the test assertions needed `!` (null-forgiving) to satisfy `TreatWarningsAsErrors`. Not a
  behavioral issue — only nullable-annotation plumbing in the test that unwraps the built params.
- The SDK exposes a type `Anthropic.Models.Messages.Type` that collides with `System.Type`; irrelevant
  to the shipped code (encountered only in the throwaway reflection probe, since deleted).
- `AnthropicClient`'s default ctor reads `ANTHROPIC_API_KEY` from the environment (no explicit key
  plumbing needed) — used the parameterless ctor to keep the wrapper minimal.

## Deferred / not done
- **Live API call / `AnthropicMessagesClient` runtime behavior** — deferred by design (no key). The
  wrapper compiles but is not exercised end-to-end against the real API.
- **Wiring App/DI to select the real decider** — out of scope (later small spec).
- **Prompt-cache verification** — needs live `usage` fields; deferred (later, with a key).
- **Windows runtime** — not built/run here (the spec adds no host; the existing publish path is
  unaffected).

## Standing flags
None.

## Roles update notice
No role docs edited this session.
