#!/usr/bin/env bash
set -euo pipefail

# Repo root = parent of the linux/ folder
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

echo "RepoRoot: $REPO_ROOT"

mkdir -p "$HOME/.config"
mkdir -p "$HOME/.local/share/nvim"

# Configs to symlink: each entry is a directory under .config/
CONFIGS=(nvim hypr waybar dunst gtk-3.0 kitty wofi firefox-theme hyprchat omni-launcher quick-visor sherlock)

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

# Fish writes universal variables to fish_variables, so link config entries individually
# instead of symlinking the whole ~/.config/fish directory.
FISH_SOURCE="$REPO_ROOT/.config/fish"
FISH_TARGET="$HOME/.config/fish"

if [[ ! -d "$FISH_SOURCE" ]]; then
  echo "Creating fish directory in repo at $FISH_SOURCE"
  mkdir -p "$FISH_SOURCE"
fi

if [[ -L "$FISH_TARGET" ]]; then
  echo "Replacing existing fish config symlink with a real directory: $FISH_TARGET"
  rm "$FISH_TARGET"
elif [[ -e "$FISH_TARGET" && ! -d "$FISH_TARGET" ]]; then
  BACKUP="$FISH_TARGET.backup.$(date +%Y%m%d_%H%M%S)"
  echo "Backing up existing fish config path: $FISH_TARGET -> $BACKUP"
  mv "$FISH_TARGET" "$BACKUP"
fi

mkdir -p "$FISH_TARGET"

shopt -s nullglob dotglob
for SOURCE in "$FISH_SOURCE"/*; do
  NAME="$(basename "$SOURCE")"
  [[ "$NAME" == "fish_variables" ]] && continue

  TARGET="$FISH_TARGET/$NAME"
  if [[ -e "$TARGET" && ! -L "$TARGET" ]]; then
    BACKUP="$TARGET.backup.$(date +%Y%m%d_%H%M%S)"
    echo "Backing up existing fish config entry: $TARGET -> $BACKUP"
    mv "$TARGET" "$BACKUP"
  fi

  echo "Linking fish/$NAME:"
  echo "  $SOURCE -> $TARGET"
  ln -sfn "$SOURCE" "$TARGET"
done
shopt -u nullglob dotglob

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

# Symlink themes into rainbeau config dir so `rainbeau select --themes-dir ~/.config/rainbeau/themes/` works
THEMES_SOURCE="$REPO_ROOT/themes"
THEMES_TARGET="$HOME/.config/rainbeau/themes"
if [[ -d "$THEMES_SOURCE" ]]; then
  mkdir -p "$HOME/.config/rainbeau"
  if [[ -e "$THEMES_TARGET" && ! -L "$THEMES_TARGET" ]]; then
    BACKUP="$THEMES_TARGET.backup.$(date +%Y%m%d_%H%M%S)"
    echo "Backing up existing themes dir: $THEMES_TARGET -> $BACKUP"
    mv "$THEMES_TARGET" "$BACKUP"
  fi
  echo "Linking themes:"
  echo "  $THEMES_SOURCE -> $THEMES_TARGET"
  ln -sfn "$THEMES_SOURCE" "$THEMES_TARGET"
fi

# Symlink VS Code settings.json (file-level, not the whole User dir)
VSCODE_SOURCE="$REPO_ROOT/.config/Code/User/settings.json"
VSCODE_TARGET="$HOME/.config/Code/User/settings.json"
mkdir -p "$(dirname "$VSCODE_SOURCE")"
# Create an empty settings.json in the repo if it doesn't exist yet
if [[ ! -f "$VSCODE_SOURCE" ]]; then
  echo '{}' >"$VSCODE_SOURCE"
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

# Symlink Microsoft Edge flags config (Wayland fixes)
EDGE_FLAGS_SOURCE="$REPO_ROOT/.config/microsoft-edge-stable-flags.conf"
EDGE_FLAGS_TARGET="$HOME/.config/microsoft-edge-stable-flags.conf"
if [[ -e "$EDGE_FLAGS_TARGET" && ! -L "$EDGE_FLAGS_TARGET" ]]; then
  BACKUP="$EDGE_FLAGS_TARGET.backup.$(date +%Y%m%d_%H%M%S)"
  echo "Backing up existing Edge flags: $EDGE_FLAGS_TARGET -> $BACKUP"
  mv "$EDGE_FLAGS_TARGET" "$BACKUP"
fi
echo "Linking Microsoft Edge flags:"
echo "  $EDGE_FLAGS_SOURCE -> $EDGE_FLAGS_TARGET"
ln -sfn "$EDGE_FLAGS_SOURCE" "$EDGE_FLAGS_TARGET"

# Create a default monitors.conf if it doesn't exist (machine-specific, not tracked in git)
MONITORS_CONF="$REPO_ROOT/.config/hypr/monitors.conf"
if [[ ! -f "$MONITORS_CONF" ]]; then
  echo "Creating default monitors.conf — edit this for your machine's displays"
  cat >"$MONITORS_CONF" <<'EOF'
# Machine-specific monitor configuration
# This file is not tracked in git — edit per machine
# See https://wiki.hypr.land/Configuring/Monitors/

monitor = ,preferred,auto,1
EOF
fi

# Configure SDDM autologin for current user (machine-specific)
echo "Configuring SDDM autologin for user: $USER"
sudo tee /etc/sddm.conf >/dev/null <<EOF
[Autologin]
User=$USER
Session=hyprland
EOF

echo
echo "Setup complete!"
