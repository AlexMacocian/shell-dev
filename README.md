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

## AI workspace (Linux)

`ai/` is a sherlock workspace for AI work that is not tied to any project. It is
symlinked to `~/.local/share/ai` and opened from the Control Center's **AI** tile
(`SUPER + X`, then `C`).

Keeping it in this repo is the point: the agent's OKF memory bundles are tracked
files, so committing and pushing shell-dev carries what it has learned to every
other machine. `ai/okf-user/` is additionally mounted at `~/.okf`, OKF's
user-scope location, which makes those memories readable from *every* sherlock
project rather than only this workspace.

- [ai/README.md](ai/README.md) — layout, the two memory scopes, instruction files

