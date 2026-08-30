# Keybindings

`SUPER` is the main modifier. Full config: [hyprland.lua](../.config/hypr/hyprland.lua).

## Core

| Keybind | Action |
| ------- | ------ |
| `SUPER + Q` | Terminal |
| `SUPER + C` | Close window |
| `SUPER + E` | File manager |
| `SUPER + R` | App launcher |
| `SUPER + F` | Fullscreen |
| `SUPER + V` | Toggle floating |
| `SUPER + SHIFT + S` | Screenshot region |
| `SUPER + SHIFT + R` | Record region |
| `SUPER + SHIFT + W` | Toggle animated wallpaper |
| `SUPER + SHIFT + T` | Theme picker (also `SUPER + X` → `T`) |

## Shell (omni-shell)

| Keybind | Action |
| ------- | ------ |
| `SUPER + X` | Control Center |
| `SUPER + ALT + P` | Control Center |
| `SUPER + SHIFT + V` | Clipboard history |
| `SUPER + W` | Weather panel |
| `SUPER + SHIFT + D` | Toggle Do Not Disturb |

## Control Center

Opens with `SUPER + X`. It holds an exclusive keyboard grab, so bare letters
work as mnemonics. Arrows move, `Enter` activates, `Esc` closes.

| Key | Action |
| --- | ------ |
| `L` | Lock |
| `S` | Sleep |
| `M` | Log off |
| `R` | Restart |
| `P` | Power off |
| `N` | Wi-Fi networks |
| `B` | Bluetooth devices |
| `V` | Toggle VPN |
| `D` | Toggle Do Not Disturb |
| `E` | Cycle power profile |
| `T` | Change theme (extension, see below) |
| `A` | Audio output picker |

Log off, restart and power off are two-step: the first press arms the action,
the second confirms, and the arm expires after 5s. Disconnecting Wi-Fi and
disconnecting or forgetting a Bluetooth device are guarded the same way.

The Theme tile is not built into omni-shell — it is a Control Center extension
in [`.config/omni-shell/extensions/theme.qml`](../.config/omni-shell/extensions/theme.qml),
the glue between omni-shell and rainbeau. Both are standalone projects that know
nothing about each other, so the integration lives here. Delete the file and the
tile disappears; nothing else changes.

## Bar clicks

| Widget | Action |
| ------ | ------ |
| Volume | Left: Control Center · Right: mute · Wheel: adjust |
| Updates | Left: run system upgrade · Right: re-check |
| Weather | Left: forecast panel · Right: refresh |
| Notifications | Left: notification centre · Right: clear all |
| Power | Control Center |
