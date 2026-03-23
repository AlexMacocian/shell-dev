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

  # Create the directory in the repo if it doesn't exist yet
  if [[ ! -d "$SOURCE" ]]; then
    echo "Creating $cfg directory in repo at $SOURCE"
    mkdir -p "$SOURCE"
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
WALL_SOURCE="$REPO_ROOT/themes"
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
mkdir -p "$(dirname "$VSCODE_SOURCE")"
# Create an empty settings.json in the repo if it doesn't exist yet
if [[ ! -f "$VSCODE_SOURCE" ]]; then
  echo '{}' > "$VSCODE_SOURCE"
  echo "Created empty VS Code settings.json in repo"
fi
mkdir -p "$HOME/.config/Code/User"
if [[ -e "$VSCODE_TARGET" && ! -L "$VSCODE_TARGET" ]]; then
  BACKUP="$VSCODE_TARGET.backup.$(date +%Y%m%d_%H%M%S)"
  echo "Backing up existing VS Code settings: $VSCODE_TARGET -> $BACKUP"
  mv "$VSCODE_TARGET" "$BACKUP"
fi
echo "Linking VS Code settings.json:"
echo "  $VSCODE_SOURCE -> $VSCODE_TARGET"
ln -sfn "$VSCODE_SOURCE" "$VSCODE_TARGET"

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

echo
echo "Setup complete!"
