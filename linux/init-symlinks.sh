#!/usr/bin/env bash
set -euo pipefail

# Repo root = parent of the linux/ folder
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

CONFIG_SOURCE="$REPO_ROOT/.config/nvim"
CONFIG_TARGET="$HOME/.config/nvim"

DATA_DIR="$HOME/.local/share/nvim"

echo "RepoRoot: $REPO_ROOT"

if [[ ! -d "$CONFIG_SOURCE" ]]; then
  echo "Could not find .config/nvim in repo root: $REPO_ROOT" >&2
  exit 1
fi

mkdir -p "$HOME/.config"
mkdir -p "$DATA_DIR"

# If target exists and is not a symlink, back it up
if [[ -e "$CONFIG_TARGET" && ! -L "$CONFIG_TARGET" ]]; then
  BACKUP="$CONFIG_TARGET.backup.$(date +%Y%m%d_%H%M%S)"
  echo "Backing up existing config dir: $CONFIG_TARGET -> $BACKUP"
  mv "$CONFIG_TARGET" "$BACKUP"
fi

# Create/update symlink
echo "Linking config:"
echo "  Source: $CONFIG_SOURCE"
echo "  Target: $CONFIG_TARGET"
ln -sfn "$CONFIG_SOURCE" "$CONFIG_TARGET"

echo
echo "Setup complete!"
echo "Config symlink: $CONFIG_TARGET -> $(readlink -f "$CONFIG_TARGET")"
echo "Data dir ensured: $DATA_DIR"
