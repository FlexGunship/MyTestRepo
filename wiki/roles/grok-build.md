# Role: Grok Build (`GB`)

A development surface running on **Grok (xAI)** with its own **git worktree** of the project repo.
Executes specs tagged `GB`. An **optional fifth surface** beyond the balanced 2 Claude + 2 Codex core —
add it when you want a third independent model: a cross-model **tie-breaker** (when CC↔CX disagree), an
extra `GB`-tagged executor under load, or for Grok's native **image/diagram** generation.

Grok Build **adopts the [Claude Dev contract](claude-dev.md) in its entirety** — the same gate, the same
git/branch discipline, the same report ritual, the same "execute only against a Manager-committed spec +
prompt" rule, the same "the code is the truth; flag a wrong spec, don't silently guess." This doc records
only what is specific to `GB`.

## What's specific to Grok Build

1. **Branch namespace.** Work `GB` specs on `feature/gb-<slug>` branches, cut from `main`, inside its
   worktree. `git fetch --prune` + rebase on `main` at spec boundaries; never check out a branch another
   worktree already holds.
2. **Executes only `GB`-tagged specs.** The router tag in a spec filename says who executes — do not pick
   up `CC`- or `CX`-tagged work.
3. **No self-merge until graduation.** Push the feature branch + report the tip SHA; an **independent
   integrator** (a Claude or Codex surface — a *different model*, to keep the merge cross-model) runs the
   gate and performs the `--no-ff` merge. `GB` earns self-merge authority by track record on its own
   auditions, like every surface.
4. **The pinned gate is non-negotiable.** `python tools/doc_check.py` (links/structure) **plus** an
   independent source-grounded review — analysis claims verified against the cited source `file:line` (see
   the [git & gates contract](../contracts/git-and-gates.md)). Process docs (reports) ship straight to `main`.
5. **Report every ship** to `wiki/reports/` (`report-specNNN-GB-slug.md`) following
   [report-format](../rituals/report-format.md) — real output, exact counts, surprises, and the
   `--no-ff`-merge / SHA discipline. Sign reports "— Grok Build (GB)".

## Runtime & dispatch (how the Manager brings GB online / runs it)

GB runs headlessly via the **`grok` CLI**. The Manager dispatches it the same way it dispatches Claude
(`claude -p`) and Codex (`codex exec`) surfaces:

```
nohup grok -p "$PROMPT" \
  --permission-mode bypassPermissions \      # autonomy (or --always-approve)
  --cwd ~/workspace/<project>/worktrees/gb \ # its worktree
  --output-format plain --no-alt-screen \
  < /dev/null > /tmp/gb-<spec>.log 2>&1 &     # < /dev/null: never let it block on stdin
```

- **`-p/--single`** = single-turn headless: agentic (multi-tool), prints the result, exits — the Grok
  analogue of `claude -p` / `codex exec`.
- **Autonomy:** `--permission-mode bypassPermissions` (or `--always-approve`) — the isolated-worktree
  equivalent of Claude's `--dangerously-skip-permissions` / Codex's approval `never`.
- **`< /dev/null`** is mandatory on background dispatch (same as Codex) — a detached agent that reads stdin
  hangs forever otherwise.
- **Model selection.** `grok models` lists what your login exposes and the active default. Grok's coding
  model is **`grok-build`**; set it as the persistent default with `[models] default = "grok-build"` in
  `~/.grok/config.toml`, or override per-call with `-m grok-build`.
- **Reasoning effort caveat.** Do **not** pass `--effort` / `--reasoning-effort` unless the chosen model
  advertises `supports_reasoning_effort: true` — models that don't (incl. the build/composer coding
  models) return `400: ... does not support parameter reasoningEffort`. Omit the flag; the model's default
  reasoning applies.

## Session-start (read in order)

1. This doc → [`claude-dev.md`](claude-dev.md) (the full contract `GB` adopts).
2. The other role docs in [`roles/`](.) (understand your counterparts; keep produces/executes clean).
3. [`/CLAUDE.md`](../../CLAUDE.md) **Status & Roadmap** (current state of the build).
4. The most recent passdown in [`passdowns/`](../passdowns/).
5. The most recent review in [`reviews/`](../reviews/) — it surfaces BLOCKER classes the per-spec gate
   can't see; skim anything touching your spec's surface area.
6. Open questions in [`qanda/`](../qanda/); the operating manual [`README.md`](../README.md); the
   [git & gates contract](../contracts/git-and-gates.md); your own learnings in
   [`surfaces/gb/GROK.md`](../surfaces/gb/GROK.md).

**Standing rules:** the owner is the sole relay — never cue another agent directly. Never print or commit
the GitHub token or any secret/API key. No deliverables ship during onboarding.

## Changelog

- Initial Grok Build role doc — optional fifth surface on the worktree topology; adopts the Claude Dev
  contract; dispatch/runtime guidance for the `grok` CLI.
