#!/usr/bin/env bash
set -euo pipefail

MASON_ROOT="${MASON_ROOT:-${XDG_DATA_HOME:-$HOME/.local/share}/nvim/mason}"
MDFORMAT_BIN="$MASON_ROOT/bin/mdformat"
MDFORMAT_PIP="$MASON_ROOT/packages/mdformat/venv/bin/pip"
MDFORMAT_GFM_VERSION="${MDFORMAT_GFM_VERSION:-1.0.0}"

if [[ ! -x "$MDFORMAT_BIN" ]]; then
  cat >&2 <<EOF
mdformat is not installed by Mason yet.

Open Neovim and run:
  :MasonInstall mdformat

Then rerun:
  bash linux/init-nvim-tools.sh
EOF
  exit 1
fi

if [[ ! -x "$MDFORMAT_PIP" ]]; then
  echo "Could not find mdformat's Mason venv pip at: $MDFORMAT_PIP" >&2
  exit 1
fi

echo "Installing mdformat-gfm==$MDFORMAT_GFM_VERSION in Mason's mdformat environment..."
"$MDFORMAT_PIP" --disable-pip-version-check install --upgrade "mdformat-gfm==$MDFORMAT_GFM_VERSION"

printf '%s\n' '| A | B |' '| --- | --- |' '| x | y |' \
  | "$MDFORMAT_BIN" --wrap 80 --number --extensions gfm - >/dev/null

echo "Neovim markdown tools are ready."
