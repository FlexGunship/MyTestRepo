# `reference/` — the analysis subject (read-only) — *optional, for documentation/RE projects*

This tree is for projects that **document or reverse-engineer an existing codebase**. Drop the subject
source here; it is **read-only** — the team documents and cites it, never edits it. For a **build**
project (writing new software) this directory is unused — delete it, and the source you write lives
wherever the toolchain expects (e.g. `lib/`, `src/`). Claude Manager decides which at first boot from
the charter.

When in use:
- **Cite into it as `reference/<path>:<line>`.** Storage is **byte-faithful** (`.gitattributes` disables
  EOL normalization) so line numbers and bytes stay stable across every surface and citation.
- **Document the import provenance** below when you paste the source: where it came from, original
  size/file-count, what was kept vs. excluded (and why), and the top-level layout. A lean source-only
  import (drop regenerable build output, vendored packages, large binaries) keeps the repo small and
  every tracked byte analysis-relevant. The first numbered spec is usually the import/onboarding pass
  that maps the layout in depth.

## Import provenance
_(fill in when the subject source is imported — source, date, original vs. stored size, exclusions, layout)_
