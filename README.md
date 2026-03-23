# shell-dev

Development environment setup and desktop theming for Windows and Linux.

Sets up a consistent dev environment across machines - dependencies,
editor config, git/SSH, and on Linux a fully themed Hyprland desktop
driven by a single JSON file.

## Setup

See [docs/setup.md](docs/setup.md).

## Theming (Linux)

The theme engine generates configs for the entire desktop from one JSON theme file.
Switch themes instantly from the power menu or command line. Firefox themes
update live via a signed WebExtension and native messaging - no restart needed.

- [Theme Engine](docs/theme-engine.md) — how it works, how to extend it
- [Theme JSON](docs/theme-json.md) — how to create themes
- [Keybindings](docs/keybindings.md) — keyboard shortcuts
- [Fingerprint](docs/fingerprint.md) — fingerprint unlock for hyprlock
