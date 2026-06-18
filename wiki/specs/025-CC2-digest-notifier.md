# Spec 025-CC2 — Digest notifier seam + file sink

## Status
- Doc type: implementation (the pluggable digest-delivery seam the charter wanted; file sink now, email later)
- Executes: **CC2**; pushes `feature/cc2-digest-notifier`; **CX2** integrates (cross-model); CM lands on PASS. No self-merge.
- Number 025 verified free (search `wiki/specs/` for the highest; this is highest + 1).
- Paired prompt: prompt-spec025-CC2-digest-notifier.md
- Final on-disk: new files under `src/AmetekWatch.Core/Notify/` + a test file in `tests/AmetekWatch.Tests/` (no `.sln` change).

## Background
The charter wants the digest deliverable to a **pluggable sink** with **email hooks left open**. This adds
the seam + a concrete **file** sink (writes a friendly Markdown digest), keeping email a later drop-in.
Independent of the Anthropic adapters; pure/local; testable via a temp directory.

## Decisions made
1. **New folder `src/AmetekWatch.Core/Notify/`** (no new project / NuGet):
   - `IDigestNotifier` — `Task NotifyAsync(IReadOnlyList<TriagedFinding> digest, CancellationToken ct)`.
   - `FileDigestNotifier` — ctor takes an output file path; `NotifyAsync` writes a **friendly Markdown
     digest**: a heading with the run subject/date, the worth-reporting count, then per finding its
     **Category, Title, Url, and the decider's rationale** (use **friendly names** — no internal
     type/field names in the rendered text). Overwrites the file each run. Inject the timestamp (a
     `DateTimeOffset` parameter or provider) — **no `DateTimeOffset.Now` inside** (keep it testable).
   - `NullDigestNotifier` — a no-op (the default when no sink is configured).
2. **No App/DI wiring here** (calling it from the sweep host is a later spec, bundled with the real-vs-fake
   toggle) — keep this confined to the seam + impls + tests to stay conflict-free.
3. **Tests** — add `tests/AmetekWatch.Tests/DigestNotifierTests.cs`: `FileDigestNotifier` writes the expected
   Markdown for a known digest to a **temp file** (assert the rendered content — headings, count, each
   finding's friendly fields + rationale, and that an empty digest renders a clean "nothing to report"
   form); `NullDigestNotifier` writes nothing. Hand-computed; confirm a test can fail then revert.

## Out of scope
- Email/SMTP (a later drop-in impl). Wiring into `SweepHost`/App (later spec). DB sink.

## Definition of done
- [ ] `IDigestNotifier` + `FileDigestNotifier` + `NullDigestNotifier` in `Core/Notify/` (injected timestamp; friendly names).
- [ ] `tests/AmetekWatch.Tests/DigestNotifierTests.cs` (temp file; seeded + empty cases; can-fail confirmed).
- [ ] Gate green (build/format/test, each separately, real counts). Branch pushed; tip SHA reported.

## Deliverable / report-back
See the prompt file.
