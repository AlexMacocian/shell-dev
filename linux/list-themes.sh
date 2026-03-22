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
    name=$(basename "$f" .json)
    display=$(grep -o '"name": *"[^"]*"' "$f" | head -1 | sed 's/"name": *"//' | sed 's/"//')
    images=$(grep -c '"images"' "$f" 2>/dev/null || echo 0)
    videos=$(grep -c '"videos"' "$f" 2>/dev/null || echo 0)
    echo "  $name"
    echo "    Name: $display"
    echo "    File: $f"
    echo ""
done
