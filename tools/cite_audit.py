#!/usr/bin/env python3
"""Citation dump — advisory helper for authors/integrators (NOT the gate).

For a docs page, print every `reference/<path>:<line>` citation next to **the actual source line it
points at**, beside the claim that cites it. This is a *dump*, not a matcher: it makes no judgement and
raises no false positives — you read each `SRC:` line against the claim and confirm the cited line
actually contains/supports what the sentence asserts. It exists because hand-rolled "smart" self-audit
scripts kept reporting `uncited=0` while missing real wrong-line/enumeration mistakes (specs 033/036):
the reliable method is to *open every cited line and read it*, which this automates the tedious part of.

Catches, when you actually read the output:
  - a member/symbol cited to a container/brace/comment/adjacent line (the SRC line won't contain it), and
  - an *enumeration* claim that names several members but cites only the first member's line (the other
    members won't be in any SRC line for that claim).

Usage:  python3 tools/cite_audit.py docs/subsystems/<page>.md [more.md ...]
Exit 0 always (advisory). The authoritative gate is `tools/doc_check.py` + the cross-model review.
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
CITE_RE = re.compile(r"reference/([^\s`):,]+):(\d+)")
FENCE_RE = re.compile(r"^\s*(?:`{3,}|~{3,})")


def source_line(path: str, lineno: int) -> str:
    p = ROOT / "reference" / path
    if not p.exists():
        return "<file missing>"
    try:
        raw = p.read_bytes()
    except OSError:
        return "<unreadable>"
    # Decode by BOM so UTF-16 sources (e.g. some .ps1) line up with the byte-faithful path:line
    # convention instead of mis-counting under a UTF-8 assumption.
    if raw[:2] in (b"\xff\xfe", b"\xfe\xff"):
        enc = "utf-16"
    elif raw[:3] == b"\xef\xbb\xbf":
        enc = "utf-8-sig"
    else:
        enc = "utf-8"
    lines = raw.decode(enc, errors="replace").splitlines()
    return lines[lineno - 1].strip() if 1 <= lineno <= len(lines) else "<line out of range>"


def dump(md_path: Path) -> int:
    text = md_path.read_text(encoding="utf-8", errors="replace")
    n_cites = 0
    in_fence = False
    for i, line in enumerate(text.splitlines(), 1):
        if FENCE_RE.match(line):
            in_fence = not in_fence
            continue
        if in_fence:
            continue
        cites = CITE_RE.findall(line)
        if not cites:
            continue
        claim = line.strip()
        print(f"\n{md_path.relative_to(ROOT)}:{i}  {claim[:160]}")
        for path, ln in cites:
            n_cites += 1
            print(f"    reference/{path}:{ln}\n        SRC: {source_line(path, int(ln))}")
    return n_cites


def main() -> int:
    if len(sys.argv) < 2:
        print("usage: python3 tools/cite_audit.py <page.md> [more.md ...]")
        return 0
    total = 0
    for arg in sys.argv[1:]:
        p = Path(arg) if Path(arg).is_absolute() else (ROOT / arg)
        if not p.exists():
            print(f"skip (not found): {arg}")
            continue
        total += dump(p)
    print(f"\ncite_audit: dumped {total} citation(s). Read each SRC line against its claim; "
          f"a cited line must contain/support the token or member it is attached to.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
