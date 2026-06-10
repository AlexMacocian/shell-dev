#!/usr/bin/env bash
set -euo pipefail

# Adds custom XDG data directories to XDG_DATA_DIRS via environment.d
# Each subdirectory under .local/share/applications/ in the repo becomes
# a separate XDG data dir, making its .desktop files discoverable by launchers.
#
# Repo structure:
#   .local/share/applications/work/   -> teams-pwa.desktop, outlook-pwa.desktop
#   .local/share/applications/gaming/ -> steam.desktop, etc.
#
# Each dir gets symlinked into ~/.local/share/ and added to XDG_DATA_DIRS.

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
REPO_APPS_DIR="$REPO_ROOT/.local/share/applications"
ENV_FILE="$HOME/.config/environment.d/xdg-data-dirs.conf"

if [[ ! -d "$REPO_APPS_DIR" ]]; then
  echo "No application directories found at $REPO_APPS_DIR"
  exit 0
fi

# Collect all subdirectories that contain .desktop files
EXTRA_DIRS=()
for dir in "$REPO_APPS_DIR"/*/; do
  [[ -d "$dir" ]] || continue
  dir_name="$(basename "$dir")"

  # Symlink into ~/.local/share/<name> so XDG finds <name>/applications/*.desktop
  LINK_PARENT="$HOME/.local/share/shell-dev-$dir_name"
  mkdir -p "$LINK_PARENT"

  LINK_TARGET="$LINK_PARENT/applications"
  if [[ -e "$LINK_TARGET" && ! -L "$LINK_TARGET" ]]; then
    BACKUP="$LINK_TARGET.backup.$(date +%Y%m%d_%H%M%S)"
    echo "Backing up: $LINK_TARGET -> $BACKUP"
    mv "$LINK_TARGET" "$BACKUP"
  fi

  echo "Linking $dir_name desktop entries:"
  echo "  $dir -> $LINK_TARGET"
  ln -sfn "$dir" "$LINK_TARGET"

  EXTRA_DIRS+=("$LINK_PARENT")
done

if [[ ${#EXTRA_DIRS[@]} -eq 0 ]]; then
  echo "No application subdirectories found."
  exit 0
fi

# Build the XDG_DATA_DIRS entry
# Prepend our dirs, then include the standard defaults
# Note: environment.d doesn't support shell variable expansion, so hardcode defaults
DIRS_STRING=""
for d in "${EXTRA_DIRS[@]}"; do
  DIRS_STRING+="$d:"
done
DIRS_STRING+="/usr/local/share:/usr/share"

mkdir -p "$(dirname "$ENV_FILE")"
echo "XDG_DATA_DIRS=$DIRS_STRING" > "$ENV_FILE"
echo
echo "Updated $ENV_FILE:"
cat "$ENV_FILE"

# Also update hyprland.conf env line for Wayland session
HYPR_CONF="$REPO_ROOT/.config/hypr/hyprland.conf"
if [[ -f "$HYPR_CONF" ]]; then
  # Build the Hyprland env value using $HOME so it's portable
  HYPR_DIRS=""
  for d in "${EXTRA_DIRS[@]}"; do
    dir_name="$(basename "$d")"
    HYPR_DIRS+="\$HOME/.local/share/$dir_name:"
  done
  HYPR_DIRS+="\$HOME/.local/share:/usr/local/share:/usr/share"
  HYPR_LINE="env = XDG_DATA_DIRS,$HYPR_DIRS"

  if grep -q "^env = XDG_DATA_DIRS" "$HYPR_CONF"; then
    sed -i "s|^env = XDG_DATA_DIRS,.*|$HYPR_LINE|" "$HYPR_CONF"
    echo "Updated XDG_DATA_DIRS in hyprland.conf"
  else
    sed -i "/^env = HYPRCURSOR_SIZE/a\\\\n# Custom XDG data dirs for repo-managed desktop entries\n$HYPR_LINE" "$HYPR_CONF"
    echo "Added XDG_DATA_DIRS to hyprland.conf"
  fi
fi

echo
echo "Custom application directories registered."
echo "Run 'hyprctl reload' or log out and back in for changes to take effect."

# Symlink custom icons into hicolor theme
REPO_ICONS_DIR="$REPO_ROOT/.local/share/icons"
if [[ -d "$REPO_ICONS_DIR" ]]; then
  HICOLOR_DIR="$HOME/.local/share/icons/hicolor/scalable/apps"
  mkdir -p "$HICOLOR_DIR"
  for icon in "$REPO_ICONS_DIR"/*.svg; do
    [[ -f "$icon" ]] || continue
    icon_name="$(basename "$icon")"
    ln -sf "$icon" "$HICOLOR_DIR/$icon_name"
    echo "Linked icon: $icon_name"
  done
  gtk-update-icon-cache -f "$HOME/.local/share/icons/hicolor" 2>/dev/null || true
  echo "Icon cache updated"
fi
