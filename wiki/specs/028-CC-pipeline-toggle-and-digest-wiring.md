# Spec 028-CC — App: real-vs-fake pipeline toggle + digest wiring (capstone)

## Status
- Doc type: implementation (the capstone — makes the real pipeline config-selectable and emits the digest)
- Executes: **CC**; pushes `feature/cc-pipeline-toggle`; **CX** integrates (cross-model); CM lands on PASS. No self-merge.
- **Depends on:** 024 (`AnthropicSearcher`) + 019 (`AnthropicTriageDecider`) + 025 (`FileDigestNotifier`) all on `main`. Do **not** dispatch until they are.
- Number 028 verified free (search `wiki/specs/`; this is highest + 1).
- Paired prompt: prompt-spec028-CC-pipeline-toggle-and-digest-wiring.md
- Final on-disk: `src/AmetekWatch.App/` (Program/SweepHost/options/appsettings) + a test in `tests/AmetekWatch.Tests/`. App references `AmetekWatch.Anthropic`.

## Background
All pieces exist behind seams: fake + **real** `ISearcher`/`ITriageDecider` (the Anthropic adapters), SQLite
store, sweep host, dashboard, digest sink. This capstone makes the App **select real-vs-fake by config** and
**emit the digest** after each sweep — so AMETEK Watch is a genuine end-to-end product that runs the real
Sonnet→Opus pipeline the moment `ANTHROPIC_API_KEY` is present, and falls back to the fakes otherwise.

## Decisions made
1. **App references `AmetekWatch.Anthropic`** (`dotnet add … reference …` — touches `AmetekWatch.App.csproj`,
   not the `.sln`).
2. **Config** (`appsettings.json` + `SweepOptions` or a new `PipelineOptions`): a `Pipeline` section with
   `UseRealApi` (bool, **default false**), and a `Notify` section with `DigestPath` (string, optional/empty).
3. **DI / construction in `Program`:**
   - If `UseRealApi == true`: construct `AnthropicMessagesClient` (the real `IMessagesClient`, key from env)
     and from it `AnthropicSearcher` (with an injected clock — `() => DateTimeOffset.UtcNow`) +
     `AnthropicTriageDecider`. **If `ANTHROPIC_API_KEY` is not set, print a clear one-line warning and fall
     back to the fakes** (so the exe still runs and demonstrates) — do not crash, do not silently pretend it's
     real. Log which pipeline (real/fake) is active.
   - Else: `FakeSearcher` + `FakeTriageDecider` (current behaviour).
   - Inject the chosen `ISearcher`/`ITriageDecider` into `SweepHost` (unchanged seam).
4. **Digest wiring:** after the sweep, call a configured `IDigestNotifier` with the worth-reporting digest —
   `FileDigestNotifier(DigestPath, () => DateTimeOffset.UtcNow)` when `DigestPath` is set, else
   `NullDigestNotifier`. Wire this in `SweepHost.RunOnceAsync` (or `Program` right after it) so a real run
   writes the digest file. Keep `RunOnce` the default so the CLI terminates.
5. **Tests** (`tests/AmetekWatch.Tests/`): with `UseRealApi=false` + a temp DB + a temp `DigestPath`, one
   sweep persists to SQLite **and** writes the expected digest file (assert the file exists + a line of
   content). A pure selection test: a small factory/helper resolves the **real** Anthropic types when
   `UseRealApi=true` and the **fakes** when false — assert the resolved runtime types **without invoking
   them** (no network; do not require a key). Hand-computed; confirm a test can fail then revert.

## Out of scope
- Live API calls / live smoke test (needs a key — the final manual step). The live server-tool `pause_turn`
  loop hardening (024's noted follow-up). Email/SMTP. Scheduling/Windows packaging.

## Definition of done
- [ ] Config toggle (`UseRealApi`, `DigestPath`); `Program` selects real-vs-fake (env-key fallback w/ warning);
      digest written after a sweep.
- [ ] `dotnet run --project src/AmetekWatch.App` (default fakes) runs one sweep, persists to SQLite, writes the
      digest file, prints the digest, exit 0.
- [ ] Tests (selection + digest-written); can-fail confirmed.
- [ ] Gate green (build/format/test, each separately, real counts). Branch pushed; tip SHA reported.

## Deliverable / report-back
See the prompt file.
