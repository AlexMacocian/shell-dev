#!/usr/bin/env bash
# Handle laptop lid open/close events for Hyprland
# Usage: lid-switch.sh [open|close]

case "$1" in
  close)
    # If external monitors are connected, just disable the laptop screen
    if hyprctl monitors -j | grep -q '"name": "DVI-I-'; then
      hyprctl keyword monitor "eDP-1, disable"
    fi
    ;;
  open)
    # Re-enable laptop screen
    hyprctl keyword monitor "eDP-1, preferred, auto, 1"
    ;;
esac
