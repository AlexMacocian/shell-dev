#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DATA_HOME="${XDG_DATA_HOME:-$HOME/.local/share}"

for cmd in xdg-mime mimetype update-mime-database update-desktop-database; do
  if ! command -v "$cmd" >/dev/null; then
    echo "ERROR: $cmd is missing. Run linux/init-deps.sh first." >&2
    exit 1
  fi
done

link_file() {
  local source="$1" target="$2"
  mkdir -p "$(dirname "$target")"
  if [[ -e "$target" && ! -L "$target" ]]; then
    local backup="$target.backup.$(date +%Y%m%d_%H%M%S)"
    echo "Backing up: $target -> $backup"
    mv "$target" "$backup"
  fi
  echo "Linking: $source -> $target"
  ln -sfn "$source" "$target"
}

for source in "$REPO_ROOT"/.local/share/mime/packages/*.xml; do
  [[ -f "$source" ]] || continue
  link_file "$source" "$DATA_HOME/mime/packages/$(basename "$source")"
done
update-mime-database "$DATA_HOME/mime"

for source in "$REPO_ROOT"/.local/share/applications/*.desktop; do
  [[ -f "$source" ]] || continue
  link_file "$source" "$DATA_HOME/applications/$(basename "$source")"
done
update-desktop-database "$DATA_HOME/applications"

# Apply only tracked defaults; keep the machine's unrelated associations.
section=""
while IFS= read -r line || [[ -n "$line" ]]; do
  [[ -z "$line" || "$line" == \#* ]] && continue
  if [[ "$line" == \[* ]]; then
    section="$line"
    continue
  fi
  [[ "$section" == "[Default Applications]" ]] || continue
  if [[ "$line" != *=* ]]; then
    echo "ERROR: invalid MIME association: $line" >&2
    exit 1
  fi
  mime="${line%%=*}"
  desktop="${line#*=}"
  echo "Setting default: $mime -> $desktop"
  xdg-mime default "$desktop" "$mime"
done <"$REPO_ROOT/.config/mimeapps.list"

echo "File associations configured."
