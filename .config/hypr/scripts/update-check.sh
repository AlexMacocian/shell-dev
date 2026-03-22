#!/usr/bin/env bash
# Check for available system updates (pacman + AUR)

pacman_updates=$(checkupdates 2>/dev/null | wc -l)
aur_updates=0

if command -v paru &>/dev/null; then
    aur_updates=$(paru -Qua 2>/dev/null | wc -l)
elif command -v yay &>/dev/null; then
    aur_updates=$(yay -Qua 2>/dev/null | wc -l)
fi

total=$((pacman_updates + aur_updates))

if [[ "$total" -eq 0 ]]; then
    echo '{"text": "", "tooltip": "System up to date", "class": "updated"}'
else
    tooltip="${pacman_updates} pacman, ${aur_updates} AUR"
    echo "{\"text\": \"󰏔 ${total}\", \"tooltip\": \"${tooltip}\", \"class\": \"pending\"}"
fi
