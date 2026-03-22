#!/usr/bin/env bash
# List all available themes
# Usage: list-themes.sh

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
THEMES_DIR="$REPO_ROOT/themes"

echo "Available themes:"
echo ""

for f in "$THEMES_DIR"/*.json; do
    [[ ! -f "$f" ]] && continue
    name=$(grep -o '"name": *"[^"]*"' "$f" | head -1 | sed 's/"name": *"//' | sed 's/"//')
    filename=$(basename "$f" .json)
    echo "  $name"
    echo "    File: $filename.json"
    echo ""
done
