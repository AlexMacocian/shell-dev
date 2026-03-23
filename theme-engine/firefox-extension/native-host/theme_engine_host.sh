#!/usr/bin/env bash
# Native messaging host for the shell-dev Firefox theme engine.
# Watches ~/.config/firefox-theme/theme-colors.json via inotifywait and sends
# live theme updates to the browser extension over the native messaging protocol.
set -euo pipefail

# Clean up child processes (inotifywait) when Firefox terminates us
cleanup() { kill 0 2>/dev/null; exit 0; }
trap cleanup SIGTERM SIGINT SIGHUP

THEME_FILE="${HOME}/.config/firefox-theme/theme-colors.json"

# Send a native messaging message (4-byte LE length prefix + JSON).
send_message() {
    local json="$1"
    local len=${#json}
    # Write 4-byte little-endian length
    printf "\\x$(printf '%02x' $((len & 0xFF)))"
    printf "\\x$(printf '%02x' $(((len >> 8) & 0xFF)))"
    printf "\\x$(printf '%02x' $(((len >> 16) & 0xFF)))"
    printf "\\x$(printf '%02x' $(((len >> 24) & 0xFF)))"
    printf '%s' "$json"
}

# Read and send the current theme file if it exists and is valid JSON.
send_current_theme() {
    [[ -f "$THEME_FILE" ]] || return 0
    local json
    json=$(jq -c '.' "$THEME_FILE" 2>/dev/null) || return 0
    send_message "$json"
}

# Send the current theme on startup
send_current_theme

WATCH_DIR=$(dirname "$THEME_FILE")
WATCH_FILE=$(basename "$THEME_FILE")
mkdir -p "$WATCH_DIR"

# Watch for file changes with inotifywait
inotifywait -m -q -e close_write,moved_to,create "$WATCH_DIR" |
while read -r _ events filename; do
    if [[ "$filename" == "$WATCH_FILE" ]]; then
        sleep 0.05  # let the write settle
        send_current_theme
    fi
done
