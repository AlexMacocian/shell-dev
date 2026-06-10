#!/usr/bin/env bash
set -euo pipefail

scale="1"

if command -v hyprctl >/dev/null 2>&1 && command -v jq >/dev/null 2>&1; then
  detected_scale="$(
    hyprctl monitors -j 2>/dev/null \
      | jq -r 'first(.[] | select(.focused == true) | .scale) // empty' 2>/dev/null \
      || true
  )"

  if [[ "$detected_scale" =~ ^[0-9]+([.][0-9]+)?$ ]]; then
    scale="$detected_scale"
  fi
fi

exec microsoft-edge-stable --force-device-scale-factor="$scale" "$@"
