# Setup

## Windows

```powershell
# Install dependencies (via winget/choco)
.\windows\init-deps.ps1

# Set up git and SSH keys
.\windows\init-git.ps1

# Create symlinks for configs
.\windows\init-symlinks.ps1
```

## Linux

```bash
# Install packages (pacman + AUR + dotnet tools)
bash linux/init-deps.sh

# Symlinks, SDDM autologin
bash linux/init-symlinks.sh

# Neovim formatter plugins after Mason installs mdformat
bash linux/init-nvim-tools.sh

# Custom desktop entries and default file associations
bash linux/init-desktop-entries.sh

# Firefox: chrome symlinks, theme extension, native messaging host
bash linux/init-firefox.sh

# Microsoft Intune enrollment (optional, for work devices)
bash linux/init-intune.sh

# Apply a theme
bash linux/apply-theme.sh "Scarlet Rot" --restart
```

The app launcher is `omni-launcher` from the AUR, hosted inside `omni-shell`.
`init-deps.sh` installs both through `linux/deps-aur.txt`, `init-symlinks.sh`
links `.config/omni-launcher`, and Hyprland starts `omni-shell` once per
session — it provides the bar, notifications, control center and launcher.

`init-deps.sh` also removes the packages listed in `linux/deps-uninstall.txt`
(waybar, dunst, wofi and other tools omni-shell replaced). That pass runs after
the installs, so replacements are in place first, and it refuses to run if a
package is listed for both install and removal.

### File associations

[`init-desktop-entries.sh`](../linux/init-desktop-entries.sh) also runs
[`init-mime.sh`](../linux/init-mime.sh). To apply file associations independently:

```bash
bash linux/init-mime.sh
```

`xdg-mime` applies the defaults from [`mimeapps.list`](../.config/mimeapps.list)
without replacing unrelated associations.

| Files | Default application |
| --- | --- |
| `.tiled-project` | Tiled |
| `.png`, `.aseprite` | Aseprite |
| `.RPP` | REAPER |

REAPER is installed by [`deps.txt`](../linux/deps.txt). Tiled and Aseprite must
also be installed to open their associated files.
`deps.txt` also installs `mimetype`, which lets `xdg-open` recognize file
extensions on desktops such as Hyprland.
The [Tiled MIME definition](../.local/share/mime/packages/shell-dev.xml) identifies
project files separately from other JSON files.
The [REAPER launcher](../.local/share/applications/shell-dev-reaper.desktop) uses
`reaper` from PATH. These file associations also apply to `o` in the Snacks
explorer; no Neovim configuration is needed.

### Machine-specific

After running `init-symlinks.sh`, edit `~/.config/hypr/monitors.lua` for your
display layout. This file is gitignored.

### Theming

Themes live in `themes/*.json`. Apply with:

```bash
# By name
bash linux/apply-theme.sh "Scarlet Rot" --restart

# List available
bash linux/list-themes.sh

# Or from the desktop: SUPER + SHIFT + T
```

See [theme-engine.md](theme-engine.md) and [theme-json.md](theme-json.md) for details.
