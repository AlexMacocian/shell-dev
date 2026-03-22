#!/usr/bin/env bash
# Theme picker using wofi
# Lists available themes from wallpapers/*.json and applies the selected one

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$(readlink -f "${BASH_SOURCE[0]}")")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"
WALLPAPERS_DIR="$REPO_ROOT/wallpapers"
ENGINE_DIR="$REPO_ROOT/theme-engine"

# Build list of themes using display names
entries=""
for f in "$WALLPAPERS_DIR"/*.json; do
    [[ ! -f "$f" ]] && continue
    display=$(grep -o '"name": *"[^"]*"' "$f" | head -1 | sed 's/"name": *"//' | sed 's/"//')
    entries+="󰏘  ${display}\n"
done

if [[ -z "$entries" ]]; then
    notify-send "Theme Picker" "No themes found in $WALLPAPERS_DIR" 2>/dev/null
    exit 1
fi

selected=$(echo -e "$entries" | wofi --dmenu --prompt "Theme" --width 350 --height 300 --cache-file /dev/null --style ~/.config/wofi/style.css)

if [[ -z "$selected" ]]; then
    exit 0
fi

# Extract the display name (strip the icon prefix)
theme_name=$(echo "$selected" | sed 's/^[^ ]* *//')

if [[ -z "$theme_name" ]]; then
    notify-send "Theme Picker" "Could not parse theme name" 2>/dev/null
    exit 1
fi

# Find theme file by display name
theme_file=""
for f in "$WALLPAPERS_DIR"/*.json; do
    [[ ! -f "$f" ]] && continue
    display=$(grep -o '"name": *"[^"]*"' "$f" | head -1 | sed 's/"name": *"//' | sed 's/"//')
    if [[ "$display" == "$theme_name" ]]; then
        theme_file="$f"
        break
    fi
done

if [[ -z "$theme_file" ]]; then
    notify-send "Theme Picker" "Theme not found: $theme_name" 2>/dev/null
    exit 1
fi

# Apply the theme with restart
dotnet run --project "$ENGINE_DIR" -- "$theme_file" --restart
