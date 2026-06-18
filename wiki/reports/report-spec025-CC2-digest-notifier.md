# Report — Spec 025-CC2: Digest notifier seam + file sink

**Headline outcome:** Built the pluggable digest-delivery seam (`IDigestNotifier`) plus a concrete
friendly-Markdown **file** sink (`FileDigestNotifier`) and a no-op (`NullDigestNotifier`) under
`src/AmetekWatch.Core/Notify/`, with a temp-file test suite. **Not merged** (no self-merge); branch
`feature/cc2-digest-notifier` pushed for CX2 to integrate. No version bump (`<Version>` stays `0.1.0`).
Gate green on Linux .NET 8.0.422 — build 0 warn / format clean / test 56/56 (was 52).

## 1. Branch / merge state
- Pre-merge `main` SHA (branch base): `a265788861821e7e777c894d309c52409a11ae2d`
- Feature branch: `feature/cc2-digest-notifier`; working commit: see tip SHA below; branch deleted post-merge: n
- Post-merge `main` SHA (pushed): N/A — not merged (CX2 integrates).
- Merge mechanic: pushed branch; cross-model integrator (CX2) merges. No self-merge.

## 2. Changes
| File | Change |
|------|--------|
| `src/AmetekWatch.Core/Notify/IDigestNotifier.cs` | NEW — delivery seam: `Task NotifyAsync(IReadOnlyList<TriagedFinding> digest, CancellationToken ct)`. |
| `src/AmetekWatch.Core/Notify/FileDigestNotifier.cs` | NEW — friendly-Markdown file sink; ctor (output path, subject, `Func<DateTimeOffset>` provider); overwrites each run; injected UTC-normalised timestamp; no ambient clock. |
| `src/AmetekWatch.Core/Notify/NullDigestNotifier.cs` | NEW — no-op default when no sink is configured. |
| `tests/AmetekWatch.Tests/DigestNotifierTests.cs` | NEW — 4 hand-computed tests (seeded digest, empty case, overwrite, null-writes-nothing) over a temp file. |
| `CLAUDE.md` | Appended one dated `### Unreleased` entry (existing entries untouched). |

No `.csproj`, `.sln`, App/DI, SweepHost, Web, Storage, or Anthropic files were touched.

## 3. Spec-specific notes
- **Seam shape.** `NotifyAsync` takes only `(digest, ct)` per spec, so the run **subject** and **timestamp**
  are injected via the `FileDigestNotifier` constructor. The timestamp is a `Func<DateTimeOffset>` **provider**
  (the spec's "parameter or provider"); there is **no `DateTimeOffset.Now`** anywhere in `Notify/`.
- **Friendly rendering** (`FileDigestNotifier.Render`, internal & pure): heading `# <subject> Watch digest`,
  a `_Generated <weekday, dd Month yyyy HH:mm UTC>_` line, a bold worth-reporting count, then one `##` section
  per finding with the **friendly kind** ("Opinion / Social" / "Financial Report" / "Other"), title,
  `- Link: <url>`, and `- Why it matters: <rationale>`. No internal enum/type/property identifiers appear in
  the output — a test asserts the absence of `OpinionSocial`, `FinancialReport`, `WorthReporting`, `Rationale`,
  `DiscoveredAt`, `TriagedFinding`, `Verdict`, `Url:`, `Category`.
- **Empty digest** renders a clean `Nothing to report this run.` form (heading + generated line + the notice).
- **Overwrite** — `File.WriteAllTextAsync` replaces the file each run; a test proves a stale item does not linger.
- **UTC normalisation** — the seeded test stamps `16:30 +02:00` and asserts the heading reads `14:30 UTC`,
  proving `ToUniversalTime()` is applied (output is deterministic regardless of the stamp's offset).
- **Date hand-computation** — `2026-06-18` is a Thursday (2026-01-01 is a Thursday; day-of-year 169 ⇒ +168 days
  ⇒ 168 mod 7 = 0 ⇒ same weekday), so the expected heading reads `Thursday, 18 June 2026 14:30 UTC`.

## Gate results
Run on Linux, `dotnet --version` = **8.0.422**. Each command run **separately**. Clean SHA = branch tip below.

| Gate command | Result | Notes |
|---|---|---|
| `PATH="$HOME/.dotnet:$PATH" dotnet build -c Release` | ✓ | 0 Warning(s), 0 Error(s) |
| `PATH="$HOME/.dotnet:$PATH" dotnet format --verify-no-changes` | ✓ | clean (no output) |
| `PATH="$HOME/.dotnet:$PATH" dotnet test` | ✓ | **56/56** passed |

- Test count **before**: 52 (AmetekWatch.Tests 33 + Storage 4 + Web 2 + Anthropic 13).
- Test count **after**: 56 (AmetekWatch.Tests **37** + Storage 4 + Web 2 + Anthropic 13) — **+4** digest tests.
- **Can-fail confirmed:** flipped the seeded oracle's count (`**2 items…` → `**3 items…`) ⇒ exactly 1 failure
  with the expected/actual diff at the count token; reverted; suite green again.
- Files changed NOT in the spec's list: **None.** (Spec named `Core/Notify/` + a test file; `CLAUDE.md` is the
  required versioning-ritual append.)

## Sources beyond the brief / surprises
- The spec/prompt files `wiki/specs/025-CC2-digest-notifier.md` and `prompt-spec025-...` did **not** exist in
  this worktree's checked-out branch; they live on `origin/main` (commit `a265788`). Read them from
  `origin/main` and branched from `origin/main` as instructed — no impact on the work.
- `CLAUDE.md` carries **both** the 019 (48/48) and 020 (39/39) entries, authored from independent branch
  baselines; the actual `origin/main` runner baseline is the union = **52** tests. Reported counts are from
  real runner output, not the changelog arithmetic.

## Deferred / not done
- App/DI/`SweepHost` wiring, email/SMTP sink, and a DB sink — all **out of scope** here, deferred to a later
  wiring spec (bundled with the real-vs-fake toggle), exactly as the spec directs.
- Windows runtime verification of the publish artifact — not part of this gate (unchanged from prior specs).

## Standing flags
None.

## Roles update notice
No role docs edited this session.
