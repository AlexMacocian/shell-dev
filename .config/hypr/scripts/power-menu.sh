#!/usr/bin/env bash
# Power menu using wofi

entries="⏻  Shutdown\n󰜉  Reboot\n⏾  Suspend\n󰌾  Lock\n󰏘  Change Theme\n󰗼  Logout"

selected=$(echo -e "$entries" | wofi --dmenu --prompt "Power" --width 250 --height 300 --cache-file /dev/null --style ~/.config/wofi/style.css)

case "$selected" in
    *Shutdown*) systemctl poweroff ;;
    *Reboot*) systemctl reboot ;;
    *Suspend*) systemctl suspend ;;
    *Lock*) hyprctl dispatch exec hyprlock ;;
    *Change\ Theme*) ~/.config/hypr/scripts/theme-picker.sh ;;
    *Logout*) hyprctl dispatch exit ;;
esac
