#!/usr/bin/env bash
set -euo pipefail

# Repo root = parent of the linux/ folder
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

echo "RepoRoot: $REPO_ROOT"

mkdir -p "$HOME/.config"
mkdir -p "$HOME/.local/share/nvim"

# Configs to symlink: each entry is a directory under .config/
CONFIGS=(nvim hypr waybar)

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

echo
echo "Setup complete!"
