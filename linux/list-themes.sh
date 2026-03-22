#!/usr/bin/env bash
# List all available themes
# Usage: list-themes.sh

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
WALLPAPERS_DIR="$REPO_ROOT/wallpapers"

echo "Available themes:"
echo ""

for f in "$WALLPAPERS_DIR"/*.json; do
    [[ ! -f "$f" ]] && continue
    name=$(grep -o '"name": *"[^"]*"' "$f" | head -1 | sed 's/"name": *"//' | sed 's/"//')
    filename=$(basename "$f" .json)
    echo "  $name"
    echo "    File: $filename.json"
    echo ""
done
