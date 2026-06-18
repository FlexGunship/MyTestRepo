# Prompt — Spec 043-CC — Fix 042 HOLD (bless SweepHost runner seam + add 401 test)

You are **CC**. Execute Spec 043-CC (`wiki/specs/043-CC-resilience-holdfix.md`). Read it first, plus the HOLD
review `wiki/reviews/review-spec042-CX-activate-resilience.md`. This **continues your 041 branch**.

## Setup
- `git fetch --prune origin`; branch from your HELD 041 branch (NOT main):
  `git checkout -b feature/cc-resilience-holdfix origin/feature/cc-activate-resilience`.
- Prefix every dotnet call: `PATH="$HOME/.dotnet:$PATH" dotnet …`. Confirm `dotnet --version`.

## Steps
1. **Keep** the optional `SweepRunner? runner = null` param on `SweepHost` (it's blessed now — the App-side
   composed-runner injection point; backward-compatible). Optionally add a one-line XML-doc saying so. Do
   **not** revert it.
2. Add to `tests/AmetekWatch.Anthropic.Tests/AnthropicTransientTests.cs` an explicit **401 Unauthorized**
   case asserting `AnthropicTransient.IsTransient(<the 401 exception>) == false` (and, if cheap, a **403**
   case too). Use the same SDK exception construction the existing 400/404 tests use.
3. No other behaviour change. Don't touch the notifier impls, the `SweepRunner` seam, or the `.sln`.
4. Confirm a test can fail then revert (e.g. briefly flip the new 401 assertion).

## The gate
Each separately, real counts: `PATH="$HOME/.dotnet:$PATH" dotnet build -c Release` ; `… dotnet format --verify-no-changes` ; `… dotnet test` (expect 116 + the new test(s)).

## Versioning ritual
Internal — no `CHANGELOG`/tag, `<Version>` stays `0.1.0`. Append a dated `CLAUDE.md` → `### Unreleased`
entry (below existing ones; don't edit them) noting this resolves the 042 HOLD.

## Report-back format
Write `wiki/reports/report-spec043-CC-resilience-holdfix.md` per `wiki/rituals/report-format.md` (all sections;
"None." where N/A), explicitly noting: the 401(+403) test added, the `SweepHost` runner seam kept (blessed),
and a gate table (real counts, before/after, can-fail, clean SHA, `dotnet --version`). Do **not** self-merge;
push `feature/cc-resilience-holdfix` and end with the tip SHA + a one-line build/format/test summary. Never print or commit secrets.
