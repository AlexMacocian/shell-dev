#!/usr/bin/env bash
# Power menu using wofi

entries="⏻  Shutdown\n󰜉  Reboot\n⏾  Suspend\n󰌾  Lock\n󰗼  Logout"

selected=$(echo -e "$entries" | wofi --dmenu --prompt "Power" --width 250 --height 260 --cache-file /dev/null --style ~/.config/wofi/style.css)

case "$selected" in
    *Shutdown*) systemctl poweroff ;;
    *Reboot*) systemctl reboot ;;
    *Suspend*) systemctl suspend ;;
    *Lock*) hyprctl dispatch exec hyprlock ;;
    *Logout*) hyprctl dispatch exit ;;
esac
