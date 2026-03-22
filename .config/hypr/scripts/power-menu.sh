#!/usr/bin/env bash
# Power menu using wofi

entries="⏻\tShutdown\n󰜉\tReboot\n⏾\tSuspend\n󰌾\tLock\n󰏘\tChange Theme\n󰗼\tLogout"

selected=$(echo -e "$entries" | wofi --dmenu --prompt "Power" --width 600 --height 350 --cache-file /dev/null --style ~/.config/wofi/style.css)

case "$selected" in
    *Shutdown*) systemctl poweroff ;;
    *Reboot*) systemctl reboot ;;
    *Suspend*) systemctl suspend ;;
    *Lock*) hyprctl dispatch exec hyprlock ;;
    *Change\ Theme*) ~/.config/hypr/scripts/theme-picker.sh ;;
    *Logout*) hyprctl dispatch exit ;;
esac
