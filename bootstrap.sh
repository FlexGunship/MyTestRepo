#!/usr/bin/env bash
# Agentic Start Kit — single-file bootstrap.
# Download THIS one file into an empty folder and run it. It pulls the kit's contents into the
# current directory, then points you at Claude Manager to finish setup interactively.
#
#   curl -fsSL https://raw.githubusercontent.com/FlexGunship/agentic-start-kit/main/bootstrap.sh -o bootstrap.sh
#   bash bootstrap.sh                 # scaffold here, into ./repo-master
#   bash bootstrap.sh my-project      # scaffold into ./my-project/repo-master
#
# Private repo note: cloning needs your GitHub auth. Easiest is the GitHub CLI:
#   gh auth login         # once per machine
# The script uses `gh repo clone` if `gh` is present, else falls back to `git clone` (which will
# prompt for credentials / use your git credential helper).
set -euo pipefail

KIT_OWNER="FlexGunship"
KIT_REPO="agentic-start-kit"
KIT_SLUG="${KIT_OWNER}/${KIT_REPO}"

PROJECT_DIR="${1:-.}"
mkdir -p "$PROJECT_DIR"
cd "$PROJECT_DIR"

if [ -e repo-master ]; then
  echo "error: ./repo-master already exists here — refusing to overwrite. Pick an empty folder." >&2
  exit 1
fi

echo "==> Fetching the kit ($KIT_SLUG) into ./repo-master ..."
tmp="$(mktemp -d)"
if command -v gh >/dev/null 2>&1; then
  gh repo clone "$KIT_SLUG" "$tmp/kit" -- --depth 1 >/dev/null
else
  git clone --depth 1 "https://github.com/${KIT_SLUG}.git" "$tmp/kit" >/dev/null
fi

# Lay the kit down as repo-master, dropping the kit's own git history (this is a NEW project).
rm -rf "$tmp/kit/.git"
mv "$tmp/kit" repo-master
rm -rf "$tmp"

cd repo-master
git init -q
git add -A
git commit -q -m "Scaffold project from agentic-start-kit" || true

cat <<'NEXT'

==> Done. Your project skeleton is in ./repo-master (a fresh git repo, kit history removed).

   Next:
   1. (optional) create a remote and push:  gh repo create <you>/<project> --private --source=. --push
   2. Open **Claude Manager** in repo-master and say:  "bootstrap this project"
      CM will interview you for the charter, pin the gate, and set up the cc1/cc2/cx1/cx2 worktrees.
      (See wiki/bootstrap/first-boot.md — you don't need to read it; CM does.)

   You only ever talk to Claude Manager from here.
NEXT
