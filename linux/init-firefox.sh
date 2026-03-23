#!/usr/bin/env bash
# Set up Firefox integration: chrome symlinks, theme extension, and native messaging host.
# Run once after installing Firefox and creating a profile.
# Usage: bash linux/init-firefox.sh
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
EXTENSION_ID="theme-engine@shell-dev"
EXTENSION_SOURCE="$REPO_ROOT/theme-engine/firefox-extension"
NATIVE_HOST_SOURCE="$EXTENSION_SOURCE/native-host/theme_engine_host.sh"
FIREFOX_THEME_DIR="$REPO_ROOT/.config/firefox-theme"

# --- Locate Firefox profile ---
FIREFOX_PROFILES_DIR=""
for dir in "$HOME/.mozilla/firefox" "$HOME/.config/mozilla/firefox"; do
  if [[ -f "$dir/profiles.ini" ]]; then
    FIREFOX_PROFILES_DIR="$dir"
    break
  fi
done

if [[ -z "$FIREFOX_PROFILES_DIR" ]]; then
  echo "ERROR: No Firefox profiles.ini found. Launch Firefox once first, then re-run." >&2
  exit 1
fi

PROFILE_PATH=$(grep -A2 'Name=default-release' "$FIREFOX_PROFILES_DIR/profiles.ini" | grep 'Path=' | cut -d= -f2)
if [[ -z "$PROFILE_PATH" ]]; then
  echo "ERROR: Could not find default-release profile in $FIREFOX_PROFILES_DIR/profiles.ini" >&2
  exit 1
fi

PROFILE_DIR="$FIREFOX_PROFILES_DIR/$PROFILE_PATH"
echo "Firefox profile: $PROFILE_DIR"

# --- Helper: back up and symlink ---
safe_link() {
  local source="$1" target="$2" label="$3"
  if [[ -e "$target" && ! -L "$target" ]]; then
    local backup="$target.backup.$(date +%Y%m%d_%H%M%S)"
    echo "  Backing up existing $label: $target -> $backup"
    mv "$target" "$backup"
  fi
  ln -sfn "$source" "$target"
  echo "  $label: $source -> $target"
}

# --- 1. Symlink chrome/ directory (userChrome.css, userContent.css) ---
echo
echo "Setting up Firefox chrome symlink..."
mkdir -p "$FIREFOX_THEME_DIR"
safe_link "$FIREFOX_THEME_DIR" "$PROFILE_DIR/chrome" "chrome"

# --- 2. Symlink user.js (prefs) ---
echo
echo "Setting up Firefox user.js symlink..."
safe_link "$FIREFOX_THEME_DIR/user-prefs.js" "$PROFILE_DIR/user.js" "user.js"

# --- 3. Install signed theme extension ---
# Signed XPI is placed at extensions/<id>.xpi in the profile.
# Firefox will prompt once on next launch to enable it.
echo
echo "Installing theme engine extension..."
EXTENSIONS_DIR="$PROFILE_DIR/extensions"
mkdir -p "$EXTENSIONS_DIR"
XPI_SOURCE="$EXTENSION_SOURCE/dist/$EXTENSION_ID.xpi"
XPI_TARGET="$EXTENSIONS_DIR/$EXTENSION_ID.xpi"
if [[ ! -f "$XPI_SOURCE" ]]; then
  echo "  WARNING: Signed XPI not found at $XPI_SOURCE"
  echo "  Sign the extension first: web-ext sign --source-dir $EXTENSION_SOURCE --channel unlisted --api-key KEY --api-secret SECRET --artifacts-dir $EXTENSION_SOURCE/dist"
  echo "  Then rename the output to $EXTENSION_ID.xpi"
else
  safe_link "$XPI_SOURCE" "$XPI_TARGET" "extension"
fi

# --- 4. Set up native messaging host ---
# Firefox looks for native host manifests in ~/.mozilla/native-messaging-hosts/
echo
echo "Setting up native messaging host..."
NATIVE_HOSTS_DIR="$HOME/.mozilla/native-messaging-hosts"
mkdir -p "$NATIVE_HOSTS_DIR"

NATIVE_HOST_MANIFEST="$NATIVE_HOSTS_DIR/theme_engine.json"
cat > "$NATIVE_HOST_MANIFEST" <<EOF
{
    "name": "theme_engine",
    "description": "Shell-dev theme engine — watches theme-colors.json and sends updates to Firefox",
    "path": "$NATIVE_HOST_SOURCE",
    "type": "stdio",
    "allowed_extensions": ["$EXTENSION_ID"]
}
EOF
echo "  Native host manifest: $NATIVE_HOST_MANIFEST"
echo "  Native host script:   $NATIVE_HOST_SOURCE"

echo
echo "Firefox setup complete!"
echo "Restart Firefox and approve the extension when prompted."
