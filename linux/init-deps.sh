#!/usr/bin/env bash
set -euo pipefail

DEPS_FILE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/deps.txt"

if [[ ! -f "$DEPS_FILE" ]]; then
  echo "Dependency file deps.txt not found at: $DEPS_FILE" >&2
  exit 1
fi

echo "Updating package database..."
sudo pacman -Sy --needed archlinux-keyring >/dev/null 2>&1 || true
sudo pacman -Syu

# Read deps, ignore blank lines and comments
mapfile -t DEPS < <(grep -vE '^\s*($|#)' "$DEPS_FILE" | sed 's/\r$//;s/[[:space:]]*$//')

echo "Installing dependencies via pacman..."
sudo pacman -S --needed "${DEPS[@]}"

# Install AUR packages from deps-aur.txt
AUR_DEPS_FILE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/deps-aur.txt"
if [[ -f "$AUR_DEPS_FILE" ]]; then
  AUR_HELPER=""
  if command -v paru &>/dev/null; then
    AUR_HELPER="paru"
  elif command -v yay &>/dev/null; then
    AUR_HELPER="yay"
  fi

  if [[ -n "$AUR_HELPER" ]]; then
    mapfile -t AUR_DEPS < <(grep -vE '^\s*($|#)' "$AUR_DEPS_FILE" | sed 's/\r$//')
    if [[ ${#AUR_DEPS[@]} -gt 0 ]]; then
      echo
      echo "Installing AUR packages via $AUR_HELPER..."
      "$AUR_HELPER" -S --needed "${AUR_DEPS[@]}"
    fi
  else
    echo "WARNING: No AUR helper found (paru/yay). Skipping AUR packages." >&2
  fi
fi

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
        echo 'fish_add_path ~/.dotnet/tools' >>"$FISH_CONFIG"
      fi
    fi

    # Also add to bash/zsh if they exist
    for rc in "$HOME/.bashrc" "$HOME/.zshrc"; do
      if [[ -f "$rc" ]] && ! grep -q '\.dotnet/tools' "$rc"; then
        echo "Adding ~/.dotnet/tools to PATH in $rc..."
        echo 'export PATH="$HOME/.dotnet/tools:$PATH"' >>"$rc"
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
    done <"$DOTNET_DEPS_FILE"
  else
    echo "WARNING: dotnet not found. Skipping .NET global tool installs." >&2
  fi
fi

# Remove packages we explicitly do not want (deps-uninstall.txt).
# Runs last, so the replacements installed above are already in place before
# anything is taken away.
UNINSTALL_FILE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/deps-uninstall.txt"
if [[ -f "$UNINSTALL_FILE" ]]; then
  mapfile -t UNWANTED < <(grep -vE '^\s*($|#)' "$UNINSTALL_FILE" | sed 's/\r$//;s/[[:space:]]*$//')

  if [[ ${#UNWANTED[@]} -gt 0 ]]; then
    # A package in both an install list and the removal list would be installed
    # and uninstalled on every run. Fail loudly instead of flip-flopping.
    INSTALL_LIST="$(cat "$DEPS_FILE" "$AUR_DEPS_FILE" 2>/dev/null | grep -vE '^\s*($|#)' | sed 's/\r$//;s/[[:space:]]*$//')"
    CONFLICTS=()
    for pkg in "${UNWANTED[@]}"; do
      if printf '%s\n' "$INSTALL_LIST" | grep -qxF "$pkg"; then
        CONFLICTS+=("$pkg")
      fi
    done
    if [[ ${#CONFLICTS[@]} -gt 0 ]]; then
      echo "ERROR: listed for both install and removal: ${CONFLICTS[*]}" >&2
      echo "Remove them from deps.txt/deps-aur.txt or from deps-uninstall.txt." >&2
      exit 1
    fi

    TO_REMOVE=()
    for pkg in "${UNWANTED[@]}"; do
      if pacman -Qq "$pkg" &>/dev/null; then
        TO_REMOVE+=("$pkg")
      fi
    done

    if [[ ${#TO_REMOVE[@]} -gt 0 ]]; then
      echo
      echo "Removing unwanted packages: ${TO_REMOVE[*]}"
      # -Rns also drops now-orphaned dependencies and their config files.
      # A failure here is not fatal: a package still required by something else
      # should not abort the rest of the setup.
      if ! sudo pacman -Rns "${TO_REMOVE[@]}"; then
        echo "WARNING: could not remove some packages (still required?)." >&2
      fi
    else
      echo
      echo "No unwanted packages installed."
    fi
  fi
fi

echo
echo "Dependency setup complete!"
