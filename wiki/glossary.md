# Glossary

Project-specific terms and conventions for the project.

| Term | Meaning |
|---|---|
| **CM** | **Claude Manager** — the planning seat; Claude on its own planning host. Plans, specs, reviews; **never ships code**. |
| **CC** | **Claude Code** — a developer surface; Claude in its own git worktree, distinct from the Manager. |
| **CX** | **Codex** — the GPT-5.5 Codex development surface (single; `CX1`/`CX2` historical, see [`roles/surfaces.md`](roles/surfaces.md)). |
| **GB** | **Grok Build** — the Grok/xAI development surface (new, 2026-06-04). |
| **Independent surfaces** | The Manager and the three developers are separate surfaces (`CC` has its own machine), so no agent authors, executes, and self-approves the same code. Supersedes the earlier "two-hat protocol." See [`roles/manager.md`](roles/manager.md). |
| **The owner** | Shawn. Always the relay between Manager and developers; directs which agent reads what when. |
| **The wiki** | This `wiki/` directory. The single source of truth and the only communication channel. "If it isn't in the wiki, it didn't happen." |
| **Spec** | `wiki/specs/NNN-XX-slug.md` — *what* to build and *why*. Durable statement of intent. |
| **Prompt** | `wiki/specs/prompt-specNNN-XX-slug.md` — *how* to execute; the message handed to a developer. |
| **Report** | `wiki/reports/report-specNNN-XX-slug.md` — the ship's durable evidence (one per spec). Append-only memorial. |
| **Review** | `wiki/reviews/review-NN-slug.md` — a standalone codebase audit decoupled from any single spec; broad multi-area discovery, bucketed findings. **Reports = per ship; Reviews = per state of the codebase.** |
| **Audit / investigation spec** | A spec that traces or reviews existing code without shipping any. May be a single self-contained file with no prompt (per [`rituals/spec-format.md`](rituals/spec-format.md)); must `git fetch && git pull --ff-only` first and record the audited SHA. |
| **Two-file pattern** | Every Now work unit = a spec **and** a prompt. |
| **Router tag** | The `-CC-` / `-CX-` / `-GB-` infix in a spec name (`-GD-` / `-CX1-` / `-CX2-` are retired tags, preserved historically). Says *who executes*. It is **not** a separate number space — all specs share one monotonic sequence. |
| **Spec numbering** | Issue the next spec number at `(highest existing) + 1`; search the committed wiki first. Numbers are never reused. See [Best practices](best-practices.md#spec-numbering). |
| **The gate** | the project's **docs gate** — `python tools/doc_check.py` (links/structure) **plus** an independent source-grounded review — that must pass before any deliverable merges to `main`. See [`contracts/git-and-gates.md`](contracts/git-and-gates.md). |
| **Gated merge** | The only path to `main`: a `--no-ff` merge from a feature branch on a green gate. |
| **Integration trunk** | `main`. Always green. Modified only via gated merge, never direct commit. |
| **Merge authority** | Who may merge to `main`. New surfaces are *onboarding* (integrator merges their branch); they *graduate* to self-merge by track record. See [`contracts/git-and-gates.md`](contracts/git-and-gates.md#merge-authority). |
| **Author ≠ integrator** | The independence rule for product-code merges: the surface that authored or executed a change is **not** the one who merges it to `main`. Being integrator on one task does not let you merge your own code on the next. See [`contracts/git-and-gates.md`](contracts/git-and-gates.md#merge-authority). |
| **Lane (a.k.a. epic)** | A 2–4-spec feature chain run by one agent on one long-lived `feature/<agent>-<lane>` branch with **disjoint directory ownership**, integrated to `main` **once at a milestone**. Cuts owner-relay touch points to one-per-milestone. Charters live in [`wiki/lanes/`](lanes/); ritual at [`rituals/lanes.md`](rituals/lanes.md). |
| **Q&A** | Owner-mediated, memorialized question/answer channel in `wiki/qanda/`. Own number pool. For nuance, not routine clarifications. |
| **Passdown** | End-of-arc continuity brief in `wiki/passdowns/`. Subordinate to the durable docs. |
| **Now / Next / Later** | Triage tiers. **Now** = active spec+prompt in `wiki/specs/`. **Next** = imminent backlog (truncated spec, no number, no prompt) in `wiki/backlog/next/`. **Later** = speculative, in `wiki/backlog/later/`. Flow is one-directional: Later → Next → Now. |
| **Truncated spec** | A backlog item: single slug-named file, no number, no prompt. Gets a number and prompt only when promoted to Now (because code state at draft time won't match execution time). |
| **External oracle** | An independent source of truth for verification (a hand-computed expected value, a reference implementation, or an authoritative spec) — *not* "matches the previous implementation." |
| **Staleness flag** | Surfacing a drifting doc/comment in a report rather than silently fixing it. |
| **Roles update notice** | The one-line announcement (relayed via the owner) that you edited a role doc. |
| **Memorial** | A file that is append-only by convention and not deleted casually (specs, reports, Q&A). |
| **TANSTAAFL Labs** | The company. "There Ain't No Such Thing As A Free Lunch." |

---

## Changelog
- 2026-06-02 — Added 4 terms in active use across other docs but missing from this glossary:
  **Review** (the `wiki/reviews/` pattern, distinct from a Report), **Audit / investigation spec**
  (the single-file no-prompt variant from `spec-format.md`), **Author ≠ integrator** (the merge
  independence shorthand), **Lane** (the multi-spec branch-with-disjoint-ownership pattern from
  `rituals/lanes.md`).
- 2026-05-29 — Retired "two-hat protocol" → "Independent surfaces"; `CM`/`CC` redefined as separate
  surfaces (`CC` on its own machine).
- 2026-05-28 — Initial.
