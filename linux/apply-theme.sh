#!/usr/bin/env bash
# Apply a theme from the wallpapers directory
# Usage: apply-theme.sh <theme-name> [--restart]
# Example: apply-theme.sh elden-ring --restart

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
WALLPAPERS_DIR="$REPO_ROOT/wallpapers"
ENGINE_DIR="$REPO_ROOT/theme-engine"

if [[ $# -lt 1 ]]; then
    echo "Usage: apply-theme.sh <theme-name> [--restart]"
    echo ""
    echo "Available themes:"
    for f in "$WALLPAPERS_DIR"/*.json; do
        name=$(basename "$f" .json)
        display=$(grep -o '"name": *"[^"]*"' "$f" | head -1 | sed 's/"name": *"//' | sed 's/"//')
        echo "  $name  ($display)"
    done
    exit 1
fi

THEME_NAME="$1"
THEME_FILE="$WALLPAPERS_DIR/$THEME_NAME.json"

if [[ ! -f "$THEME_FILE" ]]; then
    echo "Theme not found: $THEME_FILE"
    echo ""
    echo "Available themes:"
    for f in "$WALLPAPERS_DIR"/*.json; do
        echo "  $(basename "$f" .json)"
    done
    exit 1
fi

shift
dotnet run --project "$ENGINE_DIR" -- "$THEME_FILE" "$@"
