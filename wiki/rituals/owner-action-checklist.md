# Ritual: Owner action checklist

> A living checklist of the things only **the owner** can do — the steps no agent can, because they
> touch live consoles, secrets/API keys, physical devices, paid accounts, app stores, or production
> deploys. CM maintains it; it mirrors the owner-gated flags tracked across specs and passdowns.

Most relevant to **build / product** projects (cloud backends, credentials, store releases), but any
project that needs the owner to act in the real world uses it. If a project has no such steps, this file
can stay empty or be deleted.

## How CM keeps it
- Create `wiki/owner-action-checklist.md` (a project copy of this) when the first owner-only action
  appears.
- Group by urgency: **✅ Already done** (don't redo) · **🔴 Do now** (unblocks real work) ·
  **🚀 Before launch** (pre-release) · **🔧 Future / optional**.
- For each item: what to do, why it matters, and any prerequisite. Mark items done with a date when the
  owner confirms.
- Keep **production credentials and deploys on the owner's machine only** — dev agent surfaces use
  throwaway/demo resources and never hold owner login or deploy rights.

## Template
```markdown
# <Project> — Owner Action Checklist
> Everything that needs you (the owner). Maintained by CM; the live source of truth is CLAUDE.md Status & Roadmap.

## ✅ Already done (don't redo)
- [x] <thing> — <date>

## 🔴 Do now — unblocks real work
### 1. <action>
- **Do:** <steps>   **Why:** <what it unblocks>   **Prereq:** <if any>

## 🚀 Before launch (pre-release)
- [ ] <action>

## 🔧 Future / optional
- [ ] <action>

## Standing rules
- Production credentials live only on the owner's machine; deploys run from there, once.
```

## Changelog
- (kit) Initial — generalized from a build project's owner-action checklist.
