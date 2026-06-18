# Report — Spec 043-CC: Fix 042 HOLD on 041 (bless SweepHost runner seam + add 401 test)

**Headline outcome:** Both 042 HOLD blockers resolved. **Not merged** (no self-merge — CX2 integrates).
No version bump (`<Version>` stays `0.1.0`, internal). Branch `feature/cc-resilience-holdfix` pushed;
tip `f1cc30f6be70040ceae61122ee4468ffa0e74c57`. Gate green on .NET 8.0.422: build 0 warn, format clean,
test **118/118** (was 116; +2 new no-retry tests). No behaviour change; notifier impls, the `SweepRunner`
seam, and the `.sln` untouched.

## 1. Branch / merge state
- Base (HELD 041) SHA branched from: `cbe71709903fd957c69cbbc3c0658395e1ac50ae`
  (`origin/feature/cc-activate-resilience`) — **not** main, per spec.
- Feature branch: `feature/cc-resilience-holdfix`; working commit(s): `f1cc30f` (code + CLAUDE.md),
  plus this report commit; branch deleted post-merge: n (not merged).
- Post-merge `main` SHA (pushed): N/A — not merged (CX2 integrates).
- Merge mechanic: pushed branch; **cross-model integrator (CX2) merges**. No self-merge.

## 2. Changes
| File | Change |
|---|---|
| `src/AmetekWatch.App/SweepHost.cs` | One-line XML-doc on the optional `SweepRunner? runner = null` ctor param marking it the App-side composed-runner injection point, blessed by spec 043-CC. **No code/signature change** — the seam is kept exactly as 041 left it. |
| `tests/AmetekWatch.Anthropic.Tests/AnthropicTransientTests.cs` | Added `Unauthorized_401_IsNotTransient` and `Forbidden_403_IsNotTransient` — assert `AnthropicTransient.IsTransient(<401/403>) == false`, same SDK construction as the existing 400/404 tests. |
| `CLAUDE.md` | Appended a dated `### Unreleased` entry (below existing ones) noting this resolves the 042 HOLD. |

## 3. Blocker 1 — `SweepHost` runner seam (kept / blessed)
- The optional `SweepRunner? runner = null` ctor param on `SweepHost` (`src/AmetekWatch.App/SweepHost.cs:43`)
  is **kept as-is**, not reverted. When null it defaults to the prior `new SweepRunner(_searcher, _triage,
  _store)` (`src/AmetekWatch.App/SweepHost.cs:50`), so existing 3-/4-/5-arg construction and all prior tests
  are unchanged. `Program` passes the composer-built runner via `c.Runner`.
- 041's "no `SweepHost` seam change" wording was a **spec defect**: injecting a pre-built, retry/
  `OnlyReportNew`-configured runner necessarily requires a way in, and an optional, backward-compatible
  param (null → builds one as before) is the correct minimal design. Spec 043 corrects that constraint and
  blesses the seam. Added a one-line XML-doc note (`src/AmetekWatch.App/SweepHost.cs:35-36`) recording the
  blessing for future readers. **Only the doc text changed in `src/`.**

## 4. Blocker 2 — 401 (+403) no-retry test added
- Added `Unauthorized_401_IsNotTransient` (`tests/AmetekWatch.Anthropic.Tests/AnthropicTransientTests.cs`,
  new `[Fact]`) asserting `IsTransient(new AnthropicUnauthorizedException(Hre()){ StatusCode = 401, … }) ==
  false` — same construction shape as the existing 400 (`AnthropicBadRequestException`) and 404 cases.
- Added `Forbidden_403_IsNotTransient` for symmetry (cheap; same base `Anthropic4xxException` subtype
  `AnthropicForbiddenException`).
- The predicate (`src/AmetekWatch.Anthropic/AnthropicTransient.cs:61-65`) already returned false for these by
  logic (only 429 or `>= 500` retry); these tests **lock** the named no-retry guarantee. Both SDK type names
  (`AnthropicUnauthorizedException`, `AnthropicForbiddenException`) confirmed present in the `Anthropic`
  12.29.1 assembly.

## Gate results
| Gate | Result | Counts / note |
|---|---|---|
| `PATH="$HOME/.dotnet:$PATH" dotnet build -c Release` | ✓ | 0 warnings, 0 errors |
| `PATH="$HOME/.dotnet:$PATH" dotnet format --verify-no-changes` | ✓ | exit 0, no changes |
| `PATH="$HOME/.dotnet:$PATH" dotnet test` | ✓ | **118 passed**, 0 failed, 0 skipped (was 116) |
| Can-fail proof | ✓ | Flipped `Unauthorized_401_IsNotTransient` to `Assert.True` → 1 failed / 15 passed in that project; reverted → 16/16 green |

- Test count **before**: 116. **After**: 118. Breakdown after: `AmetekWatch.Anthropic.Tests` 44→**46**
  (+2 new), `AmetekWatch.Tests` 66, `AmetekWatch.Storage.Tests` 4, `AmetekWatch.Web.Tests` 2.
- Clean SHA at which the gate ran green: `f1cc30f6be70040ceae61122ee4468ffa0e74c57`.
- `dotnet --version`: `8.0.422`.
- Files changed NOT in the spec's files-to-change list: none beyond the spec's scope
  (`SweepHost.cs` doc, `AnthropicTransientTests.cs`, `CLAUDE.md`, this report).

## Sources beyond the brief / surprises
None. The 403 case was an explicit "if cheap" suggestion in the spec; it shares the 400/404/401 construction
shape, so it was free to add.

## Deferred / not done
- Integration / merge — deferred to CX2 (cross-model, author ≠ integrator), per spec. No self-merge.
- Live Anthropic / SMTP paths remain deferred (need a key/creds) — unchanged from 041, out of scope here.

## Standing flags
None new. The live-key verification of the real two-tier pipeline remains the only outstanding
product-level item, unchanged by this HOLD-fix.

## Roles update notice
None — no role doc edited this session.
