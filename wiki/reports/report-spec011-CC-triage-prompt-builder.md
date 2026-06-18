# Report — Spec 011-CC: Triage prompt & rubric builder

**Headline outcome:** Pure-C# triage system-prompt rubric and per-finding prompt builder added behind
`src/AmetekWatch.Core/Triage/`. **Not merged** (CC does not self-merge; awaiting cross-model
integration). No version bump (`<Version>` stays `0.1.0`, internal). Branch `feature/cc-triage-prompt`
pushed; gate green on Linux .NET 8.0.422 — build (0 warn) / format / test (18/18).

> **Note on missing spec files.** `wiki/specs/011-CC-triage-prompt-builder.md` and
> `wiki/specs/prompt-spec011-CC-triage-prompt-builder.md` do **not** exist in the repo (the specs dir
> tops out at 008). I executed strictly from the dispatch prompt's inline "Key points," which were
> self-contained. Flagged here so the Manager can author the spec retroactively or confirm intent.

## 1. Branch / merge state
- Pre-branch `origin/main` SHA: `b83c359d8cef24d8badb992eb5fb128fa023c37e` (branch point).
- Feature branch: `feature/cc-triage-prompt` (branched from `origin/main`); working commit: see tip
  SHA in the final message. Branch deleted post-merge: **n** (integrator's call).
- Post-merge `main` SHA (pushed): **N/A** — not merged. Independent integrator merges (author ≠
  integrator); CM lands on PASS.
- Merge mechanic: pushed branch, integrator merges (`--no-ff`). No self-merge.

## 2. Changes
| File | Change |
| --- | --- |
| `src/AmetekWatch.Core/Triage/TriageRubric.cs` | New. Static class; `const string SystemPrompt` — the triage system-prompt rubric. Pure constant, no I/O. |
| `src/AmetekWatch.Core/Triage/TriagePromptBuilder.cs` | New. Static class; `BuildSystemPrompt()` → rubric; `BuildUserContent(Finding)` → deterministic labelled user message, null-safe. Pure, no I/O. |
| `tests/AmetekWatch.Tests/TriagePromptBuilderTests.cs` | New. 7 tests appended to the **existing** test project (its `.csproj` untouched). Hand-computed oracles. |
| `CLAUDE.md` | Appended a dated `### Unreleased` entry below existing entries (none edited). |
| `wiki/reports/report-spec011-CC-triage-prompt-builder.md` | This report. |

## 3. Implementation notes
- **Rubric content (`TriageRubric.SystemPrompt`).** A single triple-quoted string covering, in order:
  general AMETEK, Inc. (NYSE: AME) awareness with a subject-awareness guard against namesakes/passing
  mentions; a **"# Special weight"** section that names the two privileged buckets verbatim —
  *personal and social opinion pieces* and *reputable-institution financial reports* — and says they
  should clear the bar more readily; a **"# The three dimensions"** section defining **Important**,
  **Relevant**, and **WorthReporting** (with the constraint that worth-reporting implies important ∧
  relevant); and a **"# Your verdict"** section requesting the structured verdict = exactly 3 booleans
  (`important`, `relevant`, `worthReporting`) + a short `rationale`. These mirror the existing
  `TriageVerdict` record's four fields without importing or modifying it.
- **`BuildSystemPrompt()`** simply returns `TriageRubric.SystemPrompt` (the builder is the public seam;
  the rubric is the data).
- **`BuildUserContent(Finding)`** renders one labelled line per field — `Category` (enum value),
  `Title`, `Url`, `Snippet`, `PublishedAt` — after a fixed instruction line. Deterministic: a
  `StringBuilder` with literal `'\n'` separators (no environment newline, no culture-sensitive
  formatting beyond invariant), so identical inputs yield byte-identical output.
- **Null-safety.** `ArgumentNullException.ThrowIfNull(finding)` guards the argument. The optional
  `Finding.PublishedAt` (`DateTimeOffset?`) renders via `ToString("O", InvariantCulture)` when present
  and a `(unknown)` sentinel when null — chosen over a blank so the model sees an explicit "unknown"
  rather than a line it could misread.
- **Scope discipline.** No API call, no Anthropic/HTTP dependency, no new NuGet, no new project.
  `ITriageDecider`/`FakeTriageDecider`, `AmetekWatch.App`, the SQLite/Web projects, the `.sln`, and
  the test project's `.csproj` were not touched (verified via `git diff --stat`).

## Gate results
| Command | Result | Counts |
| --- | --- | --- |
| `dotnet build -c Release` | ✓ | 0 warnings, 0 errors (5 projects) |
| `dotnet format --verify-no-changes` | ✓ | exit 0, no changes |
| `dotnet test` | ✓ | Failed 0, Passed 18, Skipped 0 |

- Test count **before**: 11 (7 slice in `AmetekWatch.Tests` + 4 storage). **After**: 18 (14 in
  `AmetekWatch.Tests` = 7 slice + 7 new triage; + 4 storage). The 7 new are all in
  `TriagePromptBuilderTests`; existing suites unchanged.
- **Can-fail check:** flipped the null-`PublishedAt` assertion in
  `BuildUserContent_NullPublishedAt_RendersUnknownAndNeverThrows` to expect `"PublishedAt: NOPE-CANFAIL"`
  → test **failed** as expected (1 failed / 6 passed in the triage suite); reverted; triage suite green
  again (7/7), full suite 18/18.
- Gate ran clean at the pushed tip SHA (reported in the final message).
- `dotnet --version`: **8.0.422**. Every `dotnet` call prefixed with `PATH="$HOME/.dotnet:$PATH"`.
- Files changed NOT in a files-to-change list: `CLAUDE.md` (mandated versioning ritual) and this
  report. No product/source files outside the new `Triage/` folder + the appended test file.

## Sources beyond the brief / surprises
- **Spec files absent** (see top note) — executed from the dispatch prompt's inline key points.
- The verdict field names in the rubric were aligned to the **existing** `TriageVerdict` record
  (`Important` / `Relevant` / `WorthReporting` / `Rationale`) so a future wiring spec can map the
  model's structured output onto it without renaming. `TriageVerdict` was read for reference only, not
  modified.

## Deferred / not done
- **No `ITriageDecider` implementation.** This spec produces only the prompt/rubric text; wiring an
  Opus-backed `ITriageDecider` that actually sends `BuildSystemPrompt()` + `BuildUserContent(...)` to
  the API is a later spec (Anthropic auth still deferred project-wide).
- **No parsing of a model response** into `TriageVerdict` — out of scope here; the rubric only
  *requests* the structured shape.
- **Windows runtime verification** — N/A; pure managed code, no native/interop surface added.

## Standing flags
- Anthropic API auth remains deferred project-wide (pre-existing, untouched here).
- The `wiki/specs/011-CC-*` spec files do not exist; recommend the Manager author/backfill them.

## Roles update notice
- None. No role doc edited this session.
