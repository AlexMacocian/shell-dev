#!/usr/bin/env bash
# Apply a theme from the themes directory
# Usage: apply-theme.sh <theme-display-name>
# Example: apply-theme.sh "Elden Ring"

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
THEMES_DIR="$REPO_ROOT/themes"
ENGINE_DIR="$REPO_ROOT/theme-engine"

# Find theme file by display name
find_theme() {
    local search="$1"
    for f in "$THEMES_DIR"/*.json; do
        [[ ! -f "$f" ]] && continue
        display=$(grep -o '"name": *"[^"]*"' "$f" | head -1 | sed 's/"name": *"//' | sed 's/"//')
        if [[ "${display,,}" == "${search,,}" ]]; then
            echo "$f"
            return 0
        fi
    done
    return 1
}

list_themes() {
    echo "Available themes:"
    for f in "$THEMES_DIR"/*.json; do
        [[ ! -f "$f" ]] && continue
        display=$(grep -o '"name": *"[^"]*"' "$f" | head -1 | sed 's/"name": *"//' | sed 's/"//')
        echo "  $display"
    done
}

if [[ $# -lt 1 ]]; then
    echo "Usage: apply-theme.sh <theme-name>"
    echo ""
    list_themes
    exit 1
fi

THEME_NAME="$1"
THEME_FILE=$(find_theme "$THEME_NAME")

if [[ -z "$THEME_FILE" ]]; then
    echo "Theme not found: $THEME_NAME"
    echo ""
    list_themes
    exit 1
fi

shift
dotnet run --project "$ENGINE_DIR" -- "$THEME_FILE" "$@"
