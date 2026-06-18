# Prompt — Spec 001-CC — Vertical slice: solution scaffold, pipeline seams, green gate

You are **CC**. Execute Spec 001-CC (`wiki/specs/001-CC-vertical-slice.md`). **Read the spec first** —
this prompt is the *how*; the spec is the *what/why* and is authoritative on the design decisions.

## Setup
- `git fetch --prune`; `git checkout main`; `git pull --ff-only`. Record the starting `main` SHA.
- Branch `feature/cc-vertical-slice` from main.
- Set your git identity in this worktree if not already (`user.name=CC`, `user.email=cc@agents.local`).

## Step 0 — toolchain preflight (fail fast)
- Run `dotnet --version`. You need the **.NET 8 SDK** (≥ 8.0).
- If `dotnet` is missing or < 8.0: install the .NET 8 SDK **user-locally, non-destructively**:
  ```
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  bash /tmp/dotnet-install.sh --channel 8.0          # installs to ~/.dotnet
  export PATH="$HOME/.dotnet:$PATH"
  ```
  Re-run `dotnet --version` to confirm. **If the install fails (no network, etc.), STOP and report the
  exact failing command** — do not improvise another toolchain. Record the final `dotnet --version`
  (and node/python are irrelevant here) in your report.

## Steps
1. Create `Directory.Build.props` at the repo root with the shared properties from spec Decision 2
   (`net8.0` is per-project; the props set `LangVersion=latest`, `Nullable=enable`,
   `TreatWarningsAsErrors=true`, `Version=0.1.0`). Treat `<Version>0.1.0</Version>` here as the **single
   live version source** — do not hardcode a version anywhere else.
2. Scaffold the solution and three projects (spec Decision 1):
   `AmetekWatch.sln`; `src/AmetekWatch.Core` (classlib); `src/AmetekWatch.App` (console exe,
   `AssemblyName=ametek-watch`); `tests/AmetekWatch.Tests` (xUnit). Wire references: App→Core,
   Tests→Core. Add the projects to the solution.
3. Extend `.gitignore` with `bin/`, `obj/`, `dist/`, `*.user` (do not remove the existing entries).
4. Implement the domain types, interfaces, `SweepRunner`, the three fakes, and `InMemoryFindingStore`
   in Core exactly per spec Decisions 3–6. Keep Core free of I/O and the Anthropic SDK.
5. Implement `Program` in App per Decision 7 — one fake sweep for `Subject="AMETEK"`, print the digest,
   exit 0.
6. Write the xUnit tests per the spec's "Tests" list. Assertions must be against **hand-computed
   expected values** (independent oracle), and must be able to actually fail — confirm by briefly
   inverting one assertion locally and seeing it fail, then revert.
7. Run the app once and capture real stdout for the report:
   `dotnet run --project src/AmetekWatch.App`.
8. Produce the Windows artifact (built, **not executed**):
   `dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o dist`
   — confirm `dist/ametek-watch.exe` exists (report its path + size). It must **not** be committed
   (`dist/` is gitignored).
9. Append a dated entry to `CLAUDE.md` → `### Unreleased` recording the slice landing (do **not** edit
   the existing bring-up bullet — add a new one below it). Keep it 3–6 lines, dev-facing.

## The gate
Run each **separately** (see `wiki/contracts/git-and-gates.md`), capture real output + counts:
- `dotnet build -c Release`
- `dotnet format --verify-no-changes`
- `dotnet test`

(The compiler is the typecheck; `dotnet format` stands in for lint. There is no `doc_check.py` step for
product code.)

## Versioning ritual
**Not a versioned ship.** This is internal bring-up — **no `CHANGELOG.md`, no git tag, no version bump**
(`<Version>` stays `0.1.0`). The only version-adjacent step is appending the `CLAUDE.md` Status &
Roadmap entry (Step 9). CHANGELOG.md is created at the first *user-facing* ship, not here.

## User-facing changelog content
None — this slice has no user-visible behavior. (Omit `CHANGELOG.md` entirely.)

## Merge
**Do not self-merge.** Commit in small granular commits; push `feature/cc-vertical-slice` to origin and
report the exact tip SHA. An independent **CX** integrator will run the gate and land it (CM will author
the `002-CX-integrate` spec from your reported SHA). Author ≠ integrator.

## Report-back format
Write `wiki/reports/report-spec001-CC-vertical-slice.md` per `wiki/rituals/report-format.md` (all
load-bearing sections; write "None." where one doesn't apply), plus these spec-specific items:
- **Toolchain:** the final `dotnet --version`, and whether you had to install the SDK (and how).
- **App output:** the real stdout from `dotnet run` (the printed digest), verbatim.
- **Windows artifact:** confirm `dist/ametek-watch.exe` was produced — path + byte size; confirm it is
  **not** tracked by git.
- **Gate table:** each of the three gate commands → ✓/✗ with real counts (test count before/after — for
  a new suite, "before = 0"), and the SHA the gate ran clean at.
- **Seams note:** one line confirming Core has no Anthropic-SDK / network dependency (auth is deferred).
- **Anything that surprised you**, and any judgment calls where you deviated from the spec.
