#!/usr/bin/env python3
"""Docs gate — fast, deterministic checks over the Markdown in the repo.

Checks:
  1. Every relative Markdown link/image target resolves to a real file (anchors stripped).
  2. No leftover placeholder markers (TODO/TKTK/XXX/FIXME/???) in shipped analysis under docs/.
  3. Every wiki page under docs/ carries a "## Revision history" section
     (see wiki/rituals/wiki-page-history.md).
  4. Inside ```mermaid blocks, every edge label |...| with a parenthesis is double-quoted,
     so GitHub's Mermaid parser renders it (an unquoted `(` is read as node-shape syntax).
  5. Inside ```mermaid sequenceDiagram blocks, message/Note text (after the message colon)
     contains no ';' / '->' / '<-', which break GitHub's renderer (statement separator / arrow
     tokens). Actor arrows (->>, -->>, ...) before the colon are unaffected.
  7. No leaked tool-call/function-call markup (e.g. a stray </invoke> or </content>) in any .md,
     outside code fences/inline spans — an authoring agent's artifact that is not valid doc content.
  6. GitHub-safe LaTeX (docs/ deliverables only): on a math line (one containing `$`) outside code
     fences, no backslash-before-ASCII-punctuation spacing macro (\\, \\; \\: \\!), no
     \\operatorname, and no underscore inside a \\text{}/\\mathrm{} group — GitHub's CommonMark pass
     strips the backslash before punctuation (leaving literal ',;:!'), \\operatorname is not in its
     MathJax allow-list, and an underscore inside \\text{}/\\mathrm{} renders "'_' allowed only in
     math mode" (the \\_ escape is stripped too, so move the identifier to inline code). Process docs
     under wiki/ may quote these as examples. See CLAUDE.md "Math in docs (GitHub-safe LaTeX)".

Exit code 0 = clean; 1 = at least one error. Run from the repo root: `python tools/doc_check.py`.
No third-party dependencies (stdlib only) so any machine with Python 3 can run the gate.
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SKIP_DIRS = {".git", ".github", "node_modules"}
# Inline + reference Markdown links and images: [text](target) and ![alt](target)
LINK_RE = re.compile(r"!?\[[^\]]*\]\(([^)]+)\)")
PLACEHOLDER_RE = re.compile(r"\b(TODO|TKTK|FIXME|XXX|\?\?\?)\b")
# A wiki page must carry a revision-history footer (heading level 2+).
REVHIST_RE = re.compile(r"^#{2,}\s+revision history\b", re.IGNORECASE | re.MULTILINE)
# Mermaid: opening fence of a ```mermaid block, any closing/opening code fence, and an
# edge label sitting between two pipes (e.g. -->|label|, -.->|label|).
MERMAID_OPEN_RE = re.compile(r"^\s*`{3,}\s*mermaid\b", re.IGNORECASE)
CODE_FENCE_RE = re.compile(r"^\s*`{3,}\s*$")
EDGE_LABEL_RE = re.compile(r"\|([^|]*)\|")
# A sequence-diagram actor arrow (e.g. ->>, -->>, ->, -->, -x, --x, -), --) ) — used only to
# recognise a message line's "actor arrow actor : text" form before the message colon.
SEQ_ARROW_RE = re.compile(r"--?>>?|--?x|--?\)|<<-{1,2}>>")
# Tokens that break GitHub's sequenceDiagram parser when they appear in message/Note *text*
# (after the message colon): ';' is a statement separator, '->'/'<-' are arrow tokens.
SEQ_BAD_TOKENS = (";", "->", "<-")
# GitHub-safe LaTeX: GitHub runs its CommonMark pass *before* MathJax, so a backslash before
# ASCII punctuation (\, \; \: \!) is escape-stripped to literal punctuation, and \operatorname is
# not in GitHub's MathJax allow-list (the whole block then errors). Flag these on any math line.
MATH_UNSAFE_RE = re.compile(r"\\[,;:!]|\\operatorname")
# An underscore inside a \text{...} or \mathrm{...} group renders a red MathJax error
# ("'_' allowed only in math mode") and dumps raw source. Escaping it (\_) does NOT help: GitHub's
# CommonMark pass strips the backslash before MathJax, so \_ -> _ -> the same error. The fix is to
# keep the math symbol clean and put the code identifier (e.g. `NUM_SAMPLES`) in inline code, not in
# math. The `_` class below also matches the escaped form, since `\_` contains a literal `_`.
MATH_TEXT_UNDERSCORE_RE = re.compile(r"\\(?:text|mathrm)\{[^}]*_[^}]*\}")
# Any code-fence line (``` or ~~~, optionally followed by a language) toggles in/out of a fenced
# block. (Inline single-backtick code spans are deliberately NOT tracked: a banned macro inside an
# inline span on a math line is still flagged — that errs toward safety, and the idiomatic fix is to
# show such examples in a fenced block, which is exempt.)
FENCE_TOGGLE_RE = re.compile(r"^\s*(?:`{3,}|~{3,})")
# Leaked tool-call / function-call markup an authoring agent accidentally left in a doc (e.g. a
# trailing </content> or </invoke> after the real content). These never legitimately appear in these docs
# prose. Matches the antml namespaced and bare forms of the function-calling wrapper tags.
ARTIFACT_RE = re.compile(r"</?(?:antml:)?(?:function_calls|invoke|parameter|content)\b")
# Inline code span — stripped before the artifact scan so a doc may still *quote* the tags in `code`.
INLINE_CODE_RE = re.compile(r"`[^`]*`")


def md_files() -> list[Path]:
    out: list[Path] = []
    for p in ROOT.rglob("*.md"):
        if any(part in SKIP_DIRS for part in p.relative_to(ROOT).parts):
            continue
        out.append(p)
    return sorted(out)


def is_external(target: str) -> bool:
    t = target.strip()
    return (
        t.startswith(("http://", "https://", "mailto:", "tel:", "#"))
        or t.startswith("<http")
    )


def check_links(path: Path, text: str, errors: list[str]) -> None:
    for m in LINK_RE.finditer(text):
        target = m.group(1).strip()
        if is_external(target):
            continue
        # Strip anchor and any surrounding angle brackets / title.
        target = target.split()[0].strip("<>")
        target = target.split("#", 1)[0]
        if not target:
            continue
        resolved = (path.parent / target).resolve()
        if not resolved.exists():
            errors.append(f"{path.relative_to(ROOT)}: broken link -> {m.group(1).strip()}")


def check_placeholders(path: Path, text: str, errors: list[str]) -> None:
    # Only enforce on shipped analysis deliverables under docs/.
    rel = path.relative_to(ROOT)
    if rel.parts and rel.parts[0] == "docs":
        for i, line in enumerate(text.splitlines(), 1):
            if PLACEHOLDER_RE.search(line):
                errors.append(f"{rel}:{i}: placeholder marker in deliverable -> {line.strip()}")


def check_revision_history(path: Path, text: str, errors: list[str]) -> None:
    # Enforce a revision-history footer on every wiki page under docs/.
    rel = path.relative_to(ROOT)
    if rel.parts and rel.parts[0] == "docs":
        if not REVHIST_RE.search(text):
            errors.append(
                f"{rel}: missing a '## Revision history' section "
                f"(see wiki/rituals/wiki-page-history.md)"
            )


def _is_quoted(label: str) -> bool:
    s = label.strip()
    return len(s) >= 2 and s.startswith('"') and s.endswith('"')


def check_mermaid(path: Path, text: str, errors: list[str]) -> None:
    # Render-safety for GitHub: inside a ```mermaid block, an edge label |...| that contains
    # an unquoted parenthesis makes GitHub's parser treat the `(` as node-shape syntax and the
    # whole diagram fails to render. We scope strictly to mermaid blocks (so Markdown tables,
    # which also use `|`, are never touched) and only flag the |...| edge-label form.
    in_block = False
    diagram_type = None  # first non-empty line inside a block names the type (e.g. "sequenceDiagram")
    for i, line in enumerate(text.splitlines(), 1):
        if not in_block:
            if MERMAID_OPEN_RE.match(line):
                in_block = True
                diagram_type = None
            continue
        if CODE_FENCE_RE.match(line):
            in_block = False
            continue
        if diagram_type is None and line.strip():
            diagram_type = line.split()[0].lower()
        for m in EDGE_LABEL_RE.finditer(line):
            label = m.group(1)
            if ("(" in label or ")" in label) and not _is_quoted(label):
                errors.append(
                    f"{path.relative_to(ROOT)}:{i}: unquoted parenthesis in Mermaid edge label "
                    f'-> |{label}|  (wrap as |"{label.strip()}"| so it renders on GitHub)'
                )
        # sequenceDiagram render-safety: inside a sequenceDiagram, a ';', '->' or '<-' in the
        # message/Note *text* (after the message colon) makes GitHub's parser fail. Scoped to
        # sequenceDiagram so flowchart edge syntax ('-->') is never flagged; only message lines
        # (an actor arrow before the colon) and Note lines are checked.
        if diagram_type == "sequencediagram" and ":" in line:
            before, _, msg_text = line.partition(":")
            is_message = SEQ_ARROW_RE.search(before) is not None
            is_note = before.strip().lower().startswith("note")
            if is_message or is_note:
                for tok in SEQ_BAD_TOKENS:
                    if tok in msg_text:
                        errors.append(
                            f"{path.relative_to(ROOT)}:{i}: '{tok}' in Mermaid sequenceDiagram "
                            f"message/Note text breaks GitHub's renderer -> |{msg_text.strip()}|  "
                            f"(reword: ';' to ',' or 'then'; '->'/'<-' to words)"
                        )
                        break


def check_math(path: Path, text: str, errors: list[str]) -> None:
    # Render-safety for GitHub LaTeX (mirrors check_mermaid's fence-toggle style). On any line that
    # carries math (contains a `$`) and is *not* inside a ``` code fence, reject the macros GitHub
    # mangles: \, \; \: \! (CommonMark strips the backslash before punctuation, leaving a literal
    # ',' / ';' / ... that MathJax then prints), \operatorname (not in GitHub's MathJax
    # allow-list -> the whole block errors out and dumps raw source), and an underscore inside a
    # \text{...}/\mathrm{...} group (renders "'_' allowed only in math mode"; the \_ escape is
    # stripped by CommonMark so it cannot be fixed in-place -> move the identifier to inline code).
    # The `$`-on-line guard keeps
    # prose/currency safe ("costs $5, then $10" has no backslash, so no match); the fence skip
    # avoids false positives from code/regex samples (e.g. this convention documented in CLAUDE.md).
    # Scoped to shipped analysis deliverables under docs/ (like check_placeholders): the render-safety
    # convention governs the rendered docs/ pages; process docs under wiki/ (specs/prompts/reports)
    # legitimately *quote* the banned macros as teaching examples and must not trip the gate.
    rel = path.relative_to(ROOT)
    if not (rel.parts and rel.parts[0] == "docs"):
        return
    in_fence = False
    for i, line in enumerate(text.splitlines(), 1):
        if FENCE_TOGGLE_RE.match(line):
            in_fence = not in_fence
            continue
        if in_fence or "$" not in line:
            continue
        m = MATH_UNSAFE_RE.search(line)
        if m:
            errors.append(
                f"{rel}:{i}: GitHub-unsafe math macro -> {m.group(0)} "
                f"(use \\quad/\\mathrm; see CLAUDE.md math style)"
            )
        mu = MATH_TEXT_UNDERSCORE_RE.search(line)
        if mu:
            errors.append(
                f"{rel}:{i}: GitHub-unsafe math -> underscore inside \\text{{}}/\\mathrm{{}} "
                f"-> {mu.group(0)} (move the code identifier to inline `code`, not math; "
                f"\\_ does not survive GitHub's CommonMark pass)"
            )


def check_stray_artifacts(path: Path, text: str, errors: list[str]) -> None:
    # Catch leaked tool-call/function-call markup (e.g. </invoke>, </content>, <parameter ...>) that
    # an authoring agent accidentally left in a doc — this slipped past every other check on the 013
    # architecture page (a trailing </content>/</invoke> after the revision table). Skip fenced code
    # blocks and inline code spans so a doc may still quote the tags as examples.
    in_fence = False
    for i, line in enumerate(text.splitlines(), 1):
        if FENCE_TOGGLE_RE.match(line):
            in_fence = not in_fence
            continue
        if in_fence:
            continue
        scrubbed = INLINE_CODE_RE.sub("", line)
        m = ARTIFACT_RE.search(scrubbed)
        if m:
            errors.append(
                f"{path.relative_to(ROOT)}:{i}: stray tool-call artifact in doc -> {m.group(0)} "
                f"(remove leaked function-call markup)"
            )


def main() -> int:
    errors: list[str] = []
    files = md_files()
    for path in files:
        text = path.read_text(encoding="utf-8", errors="replace")
        check_links(path, text, errors)
        check_placeholders(path, text, errors)
        check_revision_history(path, text, errors)
        check_mermaid(path, text, errors)
        check_math(path, text, errors)
        check_stray_artifacts(path, text, errors)
    print(f"doc_check: scanned {len(files)} markdown file(s).")
    if errors:
        print(f"doc_check: {len(errors)} error(s):")
        for e in errors:
            print(f"  - {e}")
        return 1
    print("doc_check: OK - links resolve, no placeholders in docs/.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
