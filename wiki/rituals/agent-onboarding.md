# Ritual: Agent onboarding (the project, worktree)

The procedure a developer surface follows to come online on the project — clone the repo into its workspace,
set up its worktree, confirm its lightweight toolchain, read its context, and prove the docs gate runs
green from its own worktree. Each surface gets a thin `NNN-XX-<tag>-onboarding` spec pointing here.

A surface is "online" when it can: clone/pull/branch/push the repo from its worktree, run the docs gate,
and has read its role + the current state. **The onboarding report is the proof.**

## Preconditions (owner provides; the agent preflights them)
- **GitHub auth** (token) for `git fetch`/`push` — **never printed or committed**.
- **Autonomy** appropriate to the isolated worktree (Claude Code `--dangerously-skip-permissions`;
  Codex approval `never`; Grok's equivalent).

## Steps

0. **PREFLIGHT — fail fast.** If any fails, stop and report the exact item:
   - Network + auth: `git ls-remote <the project repo URL>` succeeds.
   - Python present: `python --version` (or `python3`) ≥ 3.8.

1. **Clone into your workspace as a sibling project, then add your worktree.**
   ```
   git clone <the project repo URL> ~/workspace/the project/repo-master
   cd ~/workspace/the project/repo-master
   git worktree add ../worktrees/<tag> -b feature/<tag>-onboard      # <tag> = cc | cx | gb
   ```
   (Mirrors the standard topology `~/workspace/<project>/{repo-master, worktrees/<tag>}`. Any other
   project you have, e.g. a prior one, stays untouched as a sibling folder.)

2. **Toolchain — minimal (docs project).** Confirm **git** and **Python 3** (the gate is
   `python tools/doc_check.py`; no build toolchain). Set `git config user.name`/`user.email` to your
   surface. If/when the legacy source lands and needs a language-specific reader/LSP, that is a separate
   spec — onboarding needs only git + Python.

3. **Read, in order** (session-start): your role doc → the other role docs → [`/CLAUDE.md`](../../CLAUDE.md)
   **Status & Roadmap** → the most recent [passdown](../passdowns/) → the most recent
   [review](../reviews/) → open [Q&A](../qanda/) → [`README.md`](../README.md) + the
   [git & gates contract](../contracts/git-and-gates.md).

4. **Verify the docs gate** from your worktree, real output:
   ```
   python tools/doc_check.py        # exit 0, "OK"
   ```

5. **Report** → `wiki/reports/report-specNNN-XX-<tag>-onboarding.md` (process doc → direct to `main`):
   the worktree branch + synced SHA; `python --version`; the gate output (real); confirm `git push`
   works **without printing the token**; any blocker (with the exact failing step). No deliverables ship
   in onboarding.

## Grok Build (`GB`) onboarding — surface-specific notes

GB is an optional fifth surface (see [`roles/grok-build.md`](../roles/grok-build.md)). It onboards with the
**same steps above** — preflight, `git worktree add ../worktrees/gb -b feature/gb-onboard`, set git
identity, read context in order, prove the gate green, report. A few Grok-specific items the Manager and
the surface should know:

- **Runtime is the `grok` CLI**, not `claude`/`codex`. Headless single-turn dispatch:
  `grok -p "$PROMPT" --permission-mode bypassPermissions --cwd ~/workspace/<project>/worktrees/gb
  --output-format plain --no-alt-screen < /dev/null`. `--permission-mode bypassPermissions`
  (or `--always-approve`) is the autonomy flag; **`< /dev/null` is mandatory** (a detached agent that
  reads stdin hangs).
- **Preflight `grok` auth first:** a one-line `grok -p "reply OK"` smoke test confirms login + model before
  a long onboarding run. `grok login` if it isn't authenticated.
- **Pick the model up front.** `grok models` shows the available models and the active default; Grok's
  coding model is **`grok-build`**. Set it persistently via `[models] default = "grok-build"` in
  `~/.grok/config.toml`, or per-call with `-m grok-build`. **Don't pass `--effort`/`--reasoning-effort`**
  unless the model advertises support — coding models return `400 ... does not support parameter
  reasoningEffort`.
- **Onboarding report still lands via an integrator / the Manager**, not by GB itself — GB does not
  self-merge until graduation. The report is a process doc, so the Manager can land it mechanically.
- **Add the structural rows when activating GB:** a `wiki/surfaces/gb/GROK.md` learnings file (+ its row in
  [`surfaces/README.md`](../surfaces/README.md)), a `GB` row in [`roles/README.md`](../roles/README.md),
  and flip GB's status in [`roles/surfaces.md`](../roles/surfaces.md) from reserve to onboarding.

## Standing rules
- **Do not modify the legacy source** — it is the subject (and it is not even in the repo yet).
- **Never print or commit** the GitHub token or any secret.
- **The owner is the sole relay** — surfaces never cue each other directly.
- Google Drive is off-limits unless imported by spec. Environment-only — **no deliverables** in onboarding.
