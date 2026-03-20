#!/usr/bin/env bash
# Handle laptop lid open/close events for Hyprland
# Only manages monitor enable/disable — suspend is handled by logind:
#   HandleLidSwitch=suspend
#   HandleLidSwitchExternalPower=ignore

SAVE_FILE="/tmp/hypr-monitor-scales"

case "$1" in
  close)
    # Save current scales for all monitors
    hyprctl monitors -j | jq -r '.[] | "\(.name) \(.scale)"' > "$SAVE_FILE"

    # If any external monitor is connected, disable the laptop screen
    external_count=$(hyprctl monitors -j | jq 'length')
    if [ "$external_count" -gt 1 ]; then
      hyprctl keyword monitor "eDP-1, disable"
    fi
    ;;
  open)
    # Re-enable laptop screen with default scale
    hyprctl keyword monitor "eDP-1, preferred, auto, 1"

    # Restore saved scales for all monitors
    if [ -f "$SAVE_FILE" ]; then
      while read -r name scale; do
        [ -n "$name" ] && [ -n "$scale" ] && \
          hyprctl keyword monitor "$name, preferred, auto, $scale"
      done < "$SAVE_FILE"
    fi
    ;;
esac
