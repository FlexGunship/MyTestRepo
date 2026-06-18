PASS: CC spec-032 digest sink selection is wired into the App and verified by gates, can-fail proof, runtime execution, and source review.

## Branch State

- CX branch: `feature/cx-integrate-032`
- Clean reviewed SHA before CX review commit: `979ed7624d4c6c1820834a1eb7adde9965661df3`
- Reviewed upstream SHA: `origin/feature/cc-wire-digest-sink` = `979ed7624d4c6c1820834a1eb7adde9965661df3`
- .NET SDK: `8.0.422`

## Gate Table

| Gate | Command | Result | Counts |
| --- | --- | --- | --- |
| Build | `PATH="$HOME/.dotnet:$PATH" dotnet build -c Release` | PASS | 0 warnings, 0 errors |
| Format | `PATH="$HOME/.dotnet:$PATH" dotnet format --verify-no-changes` | PASS | exit 0, no formatting changes |
| Test | `PATH="$HOME/.dotnet:$PATH" dotnet test` | PASS | 0 failed, 89 passed, 0 skipped, 89 total |
| Can-fail proof | Temporarily inverted `DigestSinkSelectionTests.File_WithPath_ResolvesFileNotifier`, then ran `PATH="$HOME/.dotnet:$PATH" dotnet test` | PASS | Expected failure observed: 1 failed, 88 passed, 0 skipped, 89 total |
| Reverted can-fail proof | Restored assertion, then ran `PATH="$HOME/.dotnet:$PATH" dotnet test` | PASS | 0 failed, 89 passed, 0 skipped, 89 total |

## Runtime EXE Check

- Command: `PATH="$HOME/.dotnet:$PATH" dotnet run --project src/AmetekWatch.App`
- Result: PASS.
- The app ran with the shipped default File sink: `Digest sink: File -> FileDigestNotifier`.
- SQLite persistence confirmed by app output and direct DB read: `Persisted findings: 4`; Python SQLite read of generated `ametek-watch.db` returned row count `4`.
- File digest confirmed: generated `ametek-watch-digest.md` with heading `AMETEK Watch digest` and `3 items worth reporting.`

## Correctness Checks

| Check | Result | Evidence |
| --- | --- | --- |
| Default File sink is configured | ok | `src/AmetekWatch.App/appsettings.json:13` opens `Notify`; `src/AmetekWatch.App/appsettings.json:14` sets `Sink` to `File`; `src/AmetekWatch.App/appsettings.json:15` sets the digest file path. |
| Notify options model carries sink, path, and email config | ok | `src/AmetekWatch.App/NotifyOptions.cs:18` documents the supported sinks; `src/AmetekWatch.App/NotifyOptions.cs:22` defaults `Sink` to `File`; `src/AmetekWatch.App/NotifyOptions.cs:28` stores `DigestPath`; `src/AmetekWatch.App/NotifyOptions.cs:35` stores optional `EmailOptions`. |
| `DigestNotifierFactory` treats null/empty sink as File default | ok | `src/AmetekWatch.App/DigestNotifierFactory.cs:42` normalizes `notify.Sink ?? "File"`. |
| `Notify:Sink="File"` resolves `FileDigestNotifier` | ok | `src/AmetekWatch.App/DigestNotifierFactory.cs:66` branches on `File`; `src/AmetekWatch.App/DigestNotifierFactory.cs:68` rejects empty path; `src/AmetekWatch.App/DigestNotifierFactory.cs:75` returns `FileDigestNotifier`. Covered by `tests/AmetekWatch.Tests/DigestSinkSelectionTests.cs:24` through `tests/AmetekWatch.Tests/DigestSinkSelectionTests.cs:31`. |
| `Notify:Sink="Email"` resolves `EmailDigestNotifier` by construction only | ok | `src/AmetekWatch.App/DigestNotifierFactory.cs:49` branches on `Email`; `src/AmetekWatch.App/DigestNotifierFactory.cs:58` constructs `EmailDigestNotifier` with `SmtpEmailSender`; no send occurs in factory. Covered by `tests/AmetekWatch.Tests/DigestSinkSelectionTests.cs:46` through `tests/AmetekWatch.Tests/DigestSinkSelectionTests.cs:54`, including the no-send comment at line 51. |
| `Notify:Sink="None"` resolves `NullDigestNotifier` | ok | `src/AmetekWatch.App/DigestNotifierFactory.cs:44` branches on `None`; `src/AmetekWatch.App/DigestNotifierFactory.cs:46` returns `NullDigestNotifier`. Covered by `tests/AmetekWatch.Tests/DigestSinkSelectionTests.cs:91` through `tests/AmetekWatch.Tests/DigestSinkSelectionTests.cs:98`. |
| Disabled or incomplete email falls back to `NullDigestNotifier` with warning and no crash | ok | `src/AmetekWatch.App/DigestNotifierFactory.cs:51` checks usability; `src/AmetekWatch.App/DigestNotifierFactory.cs:53` logs a warning; `src/AmetekWatch.App/DigestNotifierFactory.cs:55` returns `NullDigestNotifier`; `src/AmetekWatch.App/DigestNotifierFactory.cs:87` through `src/AmetekWatch.App/DigestNotifierFactory.cs:103` define the required email fields. Covered by disabled-email test `tests/AmetekWatch.Tests/DigestSinkSelectionTests.cs:57` through `tests/AmetekWatch.Tests/DigestSinkSelectionTests.cs:66` and incomplete-host test `tests/AmetekWatch.Tests/DigestSinkSelectionTests.cs:69` through `tests/AmetekWatch.Tests/DigestSinkSelectionTests.cs:78`. |
| Program binds Notify safely and uses factory | ok | `src/AmetekWatch.App/Program.cs:30` through `src/AmetekWatch.App/Program.cs:35` bind `Notify`; `src/AmetekWatch.App/Program.cs:37` through `src/AmetekWatch.App/Program.cs:54` tolerate incomplete email binding; `src/AmetekWatch.App/Program.cs:89` through `src/AmetekWatch.App/Program.cs:90` call `DigestNotifierFactory.Create`; `src/AmetekWatch.App/Program.cs:92` through `src/AmetekWatch.App/Program.cs:95` pass notifier into `SweepHost` and run once. |
| `SweepHost` delivery path remains intact | ok | `src/AmetekWatch.App/SweepHost.cs:24` stores `IDigestNotifier`; `src/AmetekWatch.App/SweepHost.cs:35` accepts optional notifier; `src/AmetekWatch.App/SweepHost.cs:41` defaults to `NullDigestNotifier`; `src/AmetekWatch.App/SweepHost.cs:53` gets the digest; `src/AmetekWatch.App/SweepHost.cs:54` calls `NotifyAsync`. |
| Notifier implementations from specs 025/030 are untouched and still provide the expected behavior | ok | Branch diff has no `src/AmetekWatch.Core/Notify/*` changes. Existing file sink writes rendered markdown at `src/AmetekWatch.Core/Notify/FileDigestNotifier.cs:44` through `src/AmetekWatch.Core/Notify/FileDigestNotifier.cs:48`; email sink sends through injected sender at `src/AmetekWatch.Core/Notify/EmailDigestNotifier.cs:52` through `src/AmetekWatch.Core/Notify/EmailDigestNotifier.cs:57`; no-op sink completes at `src/AmetekWatch.Core/Notify/NullDigestNotifier.cs:11` through `src/AmetekWatch.Core/Notify/NullDigestNotifier.cs:12`. |
| Anthropic projects and solution are untouched | ok | `git diff --name-only origin/main...HEAD | rg "(SweepHost|Core/Notify|Anthropic|\\.sln$)"` returned no paths. `AmetekWatch.sln` exists and is not in the branch diff. |
| 028 end-to-end default File digest test still passes | ok | `tests/AmetekWatch.Tests/PipelineToggleAndDigestTests.cs:64` through `tests/AmetekWatch.Tests/PipelineToggleAndDigestTests.cs:84` exercise fake sweep persistence plus file digest content; full `dotnet test` passed with 89/89 tests. |

## No-Secret Confirmation

- PASS: targeted source/test/solution diff grep for credential literals (`api key`, `token`, `secret`, `password`, `bearer`, `sk-...`) returned no matches.
- Broader branch diff grep only found documentation/report references to secret checks and environment variable names, not credential values.

## HOLD Blockers

None.

VERDICT: PASS
