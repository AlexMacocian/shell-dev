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

# Install .NET global tools from deps-dotnet.txt
DOTNET_DEPS_FILE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/deps-dotnet.txt"
if [[ -f "$DOTNET_DEPS_FILE" ]]; then
  if command -v dotnet &>/dev/null; then
    echo
    echo "Installing .NET global tools..."

    # Ensure ~/.dotnet/tools is in PATH for fish shell
    FISH_CONFIG="$HOME/.config/fish/config.fish"
    if [[ -f "$FISH_CONFIG" ]]; then
      if ! grep -q "fish_add_path.*\.dotnet/tools" "$FISH_CONFIG"; then
        echo "Adding ~/.dotnet/tools to fish PATH..."
        echo 'fish_add_path ~/.dotnet/tools' >> "$FISH_CONFIG"
      fi
    fi

    # Also add to bash/zsh if they exist
    for rc in "$HOME/.bashrc" "$HOME/.zshrc"; do
      if [[ -f "$rc" ]] && ! grep -q '\.dotnet/tools' "$rc"; then
        echo "Adding ~/.dotnet/tools to PATH in $rc..."
        echo 'export PATH="$HOME/.dotnet/tools:$PATH"' >> "$rc"
      fi
    done
    while IFS= read -r tool || [[ -n "$tool" ]]; do
      tool="$(echo "$tool" | sed 's/\r$//' | xargs)"
      [[ -z "$tool" || "$tool" == \#* ]] && continue
      if dotnet tool list -g 2>/dev/null | grep -qi "$tool"; then
        echo "$tool is already installed, updating..."
        dotnet tool update -g "$tool"
      else
        echo "Installing $tool..."
        dotnet tool install -g "$tool"
      fi
    done < "$DOTNET_DEPS_FILE"
  else
    echo "WARNING: dotnet not found. Skipping .NET global tool installs." >&2
  fi
fi

echo
echo "Dependency setup complete!"
