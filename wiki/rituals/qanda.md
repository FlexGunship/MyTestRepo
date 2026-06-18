# Ritual: Q&A

A bidirectional, asynchronous, **owner-mediated** channel for nuanced questions — distinct from
the spec/prompt/report pipeline. Files live in `wiki/qanda/` and are **durable memorials**: not
deleted on a whim. They persist as the record of what was asked and answered, and why.

Copy-paste starting points: [`templates/qanda-question.md`](../templates/qanda-question.md),
[`templates/qanda-answer.md`](../templates/qanda-answer.md).

---

## When to use Q&A

- **Verify behavior before drafting a spec that interacts with existing code.** The canonical
  use: the Manager believes the code does X, wants to spec work that depends on X, and asks the
  developer who can read the code to confirm before the spec is written on a false premise.
- **Memorialize nuance worth carrying forward** — an architectural judgment, a non-obvious
  constraint, a "why is it like this" that future sessions will re-ask if it isn't written down.

**Do NOT use Q&A for routine clarifications** that a chat-relay through the owner handles
cleanly. Q&A is for nuance worth memorializing, not for "which file is this in." When a Q&A
surfaces a durable pattern, it should *also* be inscribed into the relevant doc — Q&A
complements the durable docs, it doesn't replace them.

---

## Numbering & naming

Q&A has its **own** monotonic number pool, separate from spec numbers, starting at `1`. Search
`wiki/qanda/` for the highest `N` before filing. Question and answer share the same `N` and slug.

```
N-<asker>-question-<slug>.md      e.g.  3-CM-question-config-precedence.md
N-<answerer>-answer-<slug>.md     e.g.  3-CC-answer-config-precedence.md
```

Asker/answerer tags: `CM` (Manager), `CC` (Claude Code), `CX` (Codex), `GB` (Grok Build). (`GD` / `CX1` / `CX2` are retired — historical only.)
One question per file.

---

## Workflow (owner is the relay)

1. Asker writes the question file, commits it, and tells the owner it's staged.
2. Owner cues the responder ("read Q&A N, consider, write your answer").
3. Responder writes the answer file, commits it, tells the owner.
4. Owner cues the asker.

The owner mediates even though everything is in the wiki — the wiki holds the artifacts; the
owner directs who reads what when.

---

## Question file shape

Tight. Mobile-friendly. Should fit comfortably on one screen.

```markdown
# Q&A N — <topic>

**Asker:** <tag>
**Context:** <one line — what work this blocks>
**Question:** <the actual question, sharp>

## Background
What's relevant. Keep it short.

## What I'm hoping to learn
The decision this answer unblocks.
```

Do **not** include a speculative answer in the question — it prejudices the responder.

## Answer file shape

```markdown
# Q&A N — Answer: <topic>

**Answerer:** <tag>
**Question file:** N-<asker>-question-<slug>.md
**Date answered:** YYYY-MM-DD

## Direct answer
If it's yes/no, say so up front.

## Reasoning
Cite code paths, specs, evidence. Ground in the actual code — not intuition.

## Caveats and edge cases

## Implications for the asker's work
```

---

## Changelog
- 2026-05-28 — Initial.
