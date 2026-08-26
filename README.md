# shell-dev

Development environment setup and desktop theming for Windows and Linux.

Sets up a consistent dev environment across machines - dependencies,
editor config, git/SSH, and on Linux a fully themed Hyprland desktop
driven by a single JSON file.

## Setup

See [docs/setup.md](docs/setup.md).

## Theming (Linux)

The theme engine generates configs for the entire desktop from one JSON theme file.
Switch themes instantly from the theme picker or command line. The desktop shell
re-themes live — it watches its generated config, so no restart is needed.
Firefox themes update live via a signed WebExtension and native messaging.

- [Theme Engine](docs/theme-engine.md) — how it works, how to extend it
- [Theme JSON](docs/theme-json.md) — how to create themes
- [Keybindings](docs/keybindings.md) — keyboard shortcuts
- [Fingerprint](docs/fingerprint.md) — fingerprint unlock for hyprlock

## Desktop shell (Linux)

[`omni-shell`](https://git.macocian.com/radumaco/omni-shell) provides the bar,
notification centre, control centre and launcher as a single Quickshell process.
It replaces waybar, dunst and the wofi power menu; those packages are listed in
`linux/deps-uninstall.txt` and removed by `init-deps.sh`.
