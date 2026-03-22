#!/usr/bin/env bash
set -euo pipefail

# Repo root = parent of the linux/ folder
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

echo "RepoRoot: $REPO_ROOT"

mkdir -p "$HOME/.config"
mkdir -p "$HOME/.local/share/nvim"

# Configs to symlink: each entry is a directory under .config/
CONFIGS=(nvim hypr waybar dunst gtk-3.0 kitty wofi firefox-theme)

for cfg in "${CONFIGS[@]}"; do
  SOURCE="$REPO_ROOT/.config/$cfg"
  TARGET="$HOME/.config/$cfg"

  if [[ ! -d "$SOURCE" ]]; then
    echo "Skipping $cfg: not found in repo at $SOURCE"
    continue
  fi

  # If target exists and is not a symlink, back it up
  if [[ -e "$TARGET" && ! -L "$TARGET" ]]; then
    BACKUP="$TARGET.backup.$(date +%Y%m%d_%H%M%S)"
    echo "Backing up existing config dir: $TARGET -> $BACKUP"
    mv "$TARGET" "$BACKUP"
  fi

  echo "Linking $cfg:"
  echo "  $SOURCE -> $TARGET"
  ln -sfn "$SOURCE" "$TARGET"
done

# Symlink wallpapers into hypr config dir so hyprpaper can find them
WALL_SOURCE="$REPO_ROOT/wallpapers"
WALL_TARGET="$HOME/.config/hypr/wallpapers"
if [[ -d "$WALL_SOURCE" ]]; then
  if [[ -e "$WALL_TARGET" && ! -L "$WALL_TARGET" ]]; then
    BACKUP="$WALL_TARGET.backup.$(date +%Y%m%d_%H%M%S)"
    echo "Backing up existing wallpapers dir: $WALL_TARGET -> $BACKUP"
    mv "$WALL_TARGET" "$BACKUP"
  fi
  echo "Linking wallpapers:"
  echo "  $WALL_SOURCE -> $WALL_TARGET"
  ln -sfn "$WALL_SOURCE" "$WALL_TARGET"
fi

# Symlink VS Code settings.json (file-level, not the whole User dir)
VSCODE_SOURCE="$REPO_ROOT/.config/Code/User/settings.json"
VSCODE_TARGET="$HOME/.config/Code/User/settings.json"
if [[ -f "$VSCODE_SOURCE" ]]; then
  mkdir -p "$HOME/.config/Code/User"
  if [[ -e "$VSCODE_TARGET" && ! -L "$VSCODE_TARGET" ]]; then
    BACKUP="$VSCODE_TARGET.backup.$(date +%Y%m%d_%H%M%S)"
    echo "Backing up existing VS Code settings: $VSCODE_TARGET -> $BACKUP"
    mv "$VSCODE_TARGET" "$BACKUP"
  fi
  echo "Linking VS Code settings.json:"
  echo "  $VSCODE_SOURCE -> $VSCODE_TARGET"
  ln -sfn "$VSCODE_SOURCE" "$VSCODE_TARGET"
fi

# Create a default monitors.conf if it doesn't exist (machine-specific, not tracked in git)
MONITORS_CONF="$REPO_ROOT/.config/hypr/monitors.conf"
if [[ ! -f "$MONITORS_CONF" ]]; then
  echo "Creating default monitors.conf — edit this for your machine's displays"
  cat > "$MONITORS_CONF" << 'EOF'
# Machine-specific monitor configuration
# This file is not tracked in git — edit per machine
# See https://wiki.hypr.land/Configuring/Monitors/

monitor = ,preferred,auto,1
EOF
fi

# Configure SDDM autologin for current user (machine-specific)
echo "Configuring SDDM autologin for user: $USER"
sudo tee /etc/sddm.conf > /dev/null <<EOF
[Autologin]
User=$USER
Session=hyprland
EOF

# Symlink Firefox userChrome.css to the active profile's chrome/ dir
FIREFOX_THEME_SOURCE="$REPO_ROOT/.config/firefox-theme"
FIREFOX_PROFILES_DIR=""
for dir in "$HOME/.config/mozilla/firefox" "$HOME/.mozilla/firefox"; do
  if [[ -f "$dir/profiles.ini" ]]; then
    FIREFOX_PROFILES_DIR="$dir"
    break
  fi
done

if [[ -n "$FIREFOX_PROFILES_DIR" ]]; then
  PROFILE_PATH=$(grep -A2 'Name=default-release' "$FIREFOX_PROFILES_DIR/profiles.ini" | grep 'Path=' | cut -d= -f2)
  if [[ -n "$PROFILE_PATH" ]]; then
    CHROME_TARGET="$FIREFOX_PROFILES_DIR/$PROFILE_PATH/chrome"
    if [[ -e "$CHROME_TARGET" && ! -L "$CHROME_TARGET" ]]; then
      BACKUP="$CHROME_TARGET.backup.$(date +%Y%m%d_%H%M%S)"
      echo "Backing up existing Firefox chrome dir: $CHROME_TARGET -> $BACKUP"
      mv "$CHROME_TARGET" "$BACKUP"
    fi
    echo "Linking Firefox chrome:"
    echo "  $FIREFOX_THEME_SOURCE -> $CHROME_TARGET"
    ln -sfn "$FIREFOX_THEME_SOURCE" "$CHROME_TARGET"

    # Enable userChrome.css in Firefox prefs
    PREFS_FILE="$FIREFOX_PROFILES_DIR/$PROFILE_PATH/user.js"
    if ! grep -q 'toolkit.legacyUserProfileCustomizations.stylesheets' "$PREFS_FILE" 2>/dev/null; then
      echo 'user_pref("toolkit.legacyUserProfileCustomizations.stylesheets", true);' >> "$PREFS_FILE"
      echo "Enabled userChrome.css in Firefox user.js"
    fi
  else
    echo "Skipping Firefox: could not find default-release profile"
  fi
else
  echo "Skipping Firefox: no profiles.ini found"
fi

echo
echo "Setup complete!"
