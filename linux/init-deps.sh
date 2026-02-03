#!/usr/bin/env bash
set -euo pipefail

DEPS_FILE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/deps.txt"

if [[ ! -f "$DEPS_FILE" ]]; then
  echo "Dependency file deps.txt not found at: $DEPS_FILE" >&2
  exit 1
fi

echo "Updating package database..."
sudo pacman -Sy --needed --noconfirm archlinux-keyring >/dev/null 2>&1 || true
sudo pacman -Syu --noconfirm

# Read deps, ignore blank lines and comments
mapfile -t DEPS < <(grep -vE '^\s*($|#)' "$DEPS_FILE" | sed 's/\r$//')

echo "Installing dependencies via pacman..."
sudo pacman -S --needed "${DEPS[@]}"

echo
echo "Dependency setup complete!"
